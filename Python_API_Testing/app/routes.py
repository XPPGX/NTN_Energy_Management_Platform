from __future__ import annotations

from flask import Blueprint, jsonify, request

from .state import AppState


def create_routes(state: AppState) -> Blueprint:
    bp = Blueprint("memory", __name__)

    @bp.get("/api/memory/read-real")
    def read_real():
        port = request.args.get("type", "Unknown")
        addr = int(request.args.get("addr", "0"))
        payload = state.build_read_real_response(port, addr)
        return jsonify(payload), 200

    @bp.get("/api/memory/link-status")
    def link_status():
        data = state.get_link_status()
        if data is None:
            return jsonify({"error": "link_status not available"}), 500
        return jsonify(data), 200

    @bp.get("/api/memory/partition-status")
    def partition_status():
        port = request.args.get("type")
        protocol = request.args.get("protocol")
        if not port or not protocol:
            return jsonify({"error": "type and protocol are required"}), 400

        try:
            payload = state.get_partition_status(port, protocol)
        except ValueError as exc:
            return jsonify({"error": str(exc)}), 404

        return jsonify(payload), 200

    @bp.get("/api/memory/read-memory")
    def read_memory():
        port = request.args.get("type", "Unknown")
        addr = request.args.get("addr", "0")
        payload = state.build_read_memory_response(port, addr)
        return jsonify(payload), 200

    @bp.get("/api/memory/write-memory")
    def read_write_memory():
        port = request.args.get("type", None)
        file_name = request.args.get("protocolFileName", None)
        print(f"[R]/api/memory/write-memory : {port}, {file_name}")
        payload = state.build_write_memory_snapshot()
        return jsonify(payload), 200

    @bp.post("/api/memory/write-memory")
    def write_write_memory():
        port = request.args.get("type", None)
        file_name = request.args.get("protocolFileName", None)
        print(f"[W]/api/memory/write-memory : {port}, {file_name}")

        payload = request.get_json(force=True)
        if not isinstance(payload, list):
            return jsonify({"error": "Body must be a JSON array"}), 400

        try:
            state.apply_write_memory(payload)
        except ValueError as exc:
            return jsonify({"status": "failed", "reason": str(exc)}), 400

        return jsonify({"status": "ok", "updated": payload}), 200

    @bp.get("/api/memory/write-api")
    def read_real_write():
        port = request.args.get("type")
        try:
            payload = state.get_real_write_payload(port)
        except ValueError as exc:
            return jsonify({"error": str(exc)}), 500

        print(f"[R]/api/memory/write-api : {port}")
        return jsonify(payload), 200

    @bp.post("/api/memory/write-api")
    def write_real_write():
        port = request.args.get("type", None)
        file_name = request.args.get("protocolFileName", "NTN-5K_CAN.json")

        payload = request.get_json(force=True)
        if not isinstance(payload, list):
            return jsonify({"error": "Body must be a JSON array"}), 400

        try:
            state.update_real_write_store(file_name, payload)
        except KeyError as exc:
            return jsonify({"error": str(exc)}), 404

        print(f"[W]/api/memory/write-api : {port}, {file_name} updated")
        return jsonify({"status": "ok", "updated": payload}), 200

    return bp


__all__ = ["create_routes"]
