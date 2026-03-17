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

// Đăng ký AuthTokenHandler như một Dependency
builder.Services.AddTransient<AuthTokenHandler>();

// Đăng ký cấu hình HttpClient chuẩn cho Blazor WebAssembly thông qua HttpMessageHandler
builder.Services.AddHttpClient("API", client =>
{
    client.BaseAddress = new Uri("https://localhost:5072");
})
.AddHttpMessageHandler<AuthTokenHandler>();

// Tạo default Scoped HttpClient chỏ tới "API" Client ở trên để Inject vào các Services cũ
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("API"));

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
builder.Services.AddScoped<WalletService>();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddMudServices();

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

await builder.Build().RunAsync();
