using System;
using System.Threading.Tasks;
using EbayClone.Infrastructure.Data;
using EbayClone.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public class ConcurrencyTest
{
    public static async Task Run(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EbayDbContext>();

        // 1. Tạo dữ liệu mẫu
        var product = new Product { Name = "Concurrency Test Product", Description = "Test" };
        var variant = new ProductVariant { Name = "V1", SKU = "V1-SKU", Quantity = 10, Product = product };
        
        context.Products.Add(product);
        context.ProductVariants.Add(variant);
        await context.SaveChangesAsync();

        Console.WriteLine($"[INIT] Variant Quantity: {variant.Quantity}, RowVersion: {BitConverter.ToString(variant.RowVersion)}");

        // 2. Mô phỏng 2 phiên chỉnh sửa đồng thời
        var session1 = await context.ProductVariants.FindAsync(variant.Id);
        var session2 = await context.ProductVariants.AsNoTracking().FirstOrDefaultAsync(v => v.Id == variant.Id);

        session1.Quantity = 5;
        await context.SaveChangesAsync();
        Console.WriteLine($"[S1] Cập nhật Quantity = 5 thành công.");

        try 
        {
            session2.Quantity = 8;
            context.Entry(session2).State = EntityState.Modified;
            await context.SaveChangesAsync();
            Console.WriteLine($"[S2] Cập nhật Quantity = 8 thành công (SAI: Phải có lỗi Concurrency)");
        }
        catch (DbUpdateConcurrencyException)
        {
            Console.WriteLine($"[S2] Thành công: Bắt được lỗi DbUpdateConcurrencyException như mong đợi.");
        }
    }
}
