using demoVer.Components;
using MudBlazor.Services;
using Microsoft.AspNetCore.StaticWebAssets;
using Microsoft.AspNetCore.Components.Server.Circuits;
using demoVer.Services;
using demoVer.Models;
using demoVer.Broadcast;
using demoVer.Interfaces;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Localization;
using demoVer.Services.Localization;
using System.Globalization;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);


//自動調整此Server監聽的IP與Port
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    builder.WebHost.UseUrls("http://0.0.0.0:5080");
}
else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
{
    builder.WebHost.UseUrls("http://0.0.0.0:5040");
}
else
{
    builder.WebHost.UseUrls("http://0.0.0.0:5080");
}

// builder.WebHost.UseUrls("http://127.0.0.1:5070");
// builder.WebHost.UseUrls("http://0.0.0.0:5080");
// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.None);
// builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning);
// builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);

builder.Services.AddSingleton<CardUpdateNotifier>();
builder.Services.AddMudServices();
builder.Services.AddSingleton<ICardDefinition, InfoCardDefinition>();
builder.Services.AddSingleton<ICardDefinition, ChartCardDefinition>();
builder.Services.AddSingleton<ICardDefinition, RunningDiagramCardDefinition>();
builder.Services.AddSingleton<ICardDefinitionRegistry, CardDefinitionRegistry>();
builder.Services.AddLocalization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<LanguageService>();
builder.Services.AddScoped<Microsoft.AspNetCore.Components.Server.Circuits.CircuitHandler, CultureCircuitHandler>();
builder.Services.AddScoped<ILanguageProvider, ResourceLanguageProvider>();

builder.Services.AddSingleton<HeartbeatService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<HeartbeatService>());
builder.Services.AddSingleton<NotificationService>();
builder.Services.AddSingleton<CommonData>(); //預計是整個專案共享一個CommonData
builder.Services.AddSingleton<SubSystemManager>();
builder.Services.AddSingleton<LinkAddrManager>();
builder.Services.AddSingleton<DataCenter>();
builder.Services.AddSingleton<GlobalVar>();
builder.Services.AddSingleton<DataChangeEventManager>();
builder.Services.AddSingleton<WriteDataChangeEventManager>();
builder.Services.AddSingleton<IGroupsDataDecoder, GroupsDataDecoder>();
builder.Services.AddSingleton<CircuitsWatcher>();
builder.Services.AddSingleton<SqlProcessor>();
builder.Services.AddSingleton<VersionFileService>();
builder.Services.AddSingleton<StartupHostedService>();
builder.Services.AddSingleton<CodeTranslator>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<StartupHostedService>());

builder.Services.AddServerSideBlazor().AddCircuitOptions(o =>
{
    o.DisconnectedCircuitRetentionPeriod = TimeSpan.FromSeconds(10); //關閉網頁後10秒認為該user斷線，清除該user掛的所有action
});
// builder.Services.AddHostedService<PollingRead>();
builder.Services.AddSingleton<PollingRead>();
builder.Services.AddSingleton<PausableWorker>(sp => sp.GetRequiredService<PollingRead>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<PollingRead>());

builder.Services.AddSingleton<CircuitsWatcher>();
builder.Services.AddSingleton<CircuitHandler>(sp => sp.GetRequiredService<CircuitsWatcher>());

//根據作業系統自動切換 API Server IP 與 Port
builder.Services.AddHttpClient("ApiClient", client =>
{
    if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        // client.BaseAddress = new Uri("http://127.0.0.1:5050/"); //local debug with python
        // client.BaseAddress = new Uri("http://192.168.102.210:5039"); 
        // client.BaseAddress = new Uri("http://192.168.31.184:5039/"); //CMU3 test
        // client.BaseAddress = new Uri("http://192.168.102.201:5039/"); //windows debug with remote linux
        // client.BaseAddress = new Uri("http://192.168.102.204:5039/"); //windows debug with remote linux
        // client.BaseAddress = new Uri("http://192.168.31.90:5039/"); //windows debug with remote linux
        client.BaseAddress = new Uri("http://192.168.102.225:5039/"); //windows debug with remote linux
    }
    else if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
        client.BaseAddress = new Uri("http://127.0.0.1:5039/"); //Release
    }
    else
    {
        client.BaseAddress = new Uri("http://127.0.0.1:5050/"); //local debug with python
    }
    Console.WriteLine($"API Server : {client.BaseAddress}");
});
builder.Services.AddSingleton<ApiManager>();
builder.Services.AddSingleton<WriteProcess>();
builder.Services.AddControllers();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

var supportedCultures = new[] { "en-US", "zh-TW", "zh-CN" };
var localizationOptions = new RequestLocalizationOptions
{
    SupportedCultures = supportedCultures.Select(culture => new CultureInfo(culture)).ToList(),
    SupportedUICultures = supportedCultures.Select(culture => new CultureInfo(culture)).ToList(),
    ApplyCurrentCultureToResponseHeaders = true,
    RequestCultureProviders = new List<IRequestCultureProvider>
    {
        new QueryStringRequestCultureProvider(),
        new CookieRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
    }
};

app.UseRequestLocalization(localizationOptions);

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapControllers();
app.MapHub<DataHub>("/datahub"); //Sync data for all web that connected with the server

//Server Run之前啟動DataCenter
using (var scope = app.Services.CreateScope())
{
    var dataCenter = scope.ServiceProvider.GetRequiredService<DataCenter>();
}

app.Run();
