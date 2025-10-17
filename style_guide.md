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

| 類型         | 放置位置                  | 範例                                     |
| ---------- | --------------------- | -------------------------------------- |
| 🎨 主題與顏色變數 | `app.css`             | `--accent-color`, `--bg-primary`       |
| 🧱 通用元件樣式  | `common-elements.css` | `.btn`, `.card`, `.custom-table`       |
| 📄 頁面專屬排版  | `*.razor.css`         | `.card-container-layout`, `.top-panel` |

### 📌 原則

1. **app.css** 僅定義變數，不放具體樣式。
2. **common-elements.css** 為組件庫，不直接綁定頁面。
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
2. **在 `common-elements.css` 新增基礎樣式**：

   * 背景、邊框、陰影、顏色。
3. **在各頁 CSS 中定義布局**：

   * 位置、排列方向、間距。
4. **測試主題相容性**：

   * 在暗/亮主題下切換檢查。

### 範例：新增「通知卡片」

```css
/* common-elements.css */
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

### 🧱 common-elements.css

（通用樣式）

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

| 類型       | 檔案                    | 內容                   |
| -------- | --------------------- | -------------------- |
| 🎨 顏色與主題 | `app.css`             | 定義主題變數（如背景、文字、強調色）   |
| 🧱 通用樣式  | `common-elements.css` | 定義按鈕、卡片外觀、陰影、顏色等視覺樣式 |
| 📄 排版樣式  | `MyPage.razor.css`    | 定義頁面布局（flex、gap、位置）  |
| ⚙️ 程式邏輯  | `MyPage.razor`        | 定義 Blazor 結構與互動邏輯    |

---

## ⚙️ 專案整合指引（給開發者與 Copilot GPT‑5‑Codex）

以下為讓 Copilot、AI 編碼助手與新開發者能準確套用主題框架的逐步整合說明：

### 🔹 Step 1：複製主題檔案

將以下兩個檔案加入專案：

```
wwwroot/css/app.css
wwwroot/css/common-elements.css
```

### 🔹 Step 2：在 `_Layout.cshtml` 或 `index.html` 中引入

```html
<link href="~/css/app.css" rel="stylesheet" />
<link href="~/css/common-elements.css" rel="stylesheet" />
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
* 通用組件樣式 (`common-elements.css`)
* 自訂頁面布局 (`*.razor.css`)
* Razor 控制互動邏輯（C#）

> 📘 **提示**：Codex 若偵測到 `.btn-`, `.card-`, `.theme-tab` 類別，可自動建議從 `common-elements.css` 引用樣式，確保一致的 UI。
