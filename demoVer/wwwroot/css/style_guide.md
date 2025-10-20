# 🎨 CMU3 UI Theme Style Guide

> Internal design guide for consistent component styling and maintainable theme expansion.

---

## 🧭 1. 核心理念

本指南的目標是確保 **一致性、可擴充性與主題相容性**：

* 所有元件皆應可自動支援暗色與亮色主題。
* 新增樣式時，需盡可能使用現有變數（`--bg-*`, `--text-*`, `--accent-*`）。
* 視覺樣式（顏色、陰影）與結構樣式（flex、grid）應分離。

---

## 🧩 2. CSS 結構原則

| 類型         | 放置位置             | 範例                                     |
| ------------ | -------------------- | ---------------------------------------- |
| 🎨 主題與顏色變數 | `app.css`            | `--accent-color`, `--bg-primary`         |
| 🧱 通用元件模組  | `common-*.css`       | `.btn-primary`, `.card-container-base`, `.custom-table` |
| 📄 頁面專屬排版  | `*.razor.css`        | `.card-container-layout`, `.top-panel`   |

### 📚 通用模組對照表

- `common-elements.css`：提供 `.no-flex`、`label`、`input` 等基礎表單樣式與工具類別。
- `common-button.css`：集中管理 `.btn-*` 系列按鈕與 `.custom-toggle-button`。
- `common-card.css`：定義 `.card`、`.detail-card`、`.jumpInfo-card`、`.card-container-base` 等卡片樣式。
- `common-table.css`：收錄 `.custom-table`、`.card-table` 與行動版表格樣式。
- `common-select.css`：涵蓋 `<select>`、`.column-chooser`、`.multi-option`、`.radio-group`。
- `common-slider.css`：提供 `.custom-slider` 與 `.responsive-slider`。
- `common-menu.css`：提供 `.custom-menu` 與 `.custom-menu-item`。
- `common-tab.css`：包含 `.theme-tab`、`.theme-tab-panel`。

### 📌 原則

1. **app.css** 僅定義變數，不放具體樣式。
2. **common-*.css** 模組負責視覺造型，依元件類型拆分管理；新增樣式時選擇對應檔案。
3. **頁面 CSS** 僅負責位置與尺寸，不改動配色。

---

## 🎨 3. 命名規範

### 通用規則

* 使用小寫、以連字號（`-`）分隔，例如：`card-container-base`
* 若為狀態樣式，使用修飾詞：`.active`, `.disabled`, `.hover`
* 若為語意組件：`.btn-*`, `.card-*`, `.table-*`

### 類別前綴建議

| 前綴         | 用途                           |
| ---------- | ---------------------------- |
| `.btn-`    | 按鈕樣式                         |
| `.card-`   | 卡片樣式                         |
| `.table-`  | 表格樣式                         |
| `.multi-`  | 多選項樣式                        |
| `.radio-`  | 單選樣式                         |
| `.theme-`  | 主題相容樣式                       |
| `.custom-` | 自訂互動元件（slider, menu, toggle） |

---

## 🧱 4. 新增組件步驟

1. **設計元件結構**：決定 HTML 架構與交互邏輯。
2. **在 `wwwroot/css` 下選擇對應的 `common-*.css` 模組新增視覺樣式**：

    * 例如：按鈕放入 `common-button.css`、卡片放入 `common-card.css`、表格放入 `common-table.css`，若為表單基礎樣式則放在 `common-elements.css`。
3. **在各頁 CSS 中定義布局**：

   * 位置、排列方向、間距。
4. **測試主題相容性**：

   * 在暗/亮主題下切換檢查。

### 範例：新增「通知卡片」

```css
/* common-card.css */
.notification-card {
    background: var(--bg-secondary);
    border: 1px solid var(--border-color);
    color: var(--text-primary);
    box-shadow: 0 4px 12px var(--shadow-color);
}

/* MyPage.razor.css */
.notification-container {
    display: flex;
    flex-direction: column;
    gap: 12px;
    width: 100%;
}
```

---

## 🌈 5. Razor 整合範例：按鈕 + 卡片 + C# 邏輯

此範例展示如何同時結合顏色（`app.css`）、排版（`MyPage.razor.css`）與 C# 互動邏輯（`MyPage.razor`）。

### 🧩 app.css

（定義主題變數）

```css
:root {
    --accent-color: #6fc7ff;
    --bg-primary: #121212;
    --bg-secondary: #1e1e1e;
    --text-primary: #ffffff;
}
```

### 🧱 common-button.css

（通用按鈕樣式）

```css
.btn-primary {
    background: var(--accent-color);
    color: var(--bg-primary);
    border: 1px solid var(--accent-color);
    border-radius: 6px;
    padding: 10px 16px;
    cursor: pointer;
    transition: 0.3s;
}

.btn-primary:hover {
    background: var(--accent-hover);
}
```

### 🃏 common-card.css

（通用卡片樣式）

```css
.info-card {
    background: var(--bg-secondary);
    border: 1px solid var(--border-color);
    color: var(--text-primary);
    border-radius: 8px;
    padding: 1rem;
    box-shadow: 0 4px 10px var(--shadow-color);
}
```

### 📄 MyPage.razor.css

（頁面排版）

```css
.page-container {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 20px;
    padding: 2rem;
}

.info-card {
    width: 60%;
    text-align: center;
}
```

### ⚙️ MyPage.razor

（Blazor 元件結構 + C# 邏輯）

```razor
@page "/demo"

<div class="page-container">
    <div class="info-card">
        <h3>@Message</h3>
        <button class="btn-primary" @onclick="ChangeMessage">更換訊息</button>
    </div>
</div>

@code {
    private string Message = "這是預設訊息";

    private void ChangeMessage()
    {
        Message = $"更新時間：{DateTime.Now:HH:mm:ss}";
    }
}
```

📂 **檔案對應說明：**

| 類型       | 檔案                                      | 內容                                   |
| ---------- | ----------------------------------------- | -------------------------------------- |
| 🎨 顏色與主題 | `app.css`                                 | 定義主題變數（如背景、文字、強調色）         |
| 🧱 基礎樣式  | `common-elements.css`                   | 定義表單元件、工具類別（label、input、.no-flex） |
| 🧱 元件樣式  | `common-button.css`, `common-card.css` | 定義按鈕、卡片外觀、陰影、顏色等視覺樣式       |
| 📄 排版樣式  | `MyPage.razor.css`                      | 定義頁面布局（flex、gap、位置）            |
| ⚙️ 程式邏輯  | `MyPage.razor`                          | 定義 Blazor 結構與互動邏輯              |

---

## ⚙️ 專案整合指引（給開發者與 Copilot GPT‑5‑Codex）

以下為讓 Copilot、AI 編碼助手與新開發者能準確套用主題框架的逐步整合說明：

### 🔹 Step 1：複製主題檔案

確保 `wwwroot/css/` 底下的主題與共用模組檔案已納入專案（至少包含 `app.css`、`common-elements.css`、`common-button.css`、`common-card.css`，其餘 `common-*.css` 依實際使用情境加入）。

### 🔹 Step 2：在 `_Layout.cshtml` 或 `index.html` 中引入

```html
<link href="~/css/app.css" rel="stylesheet" />
<link href="~/css/common-elements.css" rel="stylesheet" />
<link href="~/css/common-button.css" rel="stylesheet" />
<link href="~/css/common-card.css" rel="stylesheet" />
<!-- 依需要再引入 common-table.css、common-select.css、common-tab.css... -->
```

### 🔹 Step 3：建立 Razor 頁面

範例檔案結構：

```
Pages/
 ├── MyPage.razor
 ├── MyPage.razor.css
```

`MyPage.razor` 可使用以下範例：

```razor
<div class="page-container">
    <div class="info-card">
        <h3>@Title</h3>
        <button class="btn-primary" @onclick="ChangeTheme">切換主題</button>
    </div>
</div>

@code {
    private string Title = "主題切換範例";

    private void ChangeTheme()
    {
        JS.InvokeVoidAsync("setThemePreference", "light");
    }
}
```

### 🔹 Step 4：建立對應的 CSS 排版檔

```css
/* MyPage.razor.css */
.page-container {
    display: flex;
    justify-content: center;
    align-items: center;
    height: 100vh;
}
```

### 🔹 Step 5：切換主題（選用）

```js
document.body.classList.add("theme-light");
```

### ✅ 完成後效果

* 自動套用全域主題變數 (`app.css`)
* 通用組件樣式（`common-elements.css` + 對應的 `common-*.css` 模組）
* 自訂頁面布局 (`*.razor.css`)
* Razor 控制互動邏輯（C#）

> 📘 **提示**：Codex 若偵測到 `.btn-`, `.card-`, `.theme-tab` 類別，可自動建議引用 `common-button.css`、`common-card.css`、`common-tab.css` 等模組，確保一致的 UI。
