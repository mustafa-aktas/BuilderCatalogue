using BuilderCatalogue.Client.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

const string apiBase = "https://d30r5p5favh3z8.cloudfront.net/";

// In WASM, register a pre-configured HttpClient directly (no IHttpClientFactory needed)
builder.Services.AddSingleton(new HttpClient { BaseAddress = new Uri(apiBase) });
builder.Services.AddSingleton<LegoApiService>();
builder.Services.AddSingleton<ColorService>();
builder.Services.AddSingleton<UserCacheService>();
builder.Services.AddSingleton<ActiveUserService>();

var app = builder.Build();
await app.Services.GetRequiredService<ColorService>().InitializeAsync();
await app.RunAsync();
