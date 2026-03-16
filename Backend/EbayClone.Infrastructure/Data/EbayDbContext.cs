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
        public DbSet<VariantAttributeValue> VariantAttributeValues { get; set; }
        public DbSet<CategoryItemSpecific> CategoryItemSpecifics { get; set; }
        public DbSet<ProductItemSpecific> ProductItemSpecifics { get; set; }
        public DbSet<OrderReturn> OrderReturns { get; set; }
        public DbSet<OrderCancellation> OrderCancellations { get; set; }
        public DbSet<OrderDispute> OrderDisputes { get; set; }

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
                entity.Property(e => e.MicroDepositAmount1).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.MicroDepositAmount2).HasColumnType("decimal(18, 2)");
            });

            // ShippingPolicies
            modelBuilder.Entity<ShippingPolicy>(entity => {
                entity.HasIndex(e => e.ShopId);
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(250);
                entity.Property(e => e.PackageWeightOz).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();
            });

            // ReturnPolicies
            modelBuilder.Entity<ReturnPolicy>(entity => {
                entity.HasIndex(e => e.ShopId);
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(250);
                entity.Property(e => e.DomesticShippingPaidBy).HasMaxLength(20);
                entity.Property(e => e.InternationalShippingPaidBy).HasMaxLength(20);
                // RestockingFeePercent removed — eBay cấm restocking fee
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
                entity.Property(e => e.Subtitle).HasMaxLength(80);
                entity.Property(e => e.Brand).HasMaxLength(100);
                entity.Property(e => e.Condition).HasMaxLength(50).HasDefaultValue("New");
                entity.Property(e => e.ConditionDescription).HasMaxLength(500);
                entity.Property(e => e.ListingFormat).HasMaxLength(20).HasDefaultValue("FIXED_PRICE");
                entity.Property(e => e.AutoAcceptPrice).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.AutoDeclinePrice).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("DRAFT");
                entity.Property(e => e.BasePrice).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.PrimaryImageUrl).HasMaxLength(500);
                entity.Property(e => e.IsDeleted).HasDefaultValue(false);
                
                // Global Query Filter: tự động loại trừ SP đã Soft Delete
                entity.HasQueryFilter(e => !e.IsDeleted);

                // Indexes cho Performance & Scalability
                entity.HasIndex(e => e.ShopId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => new { e.ShopId, e.Status }); // [A6] Composite index cho filter by shop + status
                entity.HasIndex(e => e.ScheduledAt);

                entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();
            });

            // ProductVariants
            modelBuilder.Entity<ProductVariant>(entity => {
                entity.HasIndex(e => e.SkuCode);
                entity.HasOne(e => e.Product).WithMany(p => p.Variants).HasForeignKey(e => e.ProductId).IsRequired(false);
                entity.Property(e => e.SkuCode).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
                // ReservedQuantity + AvailableStock removed — single-step deduction model

                entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();
            });

            // [A1] VariantAttributeValues — relational cho query/filter
            modelBuilder.Entity<VariantAttributeValue>(entity => {
                entity.HasOne(e => e.Variant)
                      .WithMany(v => v.AttributeValues)
                      .HasForeignKey(e => e.VariantId);
                entity.Property(e => e.AttributeName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.AttributeValue).HasMaxLength(200).IsRequired();
                entity.HasIndex(e => new { e.VariantId, e.AttributeName }).IsUnique();
                entity.HasIndex(e => e.AttributeName); // Index cho filter queries
            });

            // [A5] CategoryItemSpecifics — Required/Recommended per category
            modelBuilder.Entity<CategoryItemSpecific>(entity => {
                entity.HasOne(e => e.Category)
                      .WithMany(c => c.ItemSpecifics)
                      .HasForeignKey(e => e.CategoryId);
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Requirement).HasMaxLength(20).HasDefaultValue("RECOMMENDED");
                entity.HasIndex(e => new { e.CategoryId, e.Name }).IsUnique();
            });

            // [A5] ProductItemSpecifics — giá trị seller nhập cho sản phẩm
            modelBuilder.Entity<ProductItemSpecific>(entity => {
                entity.HasOne(e => e.Product)
                      .WithMany(p => p.ItemSpecifics)
                      .HasForeignKey(e => e.ProductId);
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Value).HasMaxLength(500).IsRequired();
                entity.HasIndex(e => new { e.ProductId, e.Name }).IsUnique();
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
                entity.Property(e => e.CancelReason).HasMaxLength(50);
                entity.Property(e => e.CancelRequestedBy).HasMaxLength(20);
                entity.HasIndex(e => e.Status); // Performance: filter by status
                entity.HasIndex(e => e.ShipByDate); // Performance: late shipment queries
                entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();
            });

            // OrderReturns
            modelBuilder.Entity<OrderReturn>(entity => {
                entity.HasOne(e => e.Order).WithMany(o => o.Returns).HasForeignKey(e => e.OrderId);
                entity.HasOne(e => e.Buyer).WithMany().HasForeignKey(e => e.BuyerId).OnDelete(DeleteBehavior.Restrict);
                entity.Property(e => e.Reason).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("REQUESTED");
                entity.Property(e => e.SellerResponseType).HasMaxLength(50);
                entity.Property(e => e.ReturnShippingPaidBy).HasMaxLength(20).HasDefaultValue("BUYER");
                entity.Property(e => e.ReturnTrackingCode).HasMaxLength(100);
                entity.Property(e => e.ReturnCarrier).HasMaxLength(100);
                entity.Property(e => e.RefundAmount).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.DeductionAmount).HasColumnType("decimal(18, 2)");
                entity.HasIndex(e => e.OrderId);
                entity.HasIndex(e => e.Status); // Performance: filter by return status
                entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();
            });

            // OrderCancellations
            modelBuilder.Entity<OrderCancellation>(entity => {
                entity.HasOne(e => e.Order).WithMany(o => o.Cancellations).HasForeignKey(e => e.OrderId);
                entity.Property(e => e.RequestedBy).HasMaxLength(20).IsRequired();
                entity.Property(e => e.Reason).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("REQUESTED");
                entity.HasIndex(e => e.OrderId);
                entity.HasIndex(e => e.Status); // Performance: filter by cancellation status
                entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();
            });

            // OrderDisputes
            modelBuilder.Entity<OrderDispute>(entity => {
                entity.HasOne(e => e.Order).WithMany(o => o.Disputes).HasForeignKey(e => e.OrderId);
                entity.HasOne(e => e.Buyer).WithMany().HasForeignKey(e => e.BuyerId).OnDelete(DeleteBehavior.Restrict);
                entity.Property(e => e.Type).HasMaxLength(20).IsRequired();
                entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("OPENED");
                entity.Property(e => e.Resolution).HasMaxLength(50);
                entity.HasIndex(e => e.OrderId);
                entity.HasIndex(e => e.Status); // Performance: filter by dispute status
                entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();
            });



            // OrderItems
            modelBuilder.Entity<OrderItem>(entity => {
                entity.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId).IsRequired(false);
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
                entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();
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
                entity.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId).IsRequired(false);
                entity.Property(e => e.ViewerIP).HasMaxLength(50);
            });

            // Reviews
            modelBuilder.Entity<Review>(entity => {
                entity.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId).IsRequired(false);
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
                // Prevent cascade for new Order child tables
                if ((fk.DeclaringEntityType.ClrType == typeof(OrderReturn) ||
                     fk.DeclaringEntityType.ClrType == typeof(OrderCancellation) ||
                     fk.DeclaringEntityType.ClrType == typeof(OrderDispute)) &&
                    fk.Properties.Any(p => p.Name == "BuyerId"))
                {
                    fk.DeleteBehavior = DeleteBehavior.Restrict;
                }
            }
        }
    }
}
