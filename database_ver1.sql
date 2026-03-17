/* =================================================================================
PROJECT: EBAY CLONE - SELLER HUB MODULE
DATABASE SYSTEM: SQL SERVER (T-SQL)
DESCRIPTION: Hệ thống lõi quản lý bán hàng, tồn kho và dòng tiền dành cho Seller.
UPDATED: 2026-03-15 (Đồng bộ 100% với Domain Entities - Phần 1 Complete)
=================================================================================
*/

-- 1. Tạo Database
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'EbaySellerClone')
BEGIN
    CREATE DATABASE EbaySellerClone;
END
GO

USE EbaySellerClone;
GO

-- ==============================================================================
-- MODULE 1: HẠ TẦNG & TÀI KHOẢN (INFRASTRUCTURE)
-- ==============================================================================

/* BẢNG 1: Users (Người dùng) */
CREATE TABLE [Users] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [Username] NVARCHAR(100) NOT NULL,
    [Email] NVARCHAR(255) NOT NULL UNIQUE,
    [PasswordHash] NVARCHAR(MAX) NOT NULL,
    [FullName] NVARCHAR(200),
    [IsEmailVerified] BIT DEFAULT 0,
    [EmailVerificationToken] NVARCHAR(MAX),
    [EmailVerificationTokenExpiresAt] DATETIMEOFFSET,
    [IsIdentityVerified] BIT DEFAULT 0,
    [Role] NVARCHAR(50) DEFAULT 'SELLER', -- 'ADMIN', 'BUYER', 'SELLER'
    [CreatedAt] DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET(),
    [UpdatedAt] DATETIMEOFFSET
);

/* BẢNG 2: Files (Quản lý Media) */
CREATE TABLE [Files] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [OwnerId] UNIQUEIDENTIFIER NOT NULL REFERENCES [Users]([Id]),
    [Url] NVARCHAR(MAX) NOT NULL,
    [Type] NVARCHAR(50), -- 'PRODUCT_IMAGE', 'AVATAR', 'DOCUMENT'
    [CreatedAt] DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET()
);

/* BẢNG 3: Categories (Danh mục ngành hàng) */
CREATE TABLE [Categories] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [ParentId] UNIQUEIDENTIFIER NULL REFERENCES [Categories]([Id]),
    [Name] NVARCHAR(255) NOT NULL,
    [Slug] NVARCHAR(255) NOT NULL,
    [IsActive] BIT DEFAULT 1,
    [AttributeHints] NVARCHAR(MAX) NULL -- JSON array gợi ý thuộc tính
);

-- ==============================================================================
-- MODULE 2: THIẾT LẬP CỬA HÀNG (STORE SETTINGS)
-- ==============================================================================

/* BẢNG 4: Shops (Hồ sơ cửa hàng) */
CREATE TABLE [Shops] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [OwnerId] UNIQUEIDENTIFIER NOT NULL REFERENCES [Users]([Id]),
    [Name] NVARCHAR(255) NOT NULL,
    [Description] NVARCHAR(MAX),
    [TaxCode] NVARCHAR(20),
    [Address] NVARCHAR(255),
    [IsVerified] BIT DEFAULT 0,
    [AvatarUrl] NVARCHAR(MAX),
    [BannerUrl] NVARCHAR(MAX),
    [IsActive] BIT DEFAULT 1,
    [RatingAvg] DECIMAL(3, 2) DEFAULT 0,
    [TotalShippingPolicies] INT DEFAULT 0,
    [TotalReturnPolicies] INT DEFAULT 0,
    [TotalPaymentPolicies] INT DEFAULT 0,
    [MonthlyListingLimit] INT DEFAULT 250, -- eBay thật free tier = 250 listings/tháng
    
    -- KYC & Identity Verification
    [IdentityImageUrl] NVARCHAR(MAX) NULL,
    [IsIdentityVerified] BIT DEFAULT 0,
    
    -- Managed Payments (Payouts)
    [BankName] NVARCHAR(MAX) NULL,
    [BankAccountNumber] NVARCHAR(MAX) NULL, -- MASKED: chỉ lưu ****6789
    [BankAccountHolderName] NVARCHAR(MAX) NULL,
    [BankVerificationStatus] NVARCHAR(MAX) DEFAULT 'NotStarted',
    [MicroDepositAmount1] DECIMAL(18, 2) DEFAULT 0,
    [MicroDepositAmount2] DECIMAL(18, 2) DEFAULT 0,
    [BankVerificationAttempts] INT DEFAULT 0,
    
    -- Business Policies Opt-in (eBay: seller phải bật trước khi dùng)
    [IsPolicyOptedIn] BIT DEFAULT 0,
    
    [CreatedAt] DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET()
);

/* BẢNG 5: ShippingPolicies (Chính sách vận chuyển) */
CREATE TABLE [ShippingPolicies] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [ShopId] UNIQUEIDENTIFIER NULL REFERENCES [Shops]([Id]), -- NULL = system default
    [Name] NVARCHAR(100) NOT NULL, 
    [Description] NVARCHAR(MAX) DEFAULT '',
    [HandlingTimeDays] INT DEFAULT 2,
    [IsDefault] BIT DEFAULT 0,
    
    -- Free Shipping (eBay: toggle nổi bật trên form)
    [OfferFreeShipping] BIT DEFAULT 0,
    
    -- Domestic Shipping
    [DomesticCostType] NVARCHAR(50) DEFAULT 'Flat',
    [DomesticServicesJson] NVARCHAR(MAX) DEFAULT '[]',
    
    -- International Shipping
    [IsInternationalShippingAllowed] BIT DEFAULT 0,
    [InternationalCostType] NVARCHAR(50) DEFAULT 'Flat',
    [InternationalServicesJson] NVARCHAR(MAX) DEFAULT '[]',
    
    -- Combined Shipping (eBay: giảm giá khi mua nhiều item)
    [OfferCombinedShippingDiscount] BIT DEFAULT 0,
    
    -- Package Details (eBay: template kích thước cho listings)
    [PackageType] NVARCHAR(50) DEFAULT 'Package',
    [PackageWeightOz] DECIMAL(18, 2) DEFAULT 0,
    [PackageDimensionsJson] NVARCHAR(MAX) DEFAULT '{}',
    
    -- Handling Time Cutoff (eBay: giờ cutoff, sau giờ này +1 ngày handling)
    [HandlingTimeCutoff] NVARCHAR(10) DEFAULT '14:00',
    
    -- Preferences
    [ExcludedLocationsJson] NVARCHAR(MAX) DEFAULT '[]',
    
    [IsArchived] BIT DEFAULT 0,
    [RowVersion] ROWVERSION NOT NULL
);

/* BẢNG 6: ReturnPolicies (Chính sách đổi trả) */
CREATE TABLE [ReturnPolicies] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [ShopId] UNIQUEIDENTIFIER NULL REFERENCES [Shops]([Id]), -- NULL = system default
    [Name] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(MAX) DEFAULT '',
    
    -- Domestic
    [IsDomesticAccepted] BIT DEFAULT 0,
    [DomesticReturnDays] INT DEFAULT 30,
    [DomesticShippingPaidBy] NVARCHAR(20) DEFAULT 'BUYER',
    
    -- International
    [IsInternationalAccepted] BIT DEFAULT 0,
    [InternationalReturnDays] INT DEFAULT 30,
    [InternationalShippingPaidBy] NVARCHAR(20) DEFAULT 'BUYER',
    
    -- Auto-Accept (eBay: tự chấp nhận return request)
    [AutoAcceptReturns] BIT DEFAULT 0,
    
    -- Immediate Refund (eBay: hoàn tiền ngay khi buyer gửi trả)
    [SendImmediateRefund] BIT DEFAULT 0,
    
    -- Return Address (eBay: địa chỉ nhận hàng trả khác shop chính)
    [ReturnAddressJson] NVARCHAR(MAX) NULL,
    
    -- Restocking Fee (eBay: phí restock 0-20%)
    [RestockingFeePercent] DECIMAL(18, 2) DEFAULT 0,
    
    [IsDefault] BIT DEFAULT 0,
    [IsArchived] BIT DEFAULT 0,
    [RowVersion] ROWVERSION NOT NULL
);

/* BẢNG 6.1: PaymentPolicies (Chính sách thanh toán) */
CREATE TABLE [PaymentPolicies] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [ShopId] UNIQUEIDENTIFIER NULL REFERENCES [Shops]([Id]), -- NULL = system default
    [Name] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(250) DEFAULT '',
    [ImmediatePaymentRequired] BIT DEFAULT 1, -- Buy It Now: require immediate payment
    [DaysToPayment] INT DEFAULT 4, -- Auction: 2,3,5,7,10 days allowed
    [PaymentMethod] NVARCHAR(50) DEFAULT 'ManagedPayments',
    [PaymentInstructions] NVARCHAR(MAX) NULL,
    [IsDefault] BIT DEFAULT 0,
    [IsArchived] BIT DEFAULT 0,
    [RowVersion] ROWVERSION NOT NULL
);

-- ==============================================================================
-- MODULE 3: SẢN PHẨM & TỒN KHO (CORE INVENTORY)
-- ==============================================================================

/* BẢNG 7: Products (Sản phẩm gốc) */
CREATE TABLE [Products] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [ShopId] UNIQUEIDENTIFIER NOT NULL REFERENCES [Shops]([Id]),
    [CategoryId] UNIQUEIDENTIFIER NOT NULL REFERENCES [Categories]([Id]),
    [ShippingPolicyId] UNIQUEIDENTIFIER REFERENCES [ShippingPolicies]([Id]),
    [ReturnPolicyId] UNIQUEIDENTIFIER REFERENCES [ReturnPolicies]([Id]),
    [PaymentPolicyId] UNIQUEIDENTIFIER REFERENCES [PaymentPolicies]([Id]), -- NEW
    
    [Name] NVARCHAR(255) NOT NULL,
    [Description] NVARCHAR(MAX),
    [Brand] NVARCHAR(100),
    [Status] NVARCHAR(20) DEFAULT 'DRAFT', 
    [ScheduledAt] DATETIMEOFFSET NULL, 
    [BasePrice] DECIMAL(18, 2),
    [ReferenceId] NVARCHAR(255) NULL, -- NEW (SKU/External Ref)
    
    [PrimaryImageUrl] NVARCHAR(MAX) NULL,
    [ImageUrls] NVARCHAR(MAX) NULL, 
    
    [IsDeleted] BIT DEFAULT 0, 
    [CreatedAt] DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET(),
    [UpdatedAt] DATETIMEOFFSET NULL,
    [LastModifiedBy] NVARCHAR(255) NULL,
    [RowVersion] ROWVERSION NOT NULL 
);

/* BẢNG 8: ProductVariants (Biến thể/SKU) */
CREATE TABLE [ProductVariants] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [ProductId] UNIQUEIDENTIFIER NOT NULL REFERENCES [Products]([Id]),
    
    [SkuCode] NVARCHAR(100) NOT NULL,
    [Price] DECIMAL(18, 2) NOT NULL,
    [Attributes] NVARCHAR(MAX) CHECK (ISJSON([Attributes]) = 1),
    
    [Quantity] INT NOT NULL DEFAULT 0,
    [ReservedQuantity] INT NOT NULL DEFAULT 0,
    [AvailableStock] AS ([Quantity] - [ReservedQuantity]), 
    
    [ImageUrl] NVARCHAR(MAX),
    [WeightGram] INT,
    
    [CreatedAt] DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET(),
    [UpdatedAt] DATETIMEOFFSET NULL,
    [LastModifiedBy] NVARCHAR(255) NULL,
    [RowVersion] ROWVERSION NOT NULL,
    
    CONSTRAINT [CK_Stock_Valid] CHECK ([Quantity] >= 0 AND [ReservedQuantity] >= 0)
);
CREATE INDEX [IX_Variants_Sku] ON [ProductVariants]([SkuCode]);

-- ==============================================================================
-- MODULE 4: ĐƠN HÀNG & DÒNG TIỀN (ORDER FLOW)
-- ==============================================================================

/* BẢNG 9: Orders (Đơn hàng) */
CREATE TABLE [Orders] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [OrderNumber] NVARCHAR(50) NOT NULL UNIQUE,
    [IdempotencyKey] NVARCHAR(MAX) NOT NULL DEFAULT '', -- NEW
    [ShopId] UNIQUEIDENTIFIER NOT NULL REFERENCES [Shops]([Id]),
    [BuyerId] UNIQUEIDENTIFIER NOT NULL REFERENCES [Users]([Id]),
    
    [TotalAmount] DECIMAL(18, 2) NOT NULL,
    [ShippingFee] DECIMAL(18, 2) NOT NULL,
    [PlatformFee] DECIMAL(18, 2) DEFAULT 0,
    
    [Status] NVARCHAR(50) NOT NULL DEFAULT 'PENDING_PAYMENT' 
    CONSTRAINT [CK_OrderStatus] CHECK ([Status] IN ('PENDING_PAYMENT', 'PAID', 'SHIPPED', 'DELIVERED', 'CANCELLED', 'COMPLETED', 'RETURN_REQUESTED', 'RETURN_IN_PROGRESS', 'REFUNDED', 'PARTIALLY_REFUNDED', 'DISPUTE_OPENED')),
    
    [PaymentStatus] NVARCHAR(50) DEFAULT 'UNPAID'
    CONSTRAINT [CK_PaymentStatus] CHECK ([PaymentStatus] IN ('UNPAID', 'PAID', 'REFUNDED')),
    
    [ShippingCarrier] NVARCHAR(100),
    [TrackingCode] NVARCHAR(100),
    [ReceiverInfo] NVARCHAR(MAX) CHECK (ISJSON([ReceiverInfo]) = 1),
    [IsEscrowReleased] BIT NOT NULL DEFAULT 0, -- NEW
    
    [CreatedAt] DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET(),
    [PaidAt] DATETIMEOFFSET NULL,
    [ShippedAt] DATETIMEOFFSET NULL,
    [CompletedAt] DATETIMEOFFSET NULL,
    [RowVersion] ROWVERSION NOT NULL -- CONCURRENCY (Sync with Domain)
);

/* BẢNG 10: OrderItems (Chi tiết đơn hàng) */
CREATE TABLE [OrderItems] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [OrderId] UNIQUEIDENTIFIER NOT NULL REFERENCES [Orders]([Id]),
    [ProductId] UNIQUEIDENTIFIER NOT NULL REFERENCES [Products]([Id]),
    [VariantId] UNIQUEIDENTIFIER NOT NULL REFERENCES [ProductVariants]([Id]),
    
    [ProductNameSnapshot] NVARCHAR(255),
    [Quantity] INT NOT NULL,
    [PriceAtPurchase] DECIMAL(18, 2) NOT NULL,
    
    [TotalLineAmount] AS ([Quantity] * [PriceAtPurchase]) PERSISTED
);

/* BẢNG 11: Vouchers (Mã giảm giá) */
CREATE TABLE [Vouchers] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [ShopId] UNIQUEIDENTIFIER NOT NULL REFERENCES [Shops]([Id]),
    [Code] NVARCHAR(50) NOT NULL,
    [DiscountType] NVARCHAR(20) DEFAULT 'PERCENTAGE',
    [Value] DECIMAL(18, 2) NOT NULL,
    [MinOrderValue] DECIMAL(18, 2) DEFAULT 0,
    [UsageLimit] INT DEFAULT 100,
    [UsedCount] INT DEFAULT 0,
    [ValidFrom] DATETIMEOFFSET NOT NULL,
    [ValidTo] DATETIMEOFFSET NOT NULL,
    [IsActive] BIT DEFAULT 1,
    [RowVersion] ROWVERSION NOT NULL
);

-- ==============================================================================
-- MODULE 5: TÀI CHÍNH & ĐỐI SOÁT (FINANCIAL LEDGER)
-- ==============================================================================

/* BẢNG 12: SellerWallets (Ví tiền) */
CREATE TABLE [SellerWallets] (
    [ShopId] UNIQUEIDENTIFIER PRIMARY KEY REFERENCES [Shops]([Id]),
    [Currency] NVARCHAR(10) DEFAULT 'VND', -- NEW
    [AvailableBalance] DECIMAL(18, 2) DEFAULT 0 CHECK ([AvailableBalance] >= 0),
    [PendingBalance] DECIMAL(18, 2) DEFAULT 0 CHECK ([PendingBalance] >= 0),
    [UpdatedAt] DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET(),
    [RowVersion] ROWVERSION NOT NULL -- CONCURRENCY
);

/* BẢNG 13: WalletTransactions (Sổ cái giao dịch) */
CREATE TABLE [WalletTransactions] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [ShopId] UNIQUEIDENTIFIER NOT NULL REFERENCES [Shops]([Id]),
    
    [Amount] DECIMAL(18, 2) NOT NULL,
    [Type] NVARCHAR(50) NOT NULL, -- 'ORDER_INCOME', 'WITHDRAW', 'REFUND', 'PLATFORM_FEE'
    [Status] NVARCHAR(50) DEFAULT 'COMPLETED', -- NEW (PENDING, COMPLETED, CANCELLED)
    [ReferenceId] UNIQUEIDENTIFIER,
    [ReferenceType] NVARCHAR(50), 
    [Description] NVARCHAR(255),
    
    [BalanceAfter] DECIMAL(18, 2) NOT NULL,
    [CreatedAt] DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET()
);

-- ==============================================================================
-- MODULE 6: BÁO CÁO & TƯƠNG TÁC (ANALYTICS & EXTRAS)
-- ==============================================================================

/* BẢNG 14: ShopAnalyticsDaily (Báo cáo tổng hợp) */
CREATE TABLE [ShopAnalyticsDaily] (
    [ShopId] UNIQUEIDENTIFIER NOT NULL REFERENCES [Shops]([Id]),
    [ReportDate] DATE NOT NULL,
    
    [TotalRevenue] DECIMAL(18, 2) DEFAULT 0,
    [TotalOrders] INT DEFAULT 0,
    [ItemsSold] INT DEFAULT 0,
    [ViewsCount] INT DEFAULT 0,
    
    PRIMARY KEY ([ShopId], [ReportDate])
);

/* BẢNG 15: Reviews (Đánh giá & Phản hồi) */
CREATE TABLE [Reviews] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [OrderId] UNIQUEIDENTIFIER NOT NULL REFERENCES [Orders]([Id]),
    [ProductId] UNIQUEIDENTIFIER NOT NULL REFERENCES [Products]([Id]),
    [BuyerId] UNIQUEIDENTIFIER NOT NULL REFERENCES [Users]([Id]),
    
    [Rating] INT CHECK (Rating BETWEEN 1 AND 5),
    [Comment] NVARCHAR(MAX),
    [SellerReply] NVARCHAR(MAX), 
    [RepliedAt] DATETIMEOFFSET,
    
    [CreatedAt] DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET()
);

/* BẢNG 16: ProductViewLogs (Lượt xem sự kiện) */
CREATE TABLE [ProductViewLogs] (
    [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
    [ShopId] UNIQUEIDENTIFIER NOT NULL REFERENCES [Shops]([Id]),
    [ProductId] UNIQUEIDENTIFIER NOT NULL REFERENCES [Products]([Id]),
    [ViewerIP] NVARCHAR(50) NULL,
    [ViewedAt] DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET()
);
GO
