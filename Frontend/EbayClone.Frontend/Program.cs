using EbayClone.Frontend;
using EbayClone.Frontend.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// TODO: Thay bằng URL thực tế của Backend API khi chạy (thường là port 5001 hoặc 7132 tuỳ visual studio)
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:7094") });

// Register Services
builder.Services.AddScoped<ShopService>();
builder.Services.AddScoped<PolicyService>();

await builder.Build().RunAsync();
