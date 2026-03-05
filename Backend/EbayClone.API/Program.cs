using EbayClone.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<EbayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

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
