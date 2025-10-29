# Python API 測試環境指引

> 本教學針對 Windows 本地開發情境，說明如何啟動位於 `Python_API_Testing/` 的模擬 API server，並使用 `venv` 管理相依套件。

---

## 1. 環境概觀

- **目的**：在開發機上快速啟動一個 Flask API server，讓 Blazor 專案可離線模擬與框架 API 的互動。
- **程式入口**：`API_Testing.py`（預設啟動於 `http://localhost:5050`）。
- **程式架構**：
   - `app/__init__.py`：建立 Flask 應用程式並註冊路由。
   - `app/state.py`：集中管理樣板資料、隨機值邏輯與快取。
   - `app/routes.py`：定義所有 API endpoint。
   - `app/config.py`：統一維護 JSON 路徑、常數與模式顏色等設定。
- **資料來源**：依 endpoint 分散於 `app/api/memory/<endpoint>/data/*.json`（例如 `link_status/data/link_status.json`），部分共用資料（如 `partition-status`）位於對應的 endpoint 目錄下。
- **未來擴充**：後續若有新 API，請調整 `app/routes.py`、對應服務方法（多數集中在 `app/state.py`），並視需要補充相對應的 JSON 來源；建議同時更新本文的「新增 / 調整 API」小節。

---

## 2. 先備作業

1. **安裝 Python（Windows）**
   - 從 [python.org](https://www.python.org/downloads/windows/) 下載最新版安裝程式。
   - 安裝時記得勾選「**Add Python to PATH**」。
   - 安裝完成後，在 PowerShell 或 CMD 內確認：

     ```powershell
     python --version
     ```

     出現版本號即可（不需要 `python3`）。

2. **設定工作目錄**

   ```powershell
   cd C:\code\CMU3\NTN_Energy_Management_Platform\Python_API_Testing
   ```

---

## 3. 使用 `venv` 管理套件

> 底下的指令皆使用 `python`（不需要 `python3`）。

1. **建立虛擬環境**

   ```powershell
   python -m venv .venv
   ```

   指令會在目前資料夾生成 `.venv/`。

2. **啟用虛擬環境**

   ```powershell
   .\.venv\Scripts\activate
   ```

   - 成功後，提示字首會出現 `(.venv)`。
   - 若出現執行原則限制，可先在 PowerShell 以系統管理員身份執行：

     ```powershell
     Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
     ```

3. **安裝相依套件**

   ```powershell
   python -m pip install --upgrade pip
   python -m pip install -r requirements.txt
   ```

4. **停用虛擬環境（離開時）**

   ```powershell
   deactivate
   ```

---

## 4. 啟動模擬 API server

1. 確認虛擬環境已啟用（提示字首帶有 `(.venv)`）。
2. 執行程式：

   ```powershell
   python API_Testing.py
   ```

3. 看到類似下列輸出表示成功：

   ```text
   C:\...\.venv\Scripts\python.exe
   Init finished. Commands loaded:
     ...
   * Running on http://0.0.0.0:5050
   ```

   - 預設會綁定到所有介面（0.0.0.0），Blazor 端可透過 `http://localhost:5050` 連線。
   - 按 `Ctrl + C` 可停止服務。

---

## 5. 既有 API 清單（節錄）

| 路徑                               | 方法 | 說明                    |
| ---------------------------------- | ---- | ----------------------- |
| `/api/memory/link-status`          | GET  | 讀取 `link_status/data/link_status.json` |
| `/api/memory/partition-status`     | GET  | 讀取 `setting_range/setting_range.json` |
| `/api/memory/read-memory`          | GET  | 隨機回傳記憶體指令資料   |
| `/api/memory/read-real`            | GET  | 回傳實際量測範例資料     |
| `/api/memory/write-memory`         | GET  | 取得可寫入的命令         |
| `/api/memory/write-memory`         | POST | 更新命令模擬值           |
| `/api/memory/write-api`            | GET  | 取得 write API 模擬資料  |
| `/api/memory/write-api`            | POST | 覆蓋 write API 模擬資料  |

> 更多細節可直接閱讀 `app/state.py`（資料計算與快取）與 `app/routes.py`（路由定義）的程式註解。

---

## 6. 新增 / 調整 API 的建議流程

1. **定義資料來源**：在對應的 `app/api/memory/<endpoint>/data/` 目錄內新增或調整 JSON。
2. **撰寫邏輯**：
   - 在 `app/state.py` 增加或調整對應的資料處理函式。
   - 如需新的常數或設定，統一定義在 `app/config.py`。
3. **註冊路由**：在 `app/routes.py` 加入新的 Flask route，呼叫前一步定義的函式。
4. **更新文件**：
   - 若有新的相依套件，記得更新 `requirements.txt`。
   - 補充或調整本文件的 API 清單與操作說明。
5. **重啟 server**：程式會依最新程式碼載入，Blazor 端即可測試新 API。

---

## 7. 常見問題

| 問題                                         | 解法                                                                 |
| -------------------------------------------- | -------------------------------------------------------------------- |
| 啟用 `venv` 出現「執行原則」錯誤                | 依上文使用 `Set-ExecutionPolicy` 放寬權限，再重新啟用 `venv`。          |
| `pip` 安裝失敗或網路受限                       | 先執行 `python -m pip install --upgrade pip`，必要時設定代理或改離線安裝。 |
| Blazor 連不到 API server                      | 確認 API 是否啟動、Port 是否為 5050，或檢查防火牆設定。                 |
| 需要使用最新版 API 或資料格式                  | 更新相應 JSON / 程式碼後重啟 server，並同步調整 Blazor 端的解析邏輯。      |

---

若有其他操作情境，歡迎在提交新 API 時一併補充此文件，維持團隊知識的一致性。
