using demoVer.Components;
using MudBlazor.Services;
using Microsoft.AspNetCore.StaticWebAssets;
using Microsoft.AspNetCore.Components.Server.Circuits;
using demoVer.Services;
using demoVer.Models;
using demoVer.Broadcast;
var builder = WebApplication.CreateBuilder(args);
// builder.WebHost.UseUrls("http://127.0.0.1:5070");
builder.WebHost.UseUrls("http://0.0.0.0:5042");
// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<CardUpdateNotifier>();
builder.Services.AddSingleton<ArrowAnimationService>();
builder.Services.AddMudServices();

builder.Services.AddSingleton<HeartbeatService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<HeartbeatService>());
builder.Services.AddSingleton<CommonData>(); //預計是整個專案共享一個CommonData
builder.Services.AddSingleton<DataCenter>();



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
