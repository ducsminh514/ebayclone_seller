using EbayClone.Infrastructure.Data;
using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Application.UseCases.Shops;
using EbayClone.Application.UseCases.Policies;
using EbayClone.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<EbayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Dependency Injection (Clean Architecture)
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IShopRepository, ShopRepository>();
builder.Services.AddScoped<ISellerWalletRepository, SellerWalletRepository>();
builder.Services.AddScoped<IPolicyRepository, PolicyRepository>();

builder.Services.AddScoped<ICreateShopUseCase, CreateShopUseCase>();
builder.Services.AddScoped<IApproveShopUseCase, ApproveShopUseCase>();
builder.Services.AddScoped<ICreateShippingPolicyUseCase, CreateShippingPolicyUseCase>();
builder.Services.AddScoped<ICreateReturnPolicyUseCase, CreateReturnPolicyUseCase>();

builder.Services.AddControllers();
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
