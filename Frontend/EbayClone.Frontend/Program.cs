using EbayClone.Frontend;
using EbayClone.Frontend.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// TODO: Thay bằng URL thực tế của Backend API khi chạy (thường là port 5001 hoặc 7132 tuỳ visual studio)
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:5072") });

// Register Services
builder.Services.AddScoped<ShopService>();
builder.Services.AddScoped<PolicyService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<BuyerService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<FileUploadService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<SellerService>();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddMudServices();

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

await builder.Build().RunAsync();
