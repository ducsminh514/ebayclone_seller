using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Domain.Entities;
using EbayClone.Infrastructure.BackgroundJobs;
using EbayClone.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EbayClone.Tests.UseCases
{
    public class AdvancedInteractionTests
    {
        [Fact]
        public async Task EscrowReleaseJob_ShouldReleaseFunds_After7Days()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<EbayDbContext>()
                .UseInMemoryDatabase(databaseName: "EscrowTest_" + Guid.NewGuid())
                .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            var shopId = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            
            using (var context = new EbayDbContext(options))
            {
                var shop = new Shop { Id = shopId, Name = "Test Shop" };
                context.Shops.Add(shop);

                var wallet = new SellerWallet { ShopId = shopId };
                wallet.AddPending(1000); // Pending 1000
                context.SellerWallets.Add(wallet);

                var order = new Order 
                { 
                    Id = orderId, 
                    ShopId = shopId, 
                    TotalAmount = 1000,
                    PlatformFee = 50,
                    OrderNumber = "ORD-123"
                };

                // Reflection to set read-only properties for testing
                var statusProp = typeof(Order).GetProperty("Status");
                statusProp?.SetValue(order, "DELIVERED");

                var completedAtProp = typeof(Order).GetProperty("CompletedAt");
                completedAtProp?.SetValue(order, DateTimeOffset.UtcNow.AddDays(-8));

                context.Orders.Add(order);
                await context.SaveChangesAsync();
            }

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped(_ => new EbayDbContext(options));
            var serviceProvider = serviceCollection.BuildServiceProvider();

            var loggerMock = new Mock<ILogger<EscrowReleaseJob>>();
            var job = new EscrowReleaseJob(serviceProvider, loggerMock.Object);

            // Act
            var method = typeof(EscrowReleaseJob).GetMethod("ProcessEscrowReleases", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (method != null)
            {
                await (Task)method.Invoke(job, new object[] { CancellationToken.None })!;
            }

            // Assert
            using (var context = new EbayDbContext(options))
            {
                var wallet = await context.SellerWallets.FirstAsync(w => w.ShopId == shopId);
                var order = await context.Orders.FirstAsync(o => o.Id == orderId);
                var transaction = await context.WalletTransactions.FirstOrDefaultAsync(t => t.ReferenceId == orderId);

                Assert.Equal(950, wallet.AvailableBalance); // 95% of 1000
                Assert.Equal(50, wallet.PendingBalance); // 1000 - 950 (Wait, logic is wallet.ReleaseEscrow(950))
                // Actually wallet.AddPending(1000) was done initially.
                // 1000 * 0.95 = 950.
                // ReleaseEscrow(950) -> Available +950, Pending -950.
                // Result: Available 950, Pending 50.
                
                Assert.NotNull(transaction);
                Assert.Equal("ESCROW_RELEASE", transaction.Type);
                Assert.Equal(950, transaction.Amount);
            }
        }
    }
}
