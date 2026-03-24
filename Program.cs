using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using FlightApp;
using FlightApp.Services;
using FlightApp.Analysis;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");


builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<TrackBinarySerializer>();
builder.Services.AddScoped<IFlightStorage, IndexedDbFlightStorage>();
builder.Services.AddScoped<FlightImportService>();
builder.Services.AddScoped<IgcParser>();
builder.Services.AddScoped<FlightStatsCalculator>();
builder.Services.AddScoped<FlightService>();
builder.Services.AddScoped<FlightDetailsLoadService>();
builder.Services.AddScoped<FlightDetailsSelectionService>();
builder.Services.AddScoped<IKeyValueStorage, LocalStorageKeyValueStorage>();
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<FlightImportStateService>();
builder.Services.AddScoped<FlightDetailsStateService>();

await builder.Build().RunAsync();
