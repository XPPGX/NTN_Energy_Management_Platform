using demoVer.Components;
using MudBlazor.Services;
using Microsoft.AspNetCore.StaticWebAssets;
using Microsoft.AspNetCore.Components.Server.Circuits;
using demoVer.Services;
using demoVer.Models;
using demoVer.Broadcast;
using demoVer.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// builder.WebHost.UseUrls("http://127.0.0.1:5070");
builder.WebHost.UseUrls("http://0.0.0.0:5042");
// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.None);
// builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning);
// builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);

builder.Services.AddSingleton<CardUpdateNotifier>();
builder.Services.AddSingleton<ArrowAnimationService>();
builder.Services.AddMudServices();

builder.Services.AddSingleton<HeartbeatService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<HeartbeatService>());
builder.Services.AddSingleton<CommonData>(); //預計是整個專案共享一個CommonData
builder.Services.AddSingleton<DataCenter>();
builder.Services.AddSingleton<GlobalVar>();
builder.Services.AddSingleton<DataChangeEventManager>();
builder.Services.AddSingleton<IGroupsDataDecoder, GroupsDataDecoder>();
builder.Services.AddSingleton<CircuitsWatcher>();


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

//[Migrate] IP : localhost
builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri("http://127.0.0.1:5039/"); //Release
    // client.BaseAddress = new Uri("http://192.168.31.184:5039/"); //CMU3 test
    // client.BaseAddress = new Uri("http://127.0.0.1:5050/"); //local debug with python
    // client.BaseAddress = new Uri("http://192.168.102.201:5039/"); //windows debug with remote linux
    // client.BaseAddress = new Uri("http://192.168.102.204:5039/"); //windows debug with remote linux
    // client.BaseAddress = new Uri("http://192.168.31.90:5039/"); //windows debug with remote linux
});
builder.Services.AddSingleton<ApiManager>();
builder.Services.AddSingleton<WriteProcess>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapHub<DataHub>("/datahub"); //Sync data for all web that connected with the server

app.Run();
