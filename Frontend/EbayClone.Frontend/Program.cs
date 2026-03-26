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
// [Config] Đọc ApiBaseUrl từ wwwroot/appsettings.json — hỗ trợ cả dev (7250) và cluster (Nginx port 80)
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7250";
builder.Services.AddHttpClient("API", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
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
builder.Services.AddScoped<CategoryService>(); // Scoped: an toàn với HttpClient (cũng Scoped)
builder.Services.AddSingleton<CategoryCacheService>(); // Singleton: lưu category data trong browser memory suốt session
builder.Services.AddScoped<FileUploadService>();
builder.Services.AddSingleton(new ImageUrlService(apiBaseUrl));
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<SellerService>();
builder.Services.AddScoped<WalletService>();
builder.Services.AddScoped<VoucherService>();
builder.Services.AddScoped<FeedbackService>();
builder.Services.AddScoped<OrderHubService>();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddMudServices();

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

await builder.Build().RunAsync();
