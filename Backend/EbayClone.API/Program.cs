using EbayClone.Infrastructure.Data;
using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Application.UseCases.Shops;
using EbayClone.Application.UseCases.Policies;
using EbayClone.Application.UseCases.Products;
using EbayClone.Application.UseCases.Auth;
using EbayClone.Application.UseCases.Orders;
using EbayClone.Application.UseCases.Finance;
using EbayClone.Infrastructure.Repositories;
using EbayClone.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
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
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddScoped<ICreateShopUseCase, CreateShopUseCase>();
builder.Services.AddScoped<IApproveShopUseCase, ApproveShopUseCase>();
builder.Services.AddScoped<ICreateShippingPolicyUseCase, CreateShippingPolicyUseCase>();
builder.Services.AddScoped<ICreateReturnPolicyUseCase, CreateReturnPolicyUseCase>();
builder.Services.AddScoped<ICreateListingUseCase, CreateListingUseCase>();
builder.Services.AddScoped<IRestockVariantUseCase, RestockVariantUseCase>();
builder.Services.AddScoped<IGetProductsUseCase, GetProductsUseCase>();
builder.Services.AddScoped<IGetProductByIdUseCase, GetProductByIdUseCase>();
builder.Services.AddScoped<IUpdateProductBasicUseCase, UpdateProductBasicUseCase>();
builder.Services.AddScoped<IUpdateProductStatusUseCase, UpdateProductStatusUseCase>();
builder.Services.AddScoped<ISoftDeleteProductUseCase, SoftDeleteProductUseCase>();
builder.Services.AddScoped<IGetShippingPoliciesUseCase, GetShippingPoliciesUseCase>();
builder.Services.AddScoped<IGetReturnPoliciesUseCase, GetReturnPoliciesUseCase>();
builder.Services.AddScoped<IUpdateOrderStatusUseCase, UpdateOrderStatusUseCase>();
builder.Services.AddScoped<IGetOrdersUseCase, GetOrdersUseCase>();
builder.Services.AddScoped<IGetOrderByIdUseCase, GetOrderByIdUseCase>();
builder.Services.AddScoped<ICreateTestOrderUseCase, CreateTestOrderUseCase>();
builder.Services.AddScoped<IRegisterUserUseCase, RegisterUserUseCase>();
builder.Services.AddScoped<ILoginUseCase, LoginUseCase>();
builder.Services.AddScoped<IRefreshTokenUseCase, RefreshTokenUseCase>();
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
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "http://localhost:7094",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "http://localhost:7011",
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
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Khởi tạo CORS Policy cho phép Blazor gọi API không bị Browser chặn (Same-Origin Policy)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.WithOrigins("https://localhost:7011", "http://localhost:5070") // Port của Frontend
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
