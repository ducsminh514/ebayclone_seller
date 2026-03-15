using EbayClone.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EbayClone.Infrastructure.Data
{
    public class EbayDbContext : DbContext
    {
        public EbayDbContext(DbContextOptions<EbayDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<FileEntity> Files { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Shop> Shops { get; set; }
        public DbSet<ShippingPolicy> ShippingPolicies { get; set; }
        public DbSet<ReturnPolicy> ReturnPolicies { get; set; }
        public DbSet<PaymentPolicy> PaymentPolicies { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Voucher> Vouchers { get; set; }
        public DbSet<SellerWallet> SellerWallets { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }
        public DbSet<ShopAnalyticsDaily> ShopAnalyticsDaily { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<ProductViewLog> ProductViewLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Fluent API Configurations to match database_ver1.sql
            
            // Users
            modelBuilder.Entity<User>(entity => {
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Username).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
                entity.Property(e => e.FullName).HasMaxLength(200);
                entity.Property(e => e.Role).HasMaxLength(50).HasDefaultValue("SELLER");
                entity.Property(e => e.EmailVerificationToken).HasMaxLength(100);
                entity.Property(e => e.EmailVerificationTokenExpiresAt).HasColumnType("DATETIMEOFFSET");
            });

            // Files
            modelBuilder.Entity<FileEntity>(entity => {
                entity.Property(e => e.Type).HasMaxLength(50);
            });

            // Categories
            modelBuilder.Entity<Category>(entity => {
                entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Slug).HasMaxLength(255).IsRequired();
                entity.HasOne(c => c.Parent).WithMany(c => c.SubCategories).HasForeignKey(c => c.ParentId);
                // AttributeHints lưu JSON, không giới hạn độ dài
            });

            // Shops
            modelBuilder.Entity<Shop>(entity => {
                entity.HasIndex(e => e.OwnerId).IsUnique(); // Chống Race Condition
                entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
                entity.Property(e => e.TaxCode).HasMaxLength(20);
                entity.Property(e => e.Address).HasMaxLength(255);
                entity.Property(e => e.IsVerified).HasDefaultValue(false);
                entity.Property(e => e.RatingAvg).HasColumnType("decimal(3, 2)");
                entity.Property(e => e.TotalShippingPolicies).HasDefaultValue(0);
                entity.Property(e => e.TotalReturnPolicies).HasDefaultValue(0);
                entity.Property(e => e.TotalPaymentPolicies).HasDefaultValue(0);
            });

            // ShippingPolicies
            modelBuilder.Entity<ShippingPolicy>(entity => {
                entity.HasIndex(e => e.ShopId);
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(250);
                entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();
            });

            // ReturnPolicies
            modelBuilder.Entity<ReturnPolicy>(entity => {
                entity.HasIndex(e => e.ShopId);
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(250);
                entity.Property(e => e.DomesticShippingPaidBy).HasMaxLength(20);
                entity.Property(e => e.InternationalShippingPaidBy).HasMaxLength(20);
                entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();
            });

            // PaymentPolicies
            modelBuilder.Entity<PaymentPolicy>(entity => {
                entity.HasIndex(e => e.ShopId);
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(250);
                entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            });

            // Products
            modelBuilder.Entity<Product>(entity => {
                entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Brand).HasMaxLength(100);
                entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("DRAFT");
                entity.Property(e => e.BasePrice).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.PrimaryImageUrl).HasMaxLength(500);
                entity.Property(e => e.IsDeleted).HasDefaultValue(false);
                
                // Global Query Filter: tự động loại trừ SP đã Soft Delete
                entity.HasQueryFilter(e => !e.IsDeleted);

                // Indexes cho Performance & Scalability
                entity.HasIndex(e => e.ShopId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.ScheduledAt);

                entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();
            });

            // ProductVariants
            modelBuilder.Entity<ProductVariant>(entity => {
                entity.HasIndex(e => e.SkuCode);
                entity.Property(e => e.SkuCode).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
                // Computed Column mapping
                entity.Property(e => e.AvailableStock).HasComputedColumnSql("[Quantity] - [ReservedQuantity]", stored: false);

                entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();
            });

            // Orders
            modelBuilder.Entity<Order>(entity => {
                entity.HasIndex(e => e.OrderNumber).IsUnique();
                entity.Property(e => e.OrderNumber).HasMaxLength(50).IsRequired();
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.ShippingFee).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.PlatformFee).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("PENDING_PAYMENT");
                entity.Property(e => e.PaymentStatus).HasMaxLength(50).HasDefaultValue("UNPAID");
                entity.Property(e => e.ShippingCarrier).HasMaxLength(100);
                entity.Property(e => e.TrackingCode).HasMaxLength(100);
                entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();
            });

            // SellerWallet
            modelBuilder.Entity<SellerWallet>(entity => {
                entity.HasKey(e => e.ShopId);
                entity.Property(e => e.AvailableBalance).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.PendingBalance).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();
            });

            // OrderItems
            modelBuilder.Entity<OrderItem>(entity => {
                entity.Property(e => e.ProductNameSnapshot).HasMaxLength(255);
                entity.Property(e => e.PriceAtPurchase).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.TotalLineAmount).HasColumnType("decimal(18, 2)")
                      .HasComputedColumnSql("[Quantity] * [PriceAtPurchase]", stored: true);
            });

            // Vouchers
            modelBuilder.Entity<Voucher>(entity => {
                entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
                entity.Property(e => e.DiscountType).HasMaxLength(20).HasDefaultValue("PERCENTAGE");
                entity.Property(e => e.Value).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.MinOrderValue).HasColumnType("decimal(18, 2)");
                entity.Property(v => v.RowVersion).IsRowVersion().IsConcurrencyToken();
            });

            // SellerWallets
            modelBuilder.Entity<SellerWallet>(entity => {
                entity.HasKey(w => w.ShopId);
                entity.HasOne(w => w.Shop).WithOne().HasForeignKey<SellerWallet>(w => w.ShopId);
                entity.Property(e => e.AvailableBalance).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.PendingBalance).HasColumnType("decimal(18, 2)");
            });

            // WalletTransactions
            modelBuilder.Entity<WalletTransaction>(entity => {
                entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.Type).HasMaxLength(50).IsRequired();
                entity.Property(e => e.ReferenceType).HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(255);
                entity.Property(e => e.BalanceAfter).HasColumnType("decimal(18, 2)");
            });

            // ShopAnalyticsDaily
            modelBuilder.Entity<ShopAnalyticsDaily>(entity => {
                entity.HasKey(a => new { a.ShopId, a.ReportDate });
                entity.Property(e => e.TotalRevenue).HasColumnType("decimal(18, 2)");
            });

            // ProductViewLogs
            modelBuilder.Entity<ProductViewLog>(entity => {
                entity.Property(e => e.ViewerIP).HasMaxLength(50);
            });

            // Prevent SQL Server multiple cascade path errors
            var cascadeFKs = modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetForeignKeys())
                .Where(fk => !fk.IsOwnership && fk.DeleteBehavior == DeleteBehavior.Cascade);

            foreach (var fk in cascadeFKs.ToList())
            {
                if (fk.DeclaringEntityType.ClrType == typeof(OrderItem) && 
                    (fk.Properties.Any(p => p.Name == "ProductId" || p.Name == "VariantId")))
                {
                    fk.DeleteBehavior = DeleteBehavior.Restrict;
                }
                if (fk.DeclaringEntityType.ClrType == typeof(Review) && 
                    (fk.Properties.Any(p => p.Name == "ProductId" || p.Name == "OrderId")))
                {
                    fk.DeleteBehavior = DeleteBehavior.Restrict;
                }
            }
        }
    }
}
