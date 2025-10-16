
from flask import Flask, jsonify, request
import json, random
import sys
import time
from datetime import datetime, timezone, timedelta


TZ_TAIPEI = timezone(timedelta(hours=8))

app = Flask(__name__)
app.config["JSONIFY_PRETTYPRINT_REGULAR"] = False  # 少一點格式化開銷

USE_RANDOM_DATA = True

# 啟動時就把 JSON 讀進來，拆成「不含 data 的模板」+「各自 data 長度」
with open("INV_DATA.json", "r", encoding="utf-8") as f:
    _base_data = json.load(f)

with open("link_status.json", "r", encoding="utf-8") as f:
    _link_status = json.load(f)

with open("REAL_ReadMemory_JsonFormat.json", "r", encoding="utf-8") as f:
    REAL_INV_TEMPLATE = json.load(f)


_TEMPLATES = [{k: v for k, v in item.items() if k != "data"} for item in _base_data]
_LENGTHS   = [len(item["data"]) for item in _base_data]

# 固定字串（先算好 ASCII），避免每次重複計算
_MFR_MODEL_G0_Y = [ord(c) for c in "NTN-5K"]   # groupIndex = 0
_MFR_MODEL_G1_Y = [ord(c) for c in "-124  "]   # groupIndex = 1（注意兩個空白）
_MFR_MODEL_G0 = [ord(c) for c in "\x00\x00\x00\x00\x00\x00"]   # groupIndex = 0
_MFR_MODEL_G1 = [ord(c) for c in "\x00\x00\x00\x00\x00\x00"]   # groupIndex = 1（注意兩個空白）

_COMMAND_STORE = {}
_REAL_WRITE_STORE = {}

def memory_init(filename: str):
    global _COMMAND_STORE
    
    with open(filename, "r", encoding="utf-8") as f:
        data = json.load(f)
    
    _COMMAND_STORE = {}

    for item in data:
        cmd = item.get("commandName")
        is_per_addr = item.get("isPerAddr", False)

        if not is_per_addr:
            _COMMAND_STORE[cmd] = {
                "isPerAddr" : False,
                "targetValue" : item.get("targetValue", [])
            }
        else:
            addr_dict = {}
            for pair in item.get("addrValues", []):
                addr = pair.get("addr")
                val = pair.get("value", [])
                addr_dict[addr] = val
            
            _COMMAND_STORE[cmd] = {
                "isPerAddr" : True,
                "addrValues" : addr_dict
            }

        print("Init finished. Commands loaded:")
    for k,v in _COMMAND_STORE.items():
        print(f"  {k} => {v}")

def real_write_memory_init(filename: str):
    """初始化 REAL_WRITE_STORE (用 REAL_WriteMemory_JsonFormat.json)"""
    global _REAL_WRITE_STORE
    with open(filename, "r", encoding="utf-8") as f:
        data = json.load(f)

    # data 格式是 { "NTN-5K_CAN.json": [ {...}, {...} ] }
    _REAL_WRITE_STORE = data
    print(f"Init finished. REAL_WRITE_STORE loaded: {list(_REAL_WRITE_STORE.keys())}")

def get_real_write_store():
    return _REAL_WRITE_STORE

@app.route("/api/memory/read-real", methods=["GET"])
def read_real():
    port = request.args.get("type", "Unknown")
    addr = int(request.args.get("addr", "0"))

    # 複製一份 template，避免修改到原始資料
    resp = json.loads(json.dumps(REAL_INV_TEMPLATE))

    # 更新固定欄位
    resp["port"] = port
    resp["addr"] = addr
    resp["timestamp"] = datetime.now(TZ_TAIPEI).isoformat()

    # 處理 values
    for key, item in resp["values"].items():
        vtype = item.get("type")

        if vtype == "Numeric":
            # 給一個隨機 double (範圍 0~1000，可自行調整)
            item["value"] = round(random.uniform(0, 1000), 2)

        elif vtype == "BitField":
            item["value"] = ""

        elif vtype == "ASCII":
            # 保留 JSON 裡原本寫的字串，不動
            pass

    return jsonify(resp), 200

@app.route("/api/memory/link-status", methods=["GET"])
def get_link_status():
    return jsonify(_link_status), 200

@app.route("/api/memory/read-memory", methods=["GET"])
def read_memory():
    port = request.args.get("type", "Unknown")
    addr = request.args.get("addr", "0")

    resp = []
    for tpl, dlen in zip(_TEMPLATES, _LENGTHS):
        cmd = tpl.get("commandName")
        gix = tpl.get("groupIndex")

        # 預設：隨機 bytes
        if USE_RANDOM_DATA is True:
            data = [random.getrandbits(8) for _ in range(dlen)]
        else:
            data = [0] * dlen
        # 特例：MFR_MODEL
        if cmd == "MFR_MODEL":
            if port == "MOD1" and (addr == "0" or addr == "1" or addr == "2" or addr == "3" or addr == "4"):
                if gix == 0:
                    data = _MFR_MODEL_G0_Y[:dlen] + [32] * max(0, dlen - len(_MFR_MODEL_G0_Y))  # 不足補空白
                elif gix == 1:
                    data = _MFR_MODEL_G1_Y[:dlen] + [32] * max(0, dlen - len(_MFR_MODEL_G1_Y))
                # if gix == 0:
                #     data = _MFR_MODEL_G0[:dlen] + [32] * max(0, dlen - len(_MFR_MODEL_G0))  # 不足補空白
                # elif gix == 1:
                #     data = _MFR_MODEL_G1[:dlen] + [32] * max(0, dlen - len(_MFR_MODEL_G1))
            else:
                if gix == 0:
                    data = _MFR_MODEL_G0_Y[:dlen] + [32] * max(0, dlen - len(_MFR_MODEL_G0_Y))  # 不足補空白
                elif gix == 1:
                    data = _MFR_MODEL_G1_Y[:dlen] + [32] * max(0, dlen - len(_MFR_MODEL_G1_Y))
            

        # 特例：INV_FAULT 固定回 [0, 0]
        if cmd == "INV_FAULT":
            fixed = [0, 0]
            data = fixed[:dlen] + [0] * max(0, dlen - len(fixed))  # 若長度不是 2，一樣安全處理

        # 特例：INV_STATUS 固定回 [1, 0]
        if cmd == "INV_STATUS":
            fixed = [1, 0]
            data = fixed[:dlen] + [0] * max(0, dlen - len(fixed))

        # 特例：READ_FAN1_SPEED 固定回 3450
        if cmd == "READ_FAN_SPEED_1":
            fixed_val = 3450
            data = [fixed_val & 0xFF, (fixed_val >> 8) & 0xFF]  
            data = data[:dlen] + [0] * max(0, dlen - len(data))

         # === READ_VIN: 隨機浮點數 199.0 ~ 201.0 ===
        if cmd == "READ_VIN":
            # 產生隨機浮點數，保留 1 位小數
            vin_value = round(random.uniform(109.0, 111.0), 1)

            # 換算成 raw integer (scaling=0.1 → ÷0.1)
            raw = int(vin_value / 0.1)

            # 轉成 2 bytes (little-endian)
            data = [raw & 0xFF, (raw >> 8) & 0xFF]
            data = data[:dlen] + [0] * max(0, dlen - len(data))
            

        resp.append({**tpl, "data": data})

    return jsonify(resp), 200

@app.route("/api/memory/write-memory", methods=["GET"])
def R_write_memory():
    port = request.args.get("type", None)
    fileName = request.args.get("protocolFileName", None)
    print(f'[R]/api/memory/write-memory : {port}, {fileName}')

    resp = []
    for cmd, info in _COMMAND_STORE.items():
        obj = {
            "commandName" : cmd,
            "isPerAddr" : info["isPerAddr"]
        }
        
        if info["isPerAddr"]:
            obj["addrValues"] = [
                {"addr" : addr, "value" : val}
                for addr, val in info["addrValues"].items()
            ]
        else:
            obj["targetValue"] = info.get("targetValue", [])
        
        resp.append(obj)

    return jsonify(resp), 200

@app.route("/api/memory/write-memory", methods=["POST"])
def W_write_memory():
    port = request.args.get("type", None)
    fileName = request.args.get("protocolFileName", None)
    print(f'[W]/api/memory/write-memory : {port}, {fileName}')

    global _COMMAND_STORE
    payload = request.get_json(force=True)

    if not isinstance(payload, list):
        return jsonify({"error" : "Body must be a JSON array"}), 400

    ## step 1: 檢查 INV_OPERATION
    for item in payload:
        if item.get("commandName") == "INV_OPERATION" :
            if "addrValues" in item:
                for pair in item["addrValues"]:
                    addr = pair.get("addr")
                    
                    if addr not in _COMMAND_STORE["INV_OPERATION"]["addrValues"]:
                        return jsonify({"status" : "failed",
                                        "reason" : f"Invalid addr {addr} for INV_OPERATON"}), 400
    ## step 2: 檢查通過，存值
    for item in payload:
        cmd = item.get("commandName")
        if cmd not in _COMMAND_STORE:
            continue

        if item.get("isPerAddr") is False:
            _COMMAND_STORE[cmd]["targetValue"] = item.get("targetValue", [])
        else:
            addr_dict = _COMMAND_STORE[cmd]["addrValues"]
            for pair in item.get("addrValues", []):
                addr = pair.get("addr")
                val = pair.get("value", [])
                if(addr in addr_dict):
                    addr_dict[addr] = val

    return jsonify({"status" : "ok",
                    "updated" : payload}), 200

@app.route("/api/memory/write-api", methods=["GET"])
def R_real_write_memory():
    port = request.args.get("type", None)
    fileName = request.args.get("protocolFileName", "NTN-5K_CAN.json")

    store = get_real_write_store()
    if not store:
        return jsonify({"error": "REAL_WRITE_STORE not initialized"}), 500

    if fileName not in store:
        return jsonify({"error": f"File {fileName} not found"}), 404

    print(f'[R]/api/memory/write-api : {port}, {fileName}')
    return jsonify(store), 200


#未完成，正常應該不是這樣存值的
@app.route("/api/memory/write-api", methods=["POST"])
def W_real_write_memory():
    port = request.args.get("type", None)
    fileName = request.args.get("protocolFileName", "NTN-5K_CAN.json")

    global _REAL_WRITE_STORE
    payload = request.get_json(force=True)

    if fileName not in _REAL_WRITE_STORE:
        return jsonify({"error": f"File {fileName} not found"}), 404

    if not isinstance(payload, list):
        return jsonify({"error": "Body must be a JSON array"}), 400

    # 覆蓋更新
    _REAL_WRITE_STORE[fileName] = payload

    print(f'[W]/api/memory/write-api : {port}, {fileName} updated')
    return jsonify({"status": "ok", "updated": payload}), 200

if __name__ == "__main__":
    # 測速/實際部署建議關掉 debug reloader
    print(sys.executable)

    memory_init("WriteMemory_JsonFormat.json")
    real_write_memory_init("REAL_WriteMemory_JsonFormat.json")  # ⭐ 新增
    app.run(host="0.0.0.0", port=5050, debug=False)
