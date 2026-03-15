using EbayClone.Infrastructure.Data;
using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Application.UseCases.Shops;
using EbayClone.Application.UseCases.Policies;
using EbayClone.Application.UseCases.Products;
using EbayClone.Application.UseCases.Auth;
using EbayClone.Application.UseCases.Orders;
using EbayClone.Application.UseCases.Dashboard;
using EbayClone.Application.UseCases.Finance;

using EbayClone.Infrastructure.Repositories;
using EbayClone.Infrastructure.Services;
using EbayClone.Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Cho phép upload file tối đa 10MB
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024;
});
builder.Services.AddDbContext<EbayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Dependency Injection (Clean Architecture)
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IShopRepository, ShopRepository>();
builder.Services.AddScoped<ISellerWalletRepository, SellerWalletRepository>();
builder.Services.AddScoped<IWalletTransactionRepository, WalletTransactionRepository>();
builder.Services.AddScoped<IPolicyRepository, PolicyRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IDefaultPolicySeeder, DefaultPolicySeeder>();
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddScoped<ICreateShopUseCase, CreateShopUseCase>();
builder.Services.AddScoped<IVerifyShopOtpUseCase, VerifyShopOtpUseCase>();
builder.Services.AddScoped<ILinkBankAccountUseCase, LinkBankAccountUseCase>();
builder.Services.AddScoped<IVerifyMicroDepositUseCase, VerifyMicroDepositUseCase>();
builder.Services.AddScoped<IApproveShopUseCase, ApproveShopUseCase>();
builder.Services.AddScoped<IUpdateShopProfileUseCase, UpdateShopProfileUseCase>();
builder.Services.AddScoped<ICreateShippingPolicyUseCase, CreateShippingPolicyUseCase>();
builder.Services.AddScoped<ICreateReturnPolicyUseCase, CreateReturnPolicyUseCase>();
builder.Services.AddScoped<ICreatePaymentPolicyUseCase, CreatePaymentPolicyUseCase>();
builder.Services.AddScoped<ICreateListingUseCase, CreateListingUseCase>();
builder.Services.AddScoped<IRestockVariantUseCase, RestockVariantUseCase>();
builder.Services.AddScoped<IGetProductsUseCase, GetProductsUseCase>();
builder.Services.AddScoped<IGetProductByIdUseCase, GetProductByIdUseCase>();
builder.Services.AddScoped<IUpdateProductBasicUseCase, UpdateProductBasicUseCase>();
builder.Services.AddScoped<IUpdateProductVariantsUseCase, UpdateProductVariantsUseCase>();
builder.Services.AddScoped<IUpdateFullProductUseCase, UpdateFullProductUseCase>();
builder.Services.AddScoped<IUpdateProductStatusUseCase, UpdateProductStatusUseCase>();
builder.Services.AddScoped<ISoftDeleteProductUseCase, SoftDeleteProductUseCase>();
builder.Services.AddScoped<IGetShippingPoliciesUseCase, GetShippingPoliciesUseCase>();
builder.Services.AddScoped<IGetReturnPoliciesUseCase, GetReturnPoliciesUseCase>();
builder.Services.AddScoped<IGetPaymentPoliciesUseCase, GetPaymentPoliciesUseCase>();
builder.Services.AddScoped<IDeletePolicyUseCase, DeletePolicyUseCase>();
builder.Services.AddScoped<ISetDefaultPolicyUseCase, SetDefaultPolicyUseCase>();
builder.Services.AddScoped<IOptInPolicyUseCase, OptInPolicyUseCase>();
builder.Services.AddScoped<IUpdateOrderStatusUseCase, UpdateOrderStatusUseCase>();
builder.Services.AddScoped<IGetOrdersUseCase, GetOrdersUseCase>();
builder.Services.AddScoped<IGetOrderByIdUseCase, GetOrderByIdUseCase>();
builder.Services.AddScoped<ICreateTestOrderUseCase, CreateTestOrderUseCase>();
builder.Services.AddScoped<IReleaseFundsUseCase, ReleaseFundsUseCase>();
builder.Services.AddScoped<IRegisterUserUseCase, RegisterUserUseCase>();
builder.Services.AddScoped<ILoginUseCase, LoginUseCase>();
builder.Services.AddScoped<IRefreshTokenUseCase, RefreshTokenUseCase>();
builder.Services.AddScoped<IGetDashboardStatsUseCase, GetDashboardStatsUseCase>();

// Background Service: tự động kích hoạt sản phẩm SCHEDULED → ACTIVE khi đến giờ
builder.Services.AddHostedService<EbayClone.API.BackgroundServices.ScheduledListingActivatorService>();
builder.Services.AddScoped<IVerifyEmailUseCase, VerifyEmailUseCase>();
builder.Services.AddScoped<IGetSellerFinanceUseCase, GetSellerFinanceUseCase>();

// Cấu hình JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "https://localhost:5072",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "https://localhost:5071",
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "superSecretKey@345EbayClone@Authentication123!")),
        RoleClaimType = System.Security.Claims.ClaimTypes.Role
    };
});
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Nhập token: Bearer {token}",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

// Rate Limiting: chống spam API tạo sản phẩm và upload ảnh
builder.Services.AddRateLimiter(options =>
{
    // Policy cho CreateListing: 10 request/phút/IP
    options.AddFixedWindowLimiter("create_listing", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0; // Không queue, từ chối ngay khi vượt
    });
    // Policy cho Upload ảnh: 20 request/phút/IP
    options.AddFixedWindowLimiter("upload_image", opt =>
    {
        opt.PermitLimit = 20;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
    // Policy cho Business Policies: 5 request/phút (Chống spam tạo/sửa)
    options.AddFixedWindowLimiter("strict_policy", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
    // HTTP 429 khi vượt giới hạn
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Khởi tạo CORS Policy cho phép Blazor gọi API không bị Browser chặn (Same-Origin Policy)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.WithOrigins("https://localhost:5071", "http://localhost:5070") // Port của Frontend
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Kích hoạt CORS Pipeline cho Frontend Blazor (port 5002) gọi sang Backend (7132)
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// Serve file ảnh từ wwwroot (cần đặt sau UseAuthorization để ảnh public không cần auth)
app.UseStaticFiles();

// Kích hoạt Rate Limiter middleware
app.UseRateLimiter();

app.MapControllers();

// ĐOẠN DEMO: KẾT NỐI DATABASE
app.MapGet("/test-db", async (EbayClone.Infrastructure.Data.EbayDbContext dbContext) =>
{
    try
    {
        // Hàm CanConnectAsync() sẽ thử mở một connection tới SQL Server
        bool isConnected = await dbContext.Database.CanConnectAsync();
        if (isConnected)
        {
            return Results.Ok(new 
            { 
                Status = "Thành Công!", 
                Message = "API đã kết nối thông suốt với Database [EbaySellerClone] bằng Entity Framework Core." 
            });
        }
        
        return Results.Problem("Có lỗi xảy ra: Không thể mở kết nối tới Database.");
    }
    catch (Exception ex)
    {
        return Results.Problem($"Lỗi Exception: {ex.Message}");
    }
})
.WithName("TestDatabaseConnection");

app.Run();
