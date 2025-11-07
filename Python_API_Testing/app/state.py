from __future__ import annotations

import json
import random
import time
from datetime import datetime
from pathlib import Path
from typing import Any, Dict, List, MutableMapping, Optional

from .config import (
    BASE_DIR,
    DEFAULT_MEMORY_FILE,
    DEFAULT_REAL_WRITE_FILE,
    INV_DATA_FILE,
    INV_DATA_V1_1_FILE,
    INV_STATUS_DECODE_BY_PORT,
    READ_CMD_FORMAT_FILE,
    LINK_STATUS_FILE,
    LINK_STATUS_REFRESH_INTERVAL,
    MFR_MODEL_ASCII,
    MFR_MODEL_BY_PORT,
    PARTITION_STATUS_FILE,
    REAL_READ_TEMPLATE_FILE,
    TZ_TAIPEI,
    USE_RANDOM_DATA,
    V1_1_FIXED_CONFIG,
    V1_1_FIXED_VALUES,
)


class LinkStatusCache:
    """Filesystem-backed cache with time-based invalidation."""

    def __init__(self, path: Path, refresh_interval: float) -> None:
        self._path = path
        self._refresh_interval = refresh_interval
        self._payload: Optional[Dict[str, Any]] = None
        self._last_loaded: float = 0.0

    def get(self) -> Optional[Dict[str, Any]]:
        now = time.time()
        if (
            self._payload is None
            or (now - self._last_loaded) >= self._refresh_interval
        ):
            try:
                self._load()
            except Exception as exc:  # pragma: no cover - log and keep stale cache
                print(f"[link_status] reload failed: {exc}")
        return self._payload

    def _load(self) -> None:
        with self._path.open("r", encoding="utf-8") as f:
            self._payload = json.load(f)
        self._last_loaded = time.time()


class AppState:
    """Holds long-lived state and helpers for the mock NTN API."""

    def __init__(self) -> None:
        self.base_dir = BASE_DIR
        self.use_random_data = USE_RANDOM_DATA
        self._command_store: Dict[str, Dict[str, Any]] = {}
        self._real_write_store: Dict[str, Any] = {}

        self._base_data = self._load_json(INV_DATA_FILE)
        self._templates = [
            {k: v for k, v in item.items() if k != "data"}
            for item in self._base_data
        ]
        self._template_lengths = [len(item.get("data", [])) for item in self._base_data]

        self._real_inv_template = self._load_json(REAL_READ_TEMPLATE_FILE)
        self._inv_data_v1_1_template = self._load_json(INV_DATA_V1_1_FILE)

        self._link_status_cache = LinkStatusCache(
            self.base_dir / LINK_STATUS_FILE,
            LINK_STATUS_REFRESH_INTERVAL,
        )

    # ------------------------------------------------------------------
    # Initialization helpers
    # ------------------------------------------------------------------
    def memory_init(self, filename: Path | str = DEFAULT_MEMORY_FILE) -> None:
        try:
            data = self._load_json(filename)
        except (FileNotFoundError, json.JSONDecodeError) as exc:
            print(f"[memory_init] skipped loading {filename}: {exc}")
            self._command_store = {}
            return

        store: Dict[str, Dict[str, Any]] = {}

        for item in data:
            cmd = item.get("commandName")
            if not cmd:
                continue

            is_per_addr = item.get("isPerAddr", False)
            if not is_per_addr:
                store[cmd] = {
                    "isPerAddr": False,
                    "targetValue": item.get("targetValue", []),
                }
                continue

            addr_dict: Dict[str, Any] = {}
            for pair in item.get("addrValues", []):
                addr = pair.get("addr")
                if addr is None:
                    continue
                addr_dict[addr] = pair.get("value", [])

            store[cmd] = {
                "isPerAddr": True,
                "addrValues": addr_dict,
            }

        self._command_store = store
        print("Init finished. Commands loaded:")
        for key, value in self._command_store.items():
            print(f"  {key} => {value}")

    def real_write_memory_init(
        self,
        filename: Path | str = DEFAULT_REAL_WRITE_FILE,
    ) -> None:
        self._real_write_store = self._load_real_write_store(filename)
        print(
            "Init finished. REAL_WRITE_STORE loaded:"
            f" {list(self._real_write_store.keys())}"
        )

    def _load_real_write_store(
        self,
        filename: Path | str = DEFAULT_REAL_WRITE_FILE,
    ) -> Dict[str, Any]:
        try:
            data = self._load_json(filename)
        except FileNotFoundError as exc:  # pragma: no cover - surface issue upstream
            raise ValueError(f"REAL_WRITE_STORE source {filename} missing") from exc

        if not isinstance(data, dict):
            raise ValueError("REAL_WRITE_STORE expects a JSON object")

        return data

    def _write_real_write_store(
        self,
        filename: Path | str = DEFAULT_REAL_WRITE_FILE,
    ) -> None:
        path = Path(filename)
        if not path.is_absolute():
            path = self.base_dir / path

        path.parent.mkdir(parents=True, exist_ok=True)
        with path.open("w", encoding="utf-8") as handle:
            json.dump(self._real_write_store, handle, indent=4)

    def _load_partition_status_payload(
        self,
        filename: Path | str = PARTITION_STATUS_FILE,
    ) -> Dict[str, Any]:
        try:
            payload = self._load_json(filename)
        except FileNotFoundError as exc:  # pragma: no cover - surface config issue upstream
            raise ValueError(f"partition-status source {filename} missing") from exc
        except json.JSONDecodeError as exc:  # pragma: no cover - invalid JSON is operational error
            raise ValueError(f"partition-status source {filename} invalid: {exc}") from exc

        if not isinstance(payload, dict):
            raise ValueError("partition-status payload must be a JSON object")

        return payload

    # ------------------------------------------------------------------
    # Route backends
    # ------------------------------------------------------------------
    def build_read_real_response(self, port: str, addr: int) -> Dict[str, Any]:
        resp = json.loads(json.dumps(self._inv_data_v1_1_template))
        resp["port"] = port
        resp["addr"] = addr
        resp["timestamp"] = datetime.now(TZ_TAIPEI).isoformat()

        for key, item in resp.get("values", {}).items():
            value_type = item.get("type")

            if key == "MFR_MODEL":
                model_value = MFR_MODEL_BY_PORT.get(port.upper(), item.get("value"))
                item["value"] = model_value
                continue

            if key == "INV_STATUS":
                self._apply_inv_status_decode(item, port)
                continue

            if V1_1_FIXED_CONFIG.get(key):
                if key in V1_1_FIXED_VALUES:
                    item["value"] = V1_1_FIXED_VALUES[key]
                continue

            if value_type == "Numeric":
                item["value"] = round(random.uniform(0, 1000), 2)
            elif value_type == "BitField":
                item["value"] = 0
            elif value_type == "ASCII":
                continue

        return resp

    def get_link_status(self) -> Optional[Dict[str, Any]]:
        return self._link_status_cache.get()

    def build_read_memory_response(self, port: str, addr: str) -> List[Dict[str, Any]]:
        response: List[Dict[str, Any]] = []
        upper_port = port.upper() if port else ""

        for tpl, length in zip(self._templates, self._template_lengths):
            cmd = tpl.get("commandName")
            group_index = tpl.get("groupIndex")

            if self.use_random_data:
                data = [random.getrandbits(8) for _ in range(length)]
            else:
                data = [0] * length

            if cmd == "MFR_MODEL":
                data = self._build_mfr_model_data(upper_port, addr, group_index, length)

            if cmd == "INV_FAULT":
                fixed = [0, 0]
                data = fixed[:length] + [0] * max(0, length - len(fixed))

            if cmd == "INV_STATUS":
                raw_value = self._get_inv_status_raw_value(upper_port)
                data = [(raw_value >> (8 * i)) & 0xFF for i in range(length)]

            if cmd == "READ_FAN_SPEED_1":
                fixed_val = 3450
                bytes_le = [fixed_val & 0xFF, (fixed_val >> 8) & 0xFF]
                data = bytes_le[:length] + [0] * max(0, length - len(bytes_le))

            if cmd == "READ_VIN":
                vin_value = round(random.uniform(109.0, 111.0), 1)
                raw = int(vin_value / 0.1)
                bytes_le = [raw & 0xFF, (raw >> 8) & 0xFF]
                data = bytes_le[:length] + [0] * max(0, length - len(bytes_le))

            response.append({**tpl, "data": data})

        return response

    def build_write_memory_snapshot(self) -> List[Dict[str, Any]]:
        snapshot: List[Dict[str, Any]] = []
        for cmd, info in self._command_store.items():
            obj: Dict[str, Any] = {
                "commandName": cmd,
                "isPerAddr": info["isPerAddr"],
            }
            if info["isPerAddr"]:
                obj["addrValues"] = [
                    {"addr": addr, "value": value}
                    for addr, value in info["addrValues"].items()
                ]
            else:
                obj["targetValue"] = info.get("targetValue", [])
            snapshot.append(obj)
        return snapshot

    def apply_write_memory(self, payload: List[Dict[str, Any]]) -> None:
        self._validate_write_payload(payload)
        for item in payload:
            cmd = item.get("commandName")
            if cmd not in self._command_store:
                continue

            is_per_addr = item.get("isPerAddr") is True
            if not is_per_addr:
                self._command_store[cmd]["targetValue"] = item.get("targetValue", [])
                continue

            addr_dict = self._command_store[cmd]["addrValues"]
            for pair in item.get("addrValues", []):
                addr = pair.get("addr")
                if addr in addr_dict:
                    addr_dict[addr] = pair.get("value", [])

    def get_real_write_store(self) -> Dict[str, Any]:
        return self._real_write_store

    def update_real_write_store(self, file_name: str, payload: Any) -> List[Dict[str, Any]]:
        self._real_write_store = self._load_real_write_store()
        key = self._resolve_real_write_key(file_name)

        updates = self._normalize_real_write_updates(payload)
        if not updates:
            raise ValueError("Payload must contain at least one command update")

        command_list: List[Dict[str, Any]] = self._real_write_store.get(key, [])
        command_lookup = {}
        for cmd in command_list:
            name_key = self._ci_string((cmd or {}).get("commandName"))
            if name_key:
                command_lookup[name_key] = cmd

        for update in updates:
            command_name = self._ci_string(self._ci_get(update, "commandName"))
            if not command_name:
                raise ValueError("Each update item requires a commandName")

            command = command_lookup.get(command_name)
            if command is None:
                raise KeyError(f"Command {command_name} not found in {key}")

            if self._ci_bool(command, "isPerAddr"):
                self._update_real_write_per_addr(command, update)
            else:
                self._update_real_write_target(command, update)

            if self._ci_has_key(update, "targetDirty"):
                command["targetDirty"] = self._ci_get(update, "targetDirty")

        self._write_real_write_store()
        return updates

    def get_real_write_payload(self, type_hint: Optional[str]) -> Dict[str, Any]:
        self._real_write_store = self._load_real_write_store()

        payload = self._clone_real_write_store()
        type_upper = (type_hint or "").strip().upper()

        if type_upper.startswith("CAN"):
            payload = self._rename_real_write_key(
                payload,
                source_key="NTN-5K_MOD.json",
                target_key="NTN-5K_CAN.json",
            )

        return payload

    def get_partition_status(self, port: Optional[str], protocol: Optional[str]) -> Dict[str, Any]:
        payload = self._load_partition_status_payload()

        if port is not None:
            payload["port"] = port

        if protocol is not None:
            payload["protocol"] = protocol

        return payload

    def get_read_attribute_payload(self, port: str, protocol: str) -> Dict[str, Any]:
        raw = self._load_json(READ_CMD_FORMAT_FILE)
        if not isinstance(raw, dict):
            raise ValueError("read_CmdFormat payload must be a JSON object")

        payload = json.loads(json.dumps(raw))
        payload["port"] = port
        payload["protocolName"] = protocol
        return payload

    # ------------------------------------------------------------------
    # Internal helpers
    # ------------------------------------------------------------------
    def _load_json(self, filename: Path | str) -> Any:
        path = Path(filename)
        if not path.is_absolute():
            path = self.base_dir / path
        with path.open("r", encoding="utf-8") as f:
            return json.load(f)

    def _clone_real_write_store(self) -> Dict[str, Any]:
        return json.loads(json.dumps(self._real_write_store))

    def _resolve_real_write_key(self, file_name: str) -> str:
        if file_name in self._real_write_store:
            return file_name

        if (
            file_name == "NTN-5K_CAN.json"
            and "NTN-5K_MOD.json" in self._real_write_store
        ):
            return "NTN-5K_MOD.json"

        raise KeyError(f"File {file_name} not found in REAL_WRITE_STORE")

    def _normalize_real_write_updates(self, payload: Any) -> List[Dict[str, Any]]:
        if isinstance(payload, dict):
            return [self._normalize_object_case(payload)]

        if isinstance(payload, list):
            normalized: List[Dict[str, Any]] = []
            for item in payload:
                if not isinstance(item, dict):
                    raise ValueError("Updates list must contain JSON objects")
                normalized.append(self._normalize_object_case(item))
            return normalized

        raise ValueError("Payload must be a JSON object or an array of objects")

    def _update_real_write_target(self, command: Dict[str, Any], update: Dict[str, Any]) -> None:
        target_payload = update.get("target")
        data_format = (command.get("dataFormat") or "").strip()

        if target_payload is None:
            raise ValueError(f"Update for {command.get('commandName')} requires target data")

        target = self._ci_get(command, "target")
        if target is None:
            target = {"number": None, "text": None, "bits": {}}
            command["target"] = target

        if data_format == "Numeric":
            number = self._ci_get(target_payload, "number")
            if number is None:
                raise ValueError(f"Numeric command {command.get('commandName')} requires a number value")
            target["number"] = number
            if self._ci_has_key(target_payload, "text"):
                target["text"] = self._ci_get(target_payload, "text")
            if self._ci_has_key(target_payload, "bits"):
                target["bits"] = self._ci_get(target_payload, "bits")
        elif data_format in {"BitField", "BitControl"}:
            bits_payload = self._ci_get(target_payload, "bits")
            if not isinstance(bits_payload, dict):
                raise ValueError(f"BitField command {command.get('commandName')} requires a bits map")

            existing_bits = self._ci_get(target, "bits")
            if not isinstance(existing_bits, dict):
                existing_bits = {}
            target["bits"] = existing_bits

            for bit_key, bit_value in bits_payload.items():
                effective_key = self._resolve_bit_key(existing_bits, bit_key)
                existing_bits[effective_key] = bit_value

            if self._ci_has_key(target_payload, "number"):
                target["number"] = self._ci_get(target_payload, "number")
            if self._ci_has_key(target_payload, "text"):
                target["text"] = self._ci_get(target_payload, "text")
        else:
            # Fallback: copy supplied target fields verbatim
            target.update(self._case_insensitive_copy(target_payload))

    def _update_real_write_per_addr(self, command: Dict[str, Any], update: Dict[str, Any]) -> None:
        addr_values_payload = self._ci_get(update, "addrValues")
        data_format = (command.get("dataFormat") or "").strip()

        if not isinstance(addr_values_payload, list):
            raise ValueError(f"Per-address command {command.get('commandName')} requires addrValues list")

        existing_addr_values = self._ci_get(command, "addrValues")
        if not isinstance(existing_addr_values, list):
            existing_addr_values = []
            command["addrValues"] = existing_addr_values

        addr_lookup = {entry.get("addr"): entry for entry in existing_addr_values if isinstance(entry, dict)}

        for item in addr_values_payload:
            if not isinstance(item, dict):
                raise ValueError("Each addrValues item must be an object")

            addr = self._ci_get(item, "addr")
            if addr is None:
                raise ValueError("addrValues entry missing addr")

            value_payload = self._ci_get(item, "value") or self._ci_get(item, "Value")
            if not isinstance(value_payload, dict):
                raise ValueError(f"addr {addr} requires a value object")

            existing_entry = addr_lookup.get(addr)
            if existing_entry is None:
                existing_entry = {
                    "addr": addr,
                    "value": {"number": None, "text": None, "bits": {}},
                    "dirty": False,
                }
                existing_addr_values.append(existing_entry)
                addr_lookup[addr] = existing_entry

            value_container = self._ci_get(existing_entry, "value")
            if not isinstance(value_container, dict):
                value_container = {"number": None, "text": None, "bits": {}}
                existing_entry["value"] = value_container

            self._apply_value_update(value_container, value_payload, data_format)

            if self._ci_has_key(item, "dirty"):
                existing_entry["dirty"] = self._ci_get(item, "dirty")

    def _apply_value_update(self, container: Dict[str, Any], payload: Dict[str, Any], data_format: str) -> None:
        if data_format == "Numeric":
            number = self._ci_get(payload, "number")
            if number is None:
                raise ValueError("Numeric addr values require number field")
            container["number"] = number
            if self._ci_has_key(payload, "text"):
                container["text"] = self._ci_get(payload, "text")
            if self._ci_has_key(payload, "bits"):
                container["bits"] = self._ci_get(payload, "bits")
            return

        if data_format in {"BitField", "BitControl"}:
            bits_payload = self._ci_get(payload, "bits")
            if not isinstance(bits_payload, dict):
                raise ValueError("BitField addr values require bits map")

            existing_bits = self._ci_get(container, "bits")
            if not isinstance(existing_bits, dict):
                existing_bits = {}
            container["bits"] = existing_bits

            for bit_key, bit_value in bits_payload.items():
                effective_key = self._resolve_bit_key(existing_bits, bit_key)
                existing_bits[effective_key] = bit_value

            if self._ci_has_key(payload, "number"):
                container["number"] = self._ci_get(payload, "number")
            if self._ci_has_key(payload, "text"):
                container["text"] = self._ci_get(payload, "text")
            return

        # Default fallback assigns fields as-is
        container.update(self._case_insensitive_copy(payload))

    @staticmethod
    def _resolve_bit_key(bits: Optional[Dict[str, Any]], candidate: str) -> str:
        if not isinstance(candidate, str):
            return candidate  # type: ignore[return-value]

        if not isinstance(bits, dict):
            return candidate.upper()

        lowered = candidate.lower()
        for existing_key in bits.keys():
            if isinstance(existing_key, str) and existing_key.lower() == lowered:
                return existing_key

        return candidate.upper()

    @staticmethod
    def _case_insensitive_copy(source: Dict[str, Any]) -> Dict[str, Any]:
        result: Dict[str, Any] = {}
        for key, value in source.items():
            if isinstance(key, str):
                if isinstance(value, dict):
                    result[key.lower()] = AppState._case_insensitive_copy(value)
                else:
                    result[key.lower()] = value
            else:
                result[key] = value
        return result

    def _normalize_object_case(self, obj: Any) -> Any:
        if isinstance(obj, dict):
            normalized: Dict[str, Any] = {}
            for key, value in obj.items():
                new_key = key.lower() if isinstance(key, str) else key
                normalized[new_key] = self._normalize_object_case(value)
            return normalized
        if isinstance(obj, list):
            return [self._normalize_object_case(item) for item in obj]
        return obj

    @staticmethod
    def _ci_get(mapping: Any, key: str, default: Any = None) -> Any:
        if not isinstance(mapping, dict):
            return default

        lowered = key.lower()
        for existing_key, value in mapping.items():
            if isinstance(existing_key, str) and existing_key.lower() == lowered:
                return value
        return default

    @staticmethod
    def _ci_has_key(mapping: Any, key: str) -> bool:
        if not isinstance(mapping, dict):
            return False
        lowered = key.lower()
        return any(
            isinstance(existing_key, str) and existing_key.lower() == lowered
            for existing_key in mapping.keys()
        )

    @staticmethod
    def _ci_string(value: Any) -> Optional[str]:
        if isinstance(value, str):
            return value.strip().lower()
        return None

    @staticmethod
    def _ci_bool(mapping: Dict[str, Any], key: str) -> bool:
        value = AppState._ci_get(mapping, key)
        if isinstance(value, bool):
            return value
        if isinstance(value, str):
            lowered = value.lower()
            if lowered in {"true", "1", "yes", "on"}:
                return True
            if lowered in {"false", "0", "no", "off"}:
                return False
        if isinstance(value, (int, float)):
            return bool(value)
        return False

    @staticmethod
    def _rename_real_write_key(
        payload: Dict[str, Any],
        *,
        source_key: str,
        target_key: str,
    ) -> Dict[str, Any]:
        if source_key not in payload or source_key == target_key:
            return payload

        renamed: Dict[str, Any] = {}
        for key, value in payload.items():
            if key == source_key:
                renamed[target_key] = value
            elif key == target_key:
                # Skip the original target to avoid duplicates; source replaces it
                continue
            else:
                renamed[key] = value

        return renamed

    def _build_mfr_model_data(
        self,
        port: str,
        addr: str,
        group_index: Optional[int],
        length: int,
    ) -> List[int]:
        group0 = MFR_MODEL_ASCII["group0_y"]
        group1 = MFR_MODEL_ASCII["group1_y"]

        if (
            port == "MOD1"
            and addr in {"0", "1", "2", "3", "4"}
        ):
            if group_index == 0:
                base = group0
            else:
                base = group1
        else:
            if group_index == 0:
                base = group0
            else:
                base = group1

        padding = [32] * max(0, length - len(base))
        return base[:length] + padding

    def _validate_write_payload(self, payload: List[Dict[str, Any]]) -> None:
        inv_operation = self._command_store.get("INV_OPERATION")
        valid_addrs = set()
        if inv_operation and inv_operation.get("isPerAddr"):
            valid_addrs = set(inv_operation.get("addrValues", {}).keys())

        for item in payload:
            if item.get("commandName") != "INV_OPERATION":
                continue
            for pair in item.get("addrValues", []):
                addr = pair.get("addr")
                if addr not in valid_addrs:
                    raise ValueError(f"Invalid addr {addr} for INV_OPERATON")

    def _get_inv_status_raw_value(self, port: str) -> int:
        template_item = (
            self._inv_data_v1_1_template
            .get("values", {})
            .get("INV_STATUS")
        )
        if not template_item:
            return 0

        working_copy = json.loads(json.dumps(template_item))
        self._apply_inv_status_decode(working_copy, port)
        return int(working_copy.get("value", 0))

    def _apply_inv_status_decode(self, item: MutableMapping[str, Any], port: str) -> None:
        rules = item.get("rule") or []
        existing_decode = {
            entry.get("name"): entry.get("value")
            for entry in item.get("decode") or []
            if isinstance(entry, dict) and "name" in entry
        }

        overrides = INV_STATUS_DECODE_BY_PORT.get((port or "").upper(), {})
        bit_value = 0
        updated_decode = []

        for rule in rules:
            name = rule.get("name")
            bit = self._safe_int(rule.get("bit"), default=0)
            length = self._safe_int(rule.get("length"), default=1)
            length = max(1, length)

            value_map = rule.get("valueMap") or {}
            override_value = overrides.get(name)
            target_raw = self._resolve_inv_status_override(
                override_value,
                value_map,
                length,
            )

            if target_raw is None:
                if length == 1:
                    target_raw = 0
                else:
                    target_raw = self._resolve_value_map_key(
                        existing_decode.get(name),
                        value_map,
                        default=0,
                    )
            else:
                target_raw &= (1 << length) - 1

            mask = (1 << length) - 1
            bit_value &= ~(mask << bit)
            bit_value |= (target_raw & mask) << bit

            decode_text = value_map.get(str(target_raw))
            if decode_text is None:
                if length == 1:
                    decode_text = "True" if target_raw else "False"
                else:
                    decode_text = existing_decode.get(name)
                    if decode_text is None:
                        decode_text = "True" if target_raw else "False"

            updated_decode.append({"name": name, "value": decode_text})

        if updated_decode:
            item["value"] = bit_value
            item["decode"] = updated_decode

    @staticmethod
    def _resolve_inv_status_override(
        override: Any,
        value_map: Dict[str, Any],
        length: int,
    ) -> Optional[int]:
        if override is None:
            return None

        if length == 1:
            if isinstance(override, str):
                lowered = override.lower()
                if lowered in {"true", "on"}:
                    return 1
                if lowered in {"false", "off"}:
                    return 0
            return 1 if bool(override) else 0

        if isinstance(override, int):
            return override
        if isinstance(override, float):
            return int(override)
        if isinstance(override, str):
            return AppState._resolve_value_map_key(override, value_map, default=0)

        return None

    @staticmethod
    def _resolve_value_map_key(
        target: Any,
        value_map: Dict[str, Any],
        default: int = 0,
    ) -> int:
        if target is None:
            return default

        for raw_key, text in value_map.items():
            if text == target:
                try:
                    return int(raw_key)
                except (TypeError, ValueError):
                    continue

        if isinstance(target, str):
            lowered = target.lower()
            if lowered in {"true", "on"}:
                return 1
            if lowered in {"false", "off"}:
                return 0
            try:
                return int(target)
            except ValueError:
                return default

        if isinstance(target, (int, float)):
            return int(target)

        return default

    @staticmethod
    def _safe_int(value: Any, default: int = 0) -> int:
        try:
            return int(value)
        except (TypeError, ValueError):
            return default


__all__ = ["AppState", "LinkStatusCache"]
