using BuilderCatalogue.Client.Pages;
using BuilderCatalogue.Client.Services;
using BuilderCatalogue.Components;

var builder = WebApplication.CreateBuilder(args);

const string apiBase = "https://d30r5p5favh3z8.cloudfront.net/";

builder.Services.AddHttpClient<LegoApiService>(c => c.BaseAddress = new Uri(apiBase));
builder.Services.AddSingleton<ColorService>();
builder.Services.AddSingleton<UserCacheService>();
builder.Services.AddSingleton<ActiveUserService>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();
await app.Services.GetRequiredService<ColorService>().InitializeAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(BuilderCatalogue.Client._Imports).Assembly);

app.Run();
