# BottomRow 功能遷移指南

## 📋 概述

BottomRow 是一個可配置的底部導航欄功能，允許用戶自定義 4 個快捷按鈕的名稱和路由。包含以下特性：

- ✅ 4 個可自定義的導航按鈕
- ✅ 設置頁面，帶有 Modal 確認對話框
- ✅ Toast 通知（3 秒自動消失）
- ✅ 加載動畫指示器
- ✅ 響應式設計（支持手機螢幕）
- ✅ 事件驅動的實時更新
- ✅ Deep Copy 防止意外數據修改
- ✅ JSON 持久化存儲

---

## 📁 相關文件清單

### 1. 核心服務
- **`Services/BottomRowConfig.cs`** (289 行)
  - 用途：核心配置管理服務
  - 功能：文件讀寫、數據驗證、事件觸發、Deep Copy

### 2. UI 組件
- **`Components/Shared/BottomRow/BottomRow.razor`** (106 行)
  - 用途：底部導航欄顯示組件
  - 功能：訂閱配置更新、路由導航、響應式顯示

- **`Components/Shared/BottomRow/BottomRow.razor.css`** (119 行)
  - 用途：底部導航欄樣式
  - 功能：深色/淺色主題、響應式布局、動畫效果

### 3. 設置頁面
- **`Components/Pages/Shortcut_setting.razor`** (385 行)
  - 用途：快捷鍵設置頁面
  - 功能：Modal 對話框、Toast 通知、加載動畫、表單驗證

- **`Components/Pages/Shortcut_setting.razor.css`** (301 行)
  - 用途：設置頁面樣式
  - 功能：Modal、Toast、響應式設計

### 4. 數據存儲
- **`UserSetting/5040/BottomRowConfig.json`**
  - 格式：JSON 數組
  - 內容：4 個按鈕的配置信息

### 5. 依賴模型
- **`Models/cardInfo.cs`** (包含 `PAGE` 枚舉)
  - 用途：頁面路由枚舉定義

---

## 🏗️ 架構說明

### 數據模型

```csharp
// 在 BottomRowConfig.cs 中定義
public class BottomRowItem
{
    public int Id { get; set; }                    // 按鈕編號 (1-4)
    public string DisplayName { get; set; }        // 顯示名稱（最多 7 字元）
    public string RouteName { get; set; }          // 路由名稱（對應 PAGE 枚舉）
    
    public BottomRowItem() { }
    
    public BottomRowItem(int id, string displayName, string routeName)
    {
        Id = id;
        DisplayName = displayName;
        RouteName = routeName;
    }
}
```

### PAGE 枚舉定義

```csharp
// 在 Models/cardInfo.cs 中定義
public enum PAGE
{
    HOME            = 1,
    BATTERY_SETTING = 2,
    INV_SETTING     = 3,
    LINK_STATUS     = 4,
    LOG             = 5,
    USE_STRATEGY    = 6,
    SHORTCUT_SETTING = 7,
}
```

### 配置結果枚舉

```csharp
// 在 BottomRowConfig.cs 中定義
public enum ConfigResult
{
    Success,
    NoChange,
    SaveFileFailed,
    RestoreDefaultFailed
}
```

---

## 🚀 遷移步驟

### 步驟 1：複製核心文件

將以下文件複製到新專案：

```plaintext
Services/
  └── BottomRowConfig.cs

Components/
  ├── Shared/
  │   └── BottomRow/
  │       ├── BottomRow.razor
  │       └── BottomRow.razor.css
  └── Pages/
      ├── Shortcut_setting.razor
      └── Shortcut_setting.razor.css

Models/
  └── cardInfo.cs  (只需要 PAGE 枚舉部分)
```

### 步驟 2：註冊服務 (Program.cs)

在 `Program.cs` 的 `builder.Services` 區塊加入：

```csharp
builder.Services.AddSingleton<BottomRowConfig>();
```

**完整上下文位置**（第 87 行附近）：
```csharp
builder.Services.AddSingleton<CircuitsWatcher>();
builder.Services.AddSingleton<CircuitHandler>(sp => sp.GetRequiredService<CircuitsWatcher>());

builder.Services.AddSingleton<BottomRowConfig>();  // ← 加在這裡

//根據作業系統自動切換 API Server IP 與 Port
builder.Services.AddHttpClient("ApiClient", client =>
{
    // ...
});
```

### 步驟 3：整合到 MainLayout

在 `Components/Layout/MainLayout.razor` 中：

**3.1 引用 BottomRow 組件**

```razor
@inherits LayoutComponentBase
@using demoVer.Shared  
@rendermode InteractiveServer

<CascadingValue Value="this" Name="MainLayout">
    <div class="page">
        <main>
            <TopRow @ref="_topRow" />
            
            <div class="content">
                @Body
            </div>
            
            <BottomRow @ref="_bottomRow" />  <!-- ← 加在這裡 -->
        </main>
    </div>
</CascadingValue>

@code {
    private BottomRow? _bottomRow;  // ← 宣告參考
    
    // 其他程式碼...
}
```

**3.2 加入 HideBottomRowMenu 方法**（可選，用於點擊外部關閉選單）

```csharp
@code {
    private BottomRow? _bottomRow;
    
    public void HideBottomRowMenu()
    {
        _bottomRow?.HideMenuDropdown();
    }
    
    private void HandleGlobalClick()
    {
        HideBottomRowMenu();
    }
}
```

### 步驟 4：創建存儲目錄和默認配置

**4.1 Windows 環境**
```plaintext
UserSetting/5040/BottomRowConfig.json
```

**4.2 Linux 環境**
```plaintext
/userdata/CMU3/UserSetting/5040/BottomRowConfig.json
```

**4.3 默認配置內容**

```json
[
  {
    "Id": 1,
    "DisplayName": "電池設定",
    "RouteName": "BATTERY_SETTING"
  },
  {
    "Id": 2,
    "DisplayName": "變流器設定",
    "RouteName": "INV_SETTING"
  },
  {
    "Id": 3,
    "DisplayName": "使用策略",
    "RouteName": "USE_STRATEGY"
  },
  {
    "Id": 4,
    "DisplayName": "快捷設定",
    "RouteName": "SHORTCUT_SETTING"
  }
]
```

### 步驟 5：路由配置

確保以下路由頁面存在：

```csharp
// 在對應的 .razor 檔案中
@page "/battery_setting"    // 電池設定
@page "/inv_setting"         // 變流器設定
@page "/use_strategy"        // 使用策略
@page "/shortcut_setting"    // 快捷設定
@page "/home"                // 首頁
@page "/link_status"         // 連線狀態
@page "/log"                 // 日誌
```

---

## 🎨 樣式系統說明

### CSS 變數定義

BottomRow 使用 CSS 變數來支持深色/淺色主題：

```css
/* 在 MainLayout.razor.css 或 app.css 中定義 */
:root {
    --bottom-row-bg: #1e1e1e;           /* 底部欄背景 */
    --bottom-row-text: #e0e0e0;         /* 文字顏色 */
    --bottom-row-hover-bg: #2a2a2a;     /* 懸停背景 */
    --bottom-row-active-bg: #0078d4;    /* 激活背景 */
    --bottom-row-border: #444;          /* 邊框顏色 */
}

.light-theme {
    --bottom-row-bg: #f5f5f5;
    --bottom-row-text: #333;
    --bottom-row-hover-bg: #e8e8e8;
    --bottom-row-active-bg: #0078d4;
    --bottom-row-border: #ddd;
}
```

### 響應式斷點

```css
/* 大螢幕（> 1200px）：顯示底部欄 */
@media (min-width: 1200px) {
    .bottom-row { display: flex; }
}

/* 平板（768px - 1199px）：隱藏底部欄 */
@media (max-width: 1199px) {
    .bottom-row { display: none; }
}

/* Modal 響應式 */
@media (max-width: 768px) {
    .modal-content {
        width: 90%;
        padding: 20px;
    }
}

@media (max-width: 480px) {
    .modal-content {
        width: 95%;
        padding: 15px;
    }
}
```

---

## 🔧 核心功能使用指南

### 1. 在組件中訂閱配置變更

```csharp
@inject BottomRowConfig BottomRowConfig
@implements IDisposable

@code {
    protected override void OnInitialized()
    {
        // 訂閱配置變更事件
        BottomRowConfig.OnConfigChanged += HandleConfigChanged;
        LoadBottomRowItems();
    }
    
    private void HandleConfigChanged()
    {
        InvokeAsync(() =>
        {
            LoadBottomRowItems();
            StateHasChanged();
        });
    }
    
    private void LoadBottomRowItems()
    {
        // 使用 Deep Copy 快照避免意外修改
        var items = BottomRowConfig.Get_UserDefinedList_snapshot();
        // 處理數據...
    }
    
    public void Dispose()
    {
        // 取消訂閱
        BottomRowConfig.OnConfigChanged -= HandleConfigChanged;
    }
}
```

### 2. 保存配置

```csharp
private async Task SaveSettings()
{
    var newItems = new List<BottomRowItem>
    {
        new BottomRowItem(1, "電池設定", "BATTERY_SETTING"),
        new BottomRowItem(2, "變流器設定", "INV_SETTING"),
        new BottomRowItem(3, "使用策略", "USE_STRATEGY"),
        new BottomRowItem(4, "快捷設定", "SHORTCUT_SETTING")
    };
    
    var result = await BottomRowConfig.UpdateAndSave(newItems);
    
    switch (result)
    {
        case ConfigResult.Success:
            // 保存成功，觸發 OnConfigChanged 事件
            ShowToast("設定已儲存");
            break;
        case ConfigResult.NoChange:
            ShowToast("沒有任何變更");
            break;
        case ConfigResult.SaveFileFailed:
            ShowToast("儲存失敗");
            break;
    }
}
```

### 3. 還原默認設定

```csharp
private async Task ResetToDefault()
{
    var result = await BottomRowConfig.RestoreDefault();
    
    if (result == ConfigResult.Success)
    {
        ShowToast("已還原預設值");
    }
    else
    {
        ShowToast("還原失敗");
    }
}
```

### 4. 路由導航

```csharp
@inject NavigationManager NavigationManager

private void NavigateToPage(string routeName)
{
    var route = routeName.ToLower().Replace('_', '-');
    NavigationManager.NavigateTo($"/{route}");
}
```

---

## 🎯 Modal 對話框實現

### HTML 結構

```razor
@if (showModal)
{
    <div class="modal-backdrop" @onclick="CancelAction">
        <div class="modal-content" @onclick:stopPropagation="true">
            <h3>@modalTitle</h3>
            <p>@modalMessage</p>
            <div class="modal-buttons">
                <button class="btn-confirm" @onclick="ConfirmAction">
                    確定
                </button>
                <button class="btn-cancel" @onclick="CancelAction">
                    取消
                </button>
            </div>
        </div>
    </div>
}
```

### C# 邏輯

```csharp
@code {
    private bool showModal = false;
    private string modalTitle = "";
    private string modalMessage = "";
    private ModalType currentModalType = ModalType.None;
    
    private enum ModalType
    {
        None,
        Save,
        Reset
    }
    
    private void ShowConfirmModal(ModalType type)
    {
        currentModalType = type;
        
        if (type == ModalType.Save)
        {
            modalTitle = "確認儲存";
            modalMessage = "確定要儲存嗎？";
        }
        else if (type == ModalType.Reset)
        {
            modalTitle = "確認還原";
            modalMessage = "確定要還原預設嗎？";
        }
        
        showModal = true;
    }
    
    private async Task ConfirmAction()
    {
        showModal = false;
        
        if (currentModalType == ModalType.Save)
        {
            await SaveSettings();
        }
        else if (currentModalType == ModalType.Reset)
        {
            await ResetToDefault();
        }
        
        currentModalType = ModalType.None;
    }
    
    private void CancelAction()
    {
        showModal = false;
        currentModalType = ModalType.None;
    }
}
```

---

## 🔔 Toast 通知實現

### HTML 結構

```razor
@if (showToast)
{
    <div class="toast-container">
        <div class="toast @toastClass">
            @toastMessage
        </div>
    </div>
}
```

### C# 邏輯

```csharp
@code {
    private bool showToast = false;
    private string toastMessage = "";
    private string toastClass = "";
    private System.Threading.Timer? toastTimer;
    
    private void ShowToast(string message, bool isSuccess = true)
    {
        toastMessage = message;
        toastClass = isSuccess ? "toast-success" : "toast-error";
        showToast = true;
        
        // 取消舊計時器
        toastTimer?.Dispose();
        
        // 3 秒後自動隱藏
        toastTimer = new System.Threading.Timer(_ =>
        {
            InvokeAsync(() =>
            {
                showToast = false;
                StateHasChanged();
            });
        }, null, 3000, Timeout.Infinite);
        
        StateHasChanged();
    }
    
    public void Dispose()
    {
        toastTimer?.Dispose();
    }
}
```

---

## ⚙️ 加載動畫實現

### HTML 結構

```razor
<button class="btn-save" @onclick="() => ShowConfirmModal(ModalType.Save)">
    @if (isSaving)
    {
        <span class="spinner-border spinner-border-sm me-2"></span>
    }
    儲存設定
</button>
```

### C# 邏輯

```csharp
@code {
    private bool isSaving = false;
    private bool isResetting = false;
    
    private async Task SaveSettings()
    {
        isSaving = true;
        StateHasChanged();
        
        try
        {
            // 執行保存操作
            var result = await BottomRowConfig.UpdateAndSave(editableItems);
            ShowToast(result == ConfigResult.Success ? "設定已儲存" : "儲存失敗");
        }
        finally
        {
            isSaving = false;
            StateHasChanged();
        }
    }
}
```

---

## 📝 輸入驗證

### HTML maxlength 限制

```razor
<input type="text"
       class="form-control"
       @bind="editableItems[0].DisplayName"
       maxlength="@BottomRowConfig._MaxDisplayNameLength"
       placeholder="快捷1名稱（最多7字）" />
```

### 後端驗證

```csharp
// 在 BottomRowConfig.cs 中
public const int _MaxDisplayNameLength = 7;

private bool IsValidConfig(List<BottomRowItem> items)
{
    if (items == null || items.Count != 4)
        return false;
    
    foreach (var item in items)
    {
        if (string.IsNullOrWhiteSpace(item.DisplayName) ||
            item.DisplayName.Length > _MaxDisplayNameLength)
        {
            return false;
        }
        
        if (!IsValidRouteName(item.RouteName))
        {
            return false;
        }
    }
    
    return true;
}
```

---

## 🐛 常見問題排除

### 問題 1：配置文件讀取失敗

**症狀**：啟動時顯示默認配置而不是用戶配置

**原因**：文件路徑錯誤或權限不足

**解決方案**：
```csharp
// 確認路徑正確
private string GetConfigFilePath()
{
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        return Path.Combine(Directory.GetCurrentDirectory(), 
                           "UserSetting", "5040", "BottomRowConfig.json");
    }
    else
    {
        return "/userdata/CMU3/UserSetting/5040/BottomRowConfig.json";
    }
}

// 確保目錄存在
private void EnsureDirectoryExists()
{
    var directory = Path.GetDirectoryName(_configFilePath);
    if (!Directory.Exists(directory))
    {
        Directory.CreateDirectory(directory);
    }
}
```

### 問題 2：OnConfigChanged 事件未觸發

**症狀**：保存配置後，BottomRow 沒有更新

**原因**：忘記訂閱事件或未正確取消訂閱

**解決方案**：
```csharp
// 確保實現 IDisposable
@implements IDisposable

@code {
    protected override void OnInitialized()
    {
        BottomRowConfig.OnConfigChanged += HandleConfigChanged;
    }
    
    public void Dispose()
    {
        BottomRowConfig.OnConfigChanged -= HandleConfigChanged;  // ← 重要！
    }
}
```

### 問題 3：Deep Copy 失敗導致意外修改

**症狀**：在設置頁面修改未保存，但 BottomRow 已經顯示新值

**原因**：使用 `ToList()` 只複製了列表，內部對象仍是同一參考

**解決方案**：
```csharp
// ❌ 錯誤：淺拷貝
public List<BottomRowItem> Get_UserDefinedList_snapshot()
{
    return _userDefinedList.ToList();
}

// ✅ 正確：深拷貝
public List<BottomRowItem> Get_UserDefinedList_snapshot()
{
    return _userDefinedList.Select(item => 
        new BottomRowItem(item.Id, item.DisplayName, item.RouteName)
    ).ToList();
}
```

### 問題 4：Modal 在手機上顯示異常

**症狀**：Modal 對話框在小螢幕上超出邊界

**原因**：未設置響應式樣式

**解決方案**：
```css
@media (max-width: 768px) {
    .modal-content {
        width: 90%;
        padding: 20px;
        max-width: none;
    }
}

@media (max-width: 480px) {
    .modal-content {
        width: 95%;
        padding: 15px;
    }
    
    .modal-buttons {
        flex-direction: column;
        gap: 10px;
    }
}
```

### 問題 5：Toast 通知沒有自動消失

**症狀**：Toast 顯示後一直存在

**原因**：Timer 未正確實現或未 Dispose

**解決方案**：
```csharp
private System.Threading.Timer? toastTimer;

private void ShowToast(string message)
{
    // 清除舊計時器
    toastTimer?.Dispose();
    
    toastMessage = message;
    showToast = true;
    StateHasChanged();
    
    // 建立新計時器
    toastTimer = new System.Threading.Timer(_ =>
    {
        InvokeAsync(() =>
        {
            showToast = false;
            StateHasChanged();
        });
    }, null, 3000, Timeout.Infinite);
}

public void Dispose()
{
    toastTimer?.Dispose();  // ← 重要！
}
```

---

## 📦 依賴項清單

### NuGet 套件
- `Microsoft.AspNetCore.Components.Web` (Blazor 核心)
- `Microsoft.JSInterop` (JavaScript 互操作)
- `System.Text.Json` (JSON 序列化)

### .NET 命名空間
```csharp
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using System.Text.Json;
using System.Runtime.InteropServices;
```

### CSS 框架
- Bootstrap 5.x（spinner 動畫）
- 自定義 CSS 變數系統

---

## 🎓 最佳實踐

### 1. 使用 Deep Copy 防止意外修改
```csharp
// 永遠使用快照方法
var items = BottomRowConfig.Get_UserDefinedList_snapshot();
```

### 2. 正確處理事件訂閱
```csharp
// 務必實現 IDisposable 並取消訂閱
@implements IDisposable

public void Dispose()
{
    BottomRowConfig.OnConfigChanged -= HandleConfigChanged;
}
```

### 3. 使用 InvokeAsync 更新 UI
```csharp
private void HandleConfigChanged()
{
    InvokeAsync(() =>
    {
        LoadData();
        StateHasChanged();
    });
}
```

### 4. 驗證用戶輸入
```csharp
// HTML 層級
<input maxlength="7" />

// C# 層級
if (displayName.Length > BottomRowConfig._MaxDisplayNameLength)
{
    ShowToast("名稱過長", false);
    return;
}
```

### 5. 響應式設計優先
```css
/* 從手機開始設計 */
@media (max-width: 480px) { /* ... */ }
@media (max-width: 768px) { /* ... */ }
@media (min-width: 1200px) { /* ... */ }
```

---

## 📚 相關文檔鏈接

- [Blazor Components 官方文檔](https://learn.microsoft.com/zh-tw/aspnet/core/blazor/components/)
- [CSS Variables (MDN)](https://developer.mozilla.org/zh-TW/docs/Web/CSS/Using_CSS_custom_properties)
- [System.Text.Json 文檔](https://learn.microsoft.com/zh-tw/dotnet/standard/serialization/system-text-json-overview)

---

## 🆘 支援與維護

### 版本歷史
- **v1.0** (2024): 初始版本，包含基本功能
- **v1.1** (2024): 加入 Modal 確認對話框
- **v1.2** (2024): 加入 Toast 通知和加載動畫
- **v1.3** (2024): 響應式設計和 Deep Copy 修復

### 已知限制
1. 按鈕數量固定為 4 個（可擴展但需修改驗證邏輯）
2. 僅支持預定義的 PAGE 枚舉路由
3. 顯示名稱最多 7 個字元（可在 BottomRowConfig._MaxDisplayNameLength 修改）
4. 大於 1200px 寬度的螢幕才顯示底部欄

### 貢獻指南
如需擴展功能，建議修改以下位置：
- 增加按鈕數量：修改 `BottomRowConfig.cs` 的 `IsValidConfig()` 和默認配置
- 自定義驗證規則：修改 `IsValidRouteName()` 方法
- 自定義樣式：使用 CSS 變數覆蓋默認主題

---

## 🎯 快速開始檢查清單

- [ ] 複製所有相關文件到新專案
- [ ] 在 Program.cs 註冊 `BottomRowConfig` 服務
- [ ] 在 MainLayout.razor 加入 `<BottomRow />` 組件
- [ ] 創建 `UserSetting/5040/` 目錄
- [ ] 創建默認的 `BottomRowConfig.json` 文件
- [ ] 確認所有路由頁面存在
- [ ] 測試保存設定功能
- [ ] 測試還原預設功能
- [ ] 測試手機響應式顯示
- [ ] 測試 Modal 和 Toast 顯示

---

**文檔版本**：v1.0  
**最後更新**：2024  
**維護者**：CMU3 開發團隊
