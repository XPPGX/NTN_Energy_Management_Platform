# 在使用 API 之前的 Program.cs 設定

所有 API 服務 (`ApiManager` 及其相依服務，如 `SubSystemManager`, `PollingRead`, `DataCenter`…等) 需要在 `Program.cs` 中完成下列註冊與設定，才能於應用程式執行期間正常解析。

---

## 1. 註冊命名的 HttpClient

```csharp
builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri("http://127.0.0.1:5039");
});
```

- 供 `ApiManager` 使用的 `HttpClient` 必須以名稱 `ApiClient` 註冊，才能透過 `IHttpClientFactory` 建立。
- 可依需求從組態檔 (appsettings) 讀取 BaseAddress、Timeout 等設定。

---

## 2. 在ApiManager中注入Http
`demoVer/Services/ApiProcessor.cs`

```csharp
public class ApiManager
{
    private readonly HttpClient _http;
    public ApiManager(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("ApiClient");
    }
}
```