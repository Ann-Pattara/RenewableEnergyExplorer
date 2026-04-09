using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using RenewableEnergyBlazor;
using RenewableEnergyBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// When hosted by the ASP.NET Core API, BaseAddress is the app's origin
// (e.g. https://your-app.onrender.com/). All API calls resolve against it,
// so no hardcoded host or port is needed anywhere in the client.
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

builder.Services.AddScoped<EnergyApiService>();

// Suppress verbose browser console output in production builds
builder.Logging.SetMinimumLevel(LogLevel.Warning);

await builder.Build().RunAsync();
