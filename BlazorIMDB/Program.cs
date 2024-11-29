using BlazorIMDB.Components;

using IMDBCLI.model;
using Microsoft.EntityFrameworkCore;
using BlazorIMDB.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContext<DAO>();

builder.Services.AddScoped<MainService>();
builder.Services.AddHttpClient<OmdbApi>(client =>
{
    client.BaseAddress = new Uri("https://www.omdbapi.com/"); 
});

builder.Services.AddMemoryCache();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
