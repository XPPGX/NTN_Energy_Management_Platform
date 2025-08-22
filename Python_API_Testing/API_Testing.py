
from flask import Flask, jsonify, request
import json, random
import sys
import time




app = Flask(__name__)
app.config["JSONIFY_PRETTYPRINT_REGULAR"] = False  # 少一點格式化開銷

# 啟動時就把 JSON 讀進來，拆成「不含 data 的模板」+「各自 data 長度」
with open("INV_DATA.json", "r", encoding="utf-8") as f:
    _base_data = json.load(f)

_TEMPLATES = [{k: v for k, v in item.items() if k != "data"} for item in _base_data]
_LENGTHS   = [len(item["data"]) for item in _base_data]

# 固定字串（先算好 ASCII），避免每次重複計算
_MFR_MODEL_G0 = [ord(c) for c in "NTN-5K"]   # groupIndex = 0
_MFR_MODEL_G1 = [ord(c) for c in "-248  "]   # groupIndex = 1（注意兩個空白）

@app.route("/api/memory/read-memory", methods=["GET"])
def read_memory():
    port = request.args.get("type", "Unknown")
    addr = request.args.get("addr", "0")

    resp = []
    for tpl, dlen in zip(_TEMPLATES, _LENGTHS):
        cmd = tpl.get("commandName")
        gix = tpl.get("groupIndex")

        # 預設：隨機 bytes
        data = [random.getrandbits(8) for _ in range(dlen)]

        # 特例：MFR_MODEL
        if cmd == "MFR_MODEL":
            if gix == 0:
                data = _MFR_MODEL_G0[:dlen] + [32] * max(0, dlen - len(_MFR_MODEL_G0))  # 不足補空白
            elif gix == 1:
                data = _MFR_MODEL_G1[:dlen] + [32] * max(0, dlen - len(_MFR_MODEL_G1))

        # 特例：INV_FAULT 固定回 [0, 0]
        if cmd == "INV_FAULT":
            fixed = [0, 0]
            data = fixed[:dlen] + [0] * max(0, dlen - len(fixed))  # 若長度不是 2，一樣安全處理

        # 特例：INV_STATUS 固定回 [1, 0]
        if cmd == "INV_STATUS":
            fixed = [1, 0]
            data = fixed[:dlen] + [0] * max(0, dlen - len(fixed))

        resp.append({**tpl, "data": data})

    return jsonify(resp), 200

if __name__ == "__main__":
    # 測速/實際部署建議關掉 debug reloader
    print(sys.executable)
    
    # print("delay...")
    # for countdown in range(10, 0, -1):
    #     time.sleep(1)
    #     print("countdown = {}".format(countdown))


    app.run(host="0.0.0.0", port=5050, debug=False)
