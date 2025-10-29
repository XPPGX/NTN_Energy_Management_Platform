from __future__ import annotations

from datetime import timedelta, timezone
from pathlib import Path

BASE_DIR = Path(__file__).resolve().parent.parent

API_MEMORY_DIR = Path("app") / "api" / "memory"
LINK_STATUS_DATA_DIR = API_MEMORY_DIR / "link_status" / "data"
READ_MEMORY_DATA_DIR = API_MEMORY_DIR / "read_memory" / "data"
READ_REAL_DATA_DIR = API_MEMORY_DIR / "read_real" / "data"
WRITE_MEMORY_DATA_DIR = API_MEMORY_DIR / "write_memory" / "data"
SETTING_RANGE_DIR = API_MEMORY_DIR / "setting_range" / "data"

INV_DATA_FILE = READ_MEMORY_DATA_DIR / "INV_DATA.json"
INV_DATA_V1_1_FILE = READ_REAL_DATA_DIR / "INV_DATA_V1-1.json"
REAL_READ_TEMPLATE_FILE = READ_REAL_DATA_DIR / "REAL_ReadMemory_JsonFormat.json"
LINK_STATUS_FILE = LINK_STATUS_DATA_DIR / "link_status.json"
DEFAULT_MEMORY_FILE = WRITE_MEMORY_DATA_DIR / "WriteMemory_JsonFormat.json"
DEFAULT_REAL_WRITE_FILE = WRITE_MEMORY_DATA_DIR / "REAL_WriteMemory_JsonFormat.json"
PARTITION_STATUS_FILE = SETTING_RANGE_DIR / "setting_range.json"
TZ_TAIPEI = timezone(timedelta(hours=8))
USE_RANDOM_DATA = True
LINK_STATUS_REFRESH_INTERVAL = 2.0  # seconds

V1_1_FIXED_CONFIG = {
    "READ_OP_LD_PCNT": False,
    "READ_OP_WATT": False,
    "MFR_MODEL": True,
    "INV_FAULT": True,
    "READ_VIN": False,
    "READ_IIN": False,
    "READ_FREQ": False,
    "INV_STATUS": True,
}

V1_1_FIXED_VALUES = {
    "INV_FAULT": 0,
    "INV_STATUS": 1,
}

MFR_MODEL_BY_PORT = {
    "CAN1": "NTN-5K-124  ",
    "CAN2": "NTN-5K-224  ",
    "MOD1": "NTN-5K-124  ",
    "MOD2": "NTN-5K-224  ",
}

MFR_MODEL_ASCII = {
    "group0_y": [ord(c) for c in "NTN-5K"],
    "group1_y": [ord(c) for c in "-124  "],
    "group0": [ord(c) for c in "\x00\x00\x00\x00\x00\x00"],
    "group1": [ord(c) for c in "\x00\x00\x00\x00\x00\x00"],
}

INV_STATUS_DECODE_BY_PORT = {
    "CAN1": {
        "INV_MODE": True,
    },
    "CAN2": {
        "BYPASS_MODE": True,
    },
    "MOD1": {
        "INV_MODE": True,
        "SAVING_MODE": True,
    },
    "MOD2": {
        "AC_OK": True,
    },
}
