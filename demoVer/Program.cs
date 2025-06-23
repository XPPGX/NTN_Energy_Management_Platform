using demoVer.Components;
using MudBlazor.Services;
using Microsoft.AspNetCore.StaticWebAssets;
var builder = WebApplication.CreateBuilder(args);
// builder.WebHost.UseUrls("http://127.0.0.1:5070");
builder.WebHost.UseUrls("http://0.0.0.0:5042");
// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

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

app.Run();
