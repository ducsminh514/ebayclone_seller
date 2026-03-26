using EbayClone.Infrastructure.Data;
using EbayClone.API.Hubs;
using EbayClone.API.Services;
using Microsoft.AspNetCore.HttpOverrides;
using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Application.UseCases.Shops;
using EbayClone.Application.UseCases.Policies;
using EbayClone.Application.UseCases.Products;
using EbayClone.Application.UseCases.Auth;
using EbayClone.Application.UseCases.Orders;
using EbayClone.Application.UseCases.Vouchers;
using EbayClone.Application.UseCases.Dashboard;
using EbayClone.Application.UseCases.Finance;
using EbayClone.Application.UseCases.Feedbacks;
using EbayClone.Application.UseCases.Analytics;

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
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderCancellationRepository, OrderCancellationRepository>();
builder.Services.AddScoped<IOrderReturnRepository, OrderReturnRepository>();
builder.Services.AddScoped<IOrderDisputeRepository, OrderDisputeRepository>();
builder.Services.AddScoped<IDefaultPolicySeeder, DefaultPolicySeeder>();
builder.Services.AddScoped<ICategorySeeder, CategorySeeder>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IVoucherRepository, VoucherRepository>();
builder.Services.AddScoped<ISellerDefectRepository, SellerDefectRepository>();
builder.Services.AddScoped<IShopAnalyticsDailyRepository, ShopAnalyticsDailyRepository>();
builder.Services.AddScoped<IProductViewLogRepository, ProductViewLogRepository>();

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
builder.Services.AddScoped<IOpenReturnUseCase, OpenReturnUseCase>();
builder.Services.AddScoped<IRespondReturnUseCase, RespondReturnUseCase>();
builder.Services.AddScoped<IRespondPartialOfferUseCase, RespondPartialOfferUseCase>();
builder.Services.AddScoped<IIssueRefundUseCase, IssueRefundUseCase>();
builder.Services.AddScoped<IConfirmItemReceivedUseCase, ConfirmItemReceivedUseCase>();
builder.Services.AddScoped<IOpenDisputeUseCase, OpenDisputeUseCase>();
builder.Services.AddScoped<IRespondDisputeUseCase, RespondDisputeUseCase>();
builder.Services.AddScoped<IResolveDisputeUseCase, ResolveDisputeUseCase>();
builder.Services.AddScoped<IRegisterUserUseCase, RegisterUserUseCase>();
builder.Services.AddScoped<ILoginUseCase, LoginUseCase>();
builder.Services.AddScoped<IRefreshTokenUseCase, RefreshTokenUseCase>();
builder.Services.AddScoped<IGetDashboardStatsUseCase, GetDashboardStatsUseCase>();
builder.Services.AddScoped<IEvaluateSellerLevelUseCase, EvaluateSellerLevelUseCase>();
builder.Services.AddScoped<ITrackProductViewUseCase, TrackProductViewUseCase>();
builder.Services.AddScoped<IGetTrafficStatsUseCase, GetTrafficStatsUseCase>();

// Background Services: tự động kích hoạt Listing SCHEDULED, giải ngân Escrow, evaluate Seller Level
builder.Services.AddHostedService<EbayClone.API.BackgroundServices.ScheduledListingActivatorService>();
builder.Services.AddHostedService<EbayClone.API.BackgroundServices.FundReleaseHostedService>();
builder.Services.AddHostedService<EbayClone.API.BackgroundServices.EvaluateSellerLevelService>();
builder.Services.AddHostedService<EbayClone.API.BackgroundServices.ComputeDailyAnalyticsService>();
builder.Services.AddHostedService<EbayClone.API.BackgroundServices.ReconcileDenormalizedCountsService>();
builder.Services.AddScoped<IVerifyEmailUseCase, VerifyEmailUseCase>();
builder.Services.AddScoped<IGetSellerFinanceUseCase, GetSellerFinanceUseCase>();

// Voucher UseCases
builder.Services.AddScoped<CreateVoucherUseCase>();
builder.Services.AddScoped<GetVouchersUseCase>();
builder.Services.AddScoped<GetVoucherByIdUseCase>();
builder.Services.AddScoped<UpdateVoucherUseCase>();
builder.Services.AddScoped<UpdateVoucherStatusUseCase>();
builder.Services.AddScoped<DeleteVoucherUseCase>();
builder.Services.AddScoped<ApplyVoucherUseCase>();

// Feedback
builder.Services.AddScoped<IFeedbackRepository, FeedbackRepository>();
builder.Services.AddScoped<ILeaveFeedbackUseCase, LeaveFeedbackUseCase>();
builder.Services.AddScoped<IGetFeedbacksByShopUseCase, GetFeedbacksByShopUseCase>();
builder.Services.AddScoped<IReplyFeedbackUseCase, ReplyFeedbackUseCase>();
builder.Services.AddScoped<IGetFeedbackByOrderUseCase, GetFeedbackByOrderUseCase>();

// HttpClient factory cho gọi external APIs (Gemini AI)
builder.Services.AddHttpClient();

// MemoryCache cho static data (categories, item specifics) — tránh hit DB mỗi request
builder.Services.AddMemoryCache();

// [Performance Phase 2] Redis Distributed Cache — shared state giữa nhiều instances
var redisConn = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConn;
    options.InstanceName = "EbayClone_";
});

// [SignalR] Real-time Order Notifications + Redis Backplane (multi-instance broadcast)
builder.Services.AddSignalR()
    .AddStackExchangeRedis(redisConn, options =>
    {
        options.Configuration.ChannelPrefix = 
            StackExchange.Redis.RedisChannel.Literal("EbayClone_SignalR");
    });
builder.Services.AddScoped<IOrderNotificationService, SignalROrderNotificationService>();

// [Performance Phase 2] Distributed Lock Service — chống duplicate background job processing
builder.Services.AddSingleton<EbayClone.Application.Interfaces.IDistributedLockService, 
    EbayClone.Infrastructure.Services.RedisDistributedLockService>();

// [Performance Phase 1] Health Check endpoint cho Nginx load balancer
builder.Services.AddHealthChecks()
    .AddSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "sqlserver",
        timeout: TimeSpan.FromSeconds(3));

// [Performance Phase 1] ForwardedHeaders: Nginx reverse proxy → Kestrel biết IP thật từ X-Forwarded-For
// Nếu không có: Rate Limiting sẽ thấy tất cả request từ cùng 1 IP (IP của Nginx) → block toàn bộ users
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

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
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "https://localhost:7250",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "https://localhost:7251",
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "superSecretKey@345EbayClone@Authentication123!")),
        RoleClaimType = System.Security.Claims.ClaimTypes.Role
    };

    // [SignalR] WebSocket không gửi Authorization header — đọc token từ query string
    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
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

// [Performance Phase 1] CORS Policy — đọc origins từ config thay vì hardcode
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "https://localhost:7251", "http://localhost:7252" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // [SignalR] Bắt buộc cho WebSocket negotiate
        });
});

var app = builder.Build();

// [Performance Phase 1] ForwardedHeaders phải đặt TRƯỚC mọi middleware khác
app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection(); // Tắt HTTPS Redirection trong Docker để không bị lỗi 307 Redirect dẫn tới ERR_EMPTY_RESPONSE cho CORS Preflight request

// Kích hoạt CORS Pipeline cho Frontend Blazor (port 5002) gọi sang Backend (7132)
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// Serve file ảnh từ wwwroot (cần đặt sau UseAuthorization để ảnh public không cần auth)
app.UseStaticFiles();

// Kích hoạt Rate Limiter middleware
app.UseRateLimiter();

app.MapControllers();


// [SignalR] Map Hub endpoint cho real-time order notifications
app.MapHub<OrderHub>("/hubs/orders");

// [Performance Phase 1] Health check endpoint cho Nginx upstream health check
app.MapHealthChecks("/health");

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

// [A7] Seed categories + item specifics khi khởi động (idempotent)
using (var seedScope = app.Services.CreateScope())
{
    var dbContext = seedScope.ServiceProvider.GetRequiredService<EbayClone.Infrastructure.Data.EbayDbContext>();
    
    // Đợi 10 giây đầu tiên để SQL Server kịp "tỉnh giấc"
    Console.WriteLine("[Migration] Waiting 10s for SQL Server to warm up...");
    System.Threading.Thread.Sleep(10000);

    // Thêm Retry loop 10 lần (đợi 5 giây mỗi lần) để chờ SQL Server trong Docker khởi động xong
    for (int i = 0; i < 10; i++)
    {
        try
        {
            Console.WriteLine($"[Migration] Attempt {i + 1}/10: Migrating database...");
            dbContext.Database.Migrate(); // Tự động tạo bảng vào Database Ảo.
            Console.WriteLine("[Migration] Database migration successful.");
            break;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Migration] Attempt {i + 1}/10 failed: {ex.Message}");
            if (i == 9) 
            {
                Console.WriteLine("[Migration] ERR: Could not connect to SQL Server after 10 attempts. Please check if 'db' container is running and password is correct.");
                throw; 
            }
            System.Threading.Thread.Sleep(5000); // Đợi 5s cho SQL Server boot xong
        }
    }
    
    var categorySeeder = seedScope.ServiceProvider.GetRequiredService<ICategorySeeder>();
    await categorySeeder.SeedCategoriesAsync();
}

app.Run();
