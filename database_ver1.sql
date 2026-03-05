/* =================================================================================
PROJECT: EBAY CLONE - SELLER HUB MODULE
DATABASE SYSTEM: SQL SERVER (T-SQL)
DESCRIPTION: Hệ thống lõi quản lý bán hàng, tồn kho và dòng tiền dành cho Seller.
CREATED BY: Gemini (Based on User Requirements)
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

/* BẢNG 1: Users (Người dùng)
---------------------------
- Nghiệp vụ: Quản lý danh tính.
- Lưu ý: 'Role' phân biệt người mua/người bán. 'IsVerified' cực kỳ quan trọng với Seller 
  (phải xác minh danh tính mới được đăng bán để chống lừa đảo).
*/
CREATE TABLE [Users] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [Username] NVARCHAR(100) NOT NULL,
    [Email] NVARCHAR(255) NOT NULL UNIQUE,
    [PasswordHash] NVARCHAR(MAX) NOT NULL,
    [FullName] NVARCHAR(200),
    [IsEmailVerified] BIT DEFAULT 0,
    [IsIdentityVerified] BIT DEFAULT 0, -- Đã xác minh KYC (Căn cước/Hộ chiếu) chưa?
    [Role] NVARCHAR(50) DEFAULT 'SELLER', -- 'ADMIN', 'BUYER', 'SELLER'
    [CreatedAt] DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET(),
    [UpdatedAt] DATETIMEOFFSET
);

/* BẢNG 2: Files (Quản lý Media)
-----------------------------
- Nghiệp vụ: Lưu trữ tập trung mọi hình ảnh/video của hệ thống.
- Lưu ý: Không lưu file binary vào DB. Chỉ lưu URL (link tới AWS S3, Cloudinary, Firebase).
*/
CREATE TABLE [Files] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [OwnerId] UNIQUEIDENTIFIER NOT NULL REFERENCES [Users]([Id]), -- Ai upload?
    [Url] NVARCHAR(MAX) NOT NULL, -- Link ảnh
    [Type] NVARCHAR(50), -- 'PRODUCT_IMAGE', 'AVATAR', 'DOCUMENT'
    [CreatedAt] DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET()
);

/* BẢNG 3: Categories (Danh mục ngành hàng)
----------------------------------------
- Nghiệp vụ: Cây thư mục sản phẩm (VD: Điện tử -> Điện thoại -> iPhone).
- Lưu ý: Sử dụng đệ quy (ParentId) để tạo n cấp độ con.
*/
CREATE TABLE [Categories] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [ParentId] UNIQUEIDENTIFIER NULL REFERENCES [Categories]([Id]), -- Null là danh mục gốc
    [Name] NVARCHAR(255) NOT NULL,
    [Slug] NVARCHAR(255) NOT NULL,
    [IsActive] BIT DEFAULT 1
);

-- ==============================================================================
-- MODULE 2: THIẾT LẬP CỬA HÀNG (STORE SETTINGS)
-- ==============================================================================

/* BẢNG 4: Shops (Hồ sơ cửa hàng)
------------------------------
- Nghiệp vụ: Đại diện thương hiệu của Seller trên sàn.
- Lưu ý: 'RatingAvg' được tính toán và lưu lại (Cache) để hiển thị nhanh, 
  không cần tính lại mỗi lần user load trang.
*/
CREATE TABLE [Shops] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [OwnerId] UNIQUEIDENTIFIER NOT NULL REFERENCES [Users]([Id]),
    [Name] NVARCHAR(255) NOT NULL,
    [Description] NVARCHAR(MAX),
    [AvatarUrl] NVARCHAR(MAX),
    [BannerUrl] NVARCHAR(MAX),
    [IsActive] BIT DEFAULT 1,
    [RatingAvg] DECIMAL(3, 2) DEFAULT 0, -- Điểm uy tín (0.0 -> 5.0)
    [CreatedAt] DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET()
);

/* BẢNG 5: ShippingPolicies (Chính sách vận chuyển)
------------------------------------------------
- Nghiệp vụ: Giúp Seller cấu hình phí ship 1 lần và áp dụng cho hàng nghìn sản phẩm.
- Ví dụ: Policy A: "Giao nhanh $5", Policy B: "Free Ship".
*/
CREATE TABLE [ShippingPolicies] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [ShopId] UNIQUEIDENTIFIER NOT NULL REFERENCES [Shops]([Id]),
    [Name] NVARCHAR(100) NOT NULL, 
    [HandlingTimeDays] INT DEFAULT 2, -- Thời gian chuẩn bị hàng (Cam kết với khách)
    [Cost] DECIMAL(18, 2) DEFAULT 0,
    [IsDefault] BIT DEFAULT 0 -- Policy mặc định khi tạo SP mới
);

/* BẢNG 6: ReturnPolicies (Chính sách đổi trả)
-------------------------------------------
- Nghiệp vụ: Quy định rõ luật chơi trước khi bán. Rất quan trọng khi xảy ra tranh chấp (Dispute).
- Ví dụ: "Không đổi trả" hoặc "Cho phép đổi trong 30 ngày, người mua chịu phí ship".
*/
CREATE TABLE [ReturnPolicies] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [ShopId] UNIQUEIDENTIFIER NOT NULL REFERENCES [Shops]([Id]),
    [Name] NVARCHAR(100) NOT NULL,
    [ReturnDays] INT DEFAULT 0, -- 0 = No Returns
    [ShippingPaidBy] NVARCHAR(20) CHECK (ShippingPaidBy IN ('BUYER', 'SELLER')) -- Ai trả tiền ship hoàn về?
);

-- ==============================================================================
-- MODULE 3: SẢN PHẨM & TỒN KHO (CORE INVENTORY) - QUAN TRỌNG NHẤT
-- ==============================================================================

/* BẢNG 7: Products (Sản phẩm gốc - Parent)
----------------------------------------
- Nghiệp vụ: Chứa thông tin chung (SEO, Tên, Mô tả). Bảng này KHÔNG chứa giá và tồn kho cụ thể.
- Lý do: Một mẫu áo (Product) có thể có nhiều size, mỗi size là 1 Variant khác nhau.
*/
CREATE TABLE [Products] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [ShopId] UNIQUEIDENTIFIER NOT NULL REFERENCES [Shops]([Id]),
    [CategoryId] UNIQUEIDENTIFIER NOT NULL REFERENCES [Categories]([Id]),
    [ShippingPolicyId] UNIQUEIDENTIFIER REFERENCES [ShippingPolicies]([Id]),
    [ReturnPolicyId] UNIQUEIDENTIFIER REFERENCES [ReturnPolicies]([Id]),
    
    [Name] NVARCHAR(255) NOT NULL, -- Tên hiển thị (VD: Áo thun Nike Air)
    [Description] NVARCHAR(MAX), -- Mô tả HTML
    [Brand] NVARCHAR(100),
    [Status] NVARCHAR(20) DEFAULT 'DRAFT', -- DRAFT, ACTIVE, HIDDEN, BANNED
    [BasePrice] DECIMAL(18, 2), -- Giá tham khảo hiển thị bên ngoài
    
    [CreatedAt] DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET(),
    [UpdatedAt] DATETIMEOFFSET
);

/* BẢNG 8: ProductVariants (Biến thể/SKU - Child)
----------------------------------------------
- Nghiệp vụ: Đây là hàng hóa thực tế trong kho.
- Cải tiến:
  + Attributes (JSON): Lưu {"Color": "Red", "Size": "M", "Material": "Cotton"}. Không cần tạo nhiều cột cứng.
  + AvailableStock (Computed): Tự động tính = Quantity - Reserved. Giúp frontend biết chính xác còn bán được bao nhiêu.
*/
CREATE TABLE [ProductVariants] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [ProductId] UNIQUEIDENTIFIER NOT NULL REFERENCES [Products]([Id]),
    
    [SkuCode] NVARCHAR(100) NOT NULL, -- Mã quản lý nội bộ duy nhất (VD: NK-AIR-RED-M)
    [Price] DECIMAL(18, 2) NOT NULL, -- Giá bán thực tế của biến thể này
    [Attributes] NVARCHAR(MAX) CHECK (ISJSON([Attributes]) = 1), -- JSON thuộc tính
    
    -- LOGIC KHO HÀNG ATOMIC (CHỐNG BÁN LỐ)
    [Quantity] INT NOT NULL DEFAULT 0, -- Tổng số lượng đang có trong kho
    [ReservedQuantity] INT NOT NULL DEFAULT 0, -- Số lượng đang nằm trong đơn chờ thanh toán
    
    -- Cột ảo: Số lượng thực sự có thể bán
    [AvailableStock] AS ([Quantity] - [ReservedQuantity]), 
    
    [ImageUrl] NVARCHAR(MAX), -- Ảnh riêng cho biến thể (VD: Ảnh áo màu đỏ)
    [WeightGram] INT, -- Cân nặng để tính phí ship
    
    CONSTRAINT [CK_Stock_Valid] CHECK ([Quantity] >= 0 AND [ReservedQuantity] >= 0)
);
CREATE INDEX [IX_Variants_Sku] ON [ProductVariants]([SkuCode]);

-- ==============================================================================
-- MODULE 4: ĐƠN HÀNG & DÒNG TIỀN (ORDER FLOW)
-- ==============================================================================

/* BẢNG 9: Orders (Đơn hàng)
-------------------------
- Nghiệp vụ: Quản lý vòng đời đơn hàng. Trạng thái đơn hàng (Status) sẽ kích hoạt các thay đổi trong Ví tiền.
- State Machine: PENDING -> PAID -> SHIPPED -> DELIVERED.
*/
CREATE TABLE [Orders] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [OrderNumber] NVARCHAR(50) NOT NULL UNIQUE, -- Mã đơn user thấy (VD: #ORD-2026-001)
    [ShopId] UNIQUEIDENTIFIER NOT NULL REFERENCES [Shops]([Id]),
    [BuyerId] UNIQUEIDENTIFIER NOT NULL REFERENCES [Users]([Id]),
    
    -- Tiền nong
    [TotalAmount] DECIMAL(18, 2) NOT NULL, -- Tổng khách phải trả
    [ShippingFee] DECIMAL(18, 2) NOT NULL,
    [PlatformFee] DECIMAL(18, 2) DEFAULT 0, -- Phí sàn thu của Seller (VD: 5%)
    
    -- Trạng thái: ÉP BUỘC RÀNG BUỘC THEO CƠ CHẾ STATE MACHINE
    [Status] NVARCHAR(50) NOT NULL DEFAULT 'PENDING_PAYMENT' 
    CONSTRAINT [CK_OrderStatus] CHECK ([Status] IN ('PENDING_PAYMENT', 'READY_TO_SHIP', 'PROCESSING', 'SHIPPED', 'DELIVERED', 'CANCELLED', 'RETURNED')),
    
    [PaymentStatus] NVARCHAR(50) DEFAULT 'UNPAID'
    CONSTRAINT [CK_PaymentStatus] CHECK ([PaymentStatus] IN ('UNPAID', 'PAID', 'REFUNDED')),
    
    -- Vận chuyển
    [ShippingCarrier] NVARCHAR(100), -- Đơn vị vận chuyển (GHN, UPS, FedEx)
    [TrackingCode] NVARCHAR(100), -- Mã vận đơn
    [ReceiverInfo] NVARCHAR(MAX) CHECK (ISJSON([ReceiverInfo]) = 1), -- Snapshot địa chỉ người nhận (JSON)
    
    [CreatedAt] DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET(),
    [PaidAt] DATETIMEOFFSET,
    [ShippedAt] DATETIMEOFFSET,
    [CompletedAt] DATETIMEOFFSET -- Lúc này tiền mới được chuyển vào ví khả dụng
);

/* BẢNG 10: OrderItems (Chi tiết đơn hàng)
---------------------------------------
- Nghiệp vụ: Lưu trữ chính xác khách mua cái gì.
- Cải tiến: 'PriceAtPurchase' (Snapshot). Nếu Seller đổi giá SP sau khi bán, giá trong đơn này KHÔNG được đổi.
*/
CREATE TABLE [OrderItems] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [OrderId] UNIQUEIDENTIFIER NOT NULL REFERENCES [Orders]([Id]),
    [ProductId] UNIQUEIDENTIFIER NOT NULL REFERENCES [Products]([Id]),
    [VariantId] UNIQUEIDENTIFIER NOT NULL REFERENCES [ProductVariants]([Id]),
    
    [ProductNameSnapshot] NVARCHAR(255), -- Lưu cứng tên SP lúc mua
    [Quantity] INT NOT NULL,
    [PriceAtPurchase] DECIMAL(18, 2) NOT NULL, -- QUAN TRỌNG: Giá chốt tại thời điểm mua
    
    [TotalLineAmount] AS ([Quantity] * [PriceAtPurchase]) PERSISTED -- Tự động tính toán
);

/* BẢNG 11: Vouchers (Mã giảm giá)
-------------------------------
- Nghiệp vụ: Công cụ Marketing.
- Lưu ý: 'UsageLimit' để giới hạn số lượng mã (VD: Chỉ 100 người nhanh nhất).
*/
CREATE TABLE [Vouchers] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [ShopId] UNIQUEIDENTIFIER NOT NULL REFERENCES [Shops]([Id]),
    [Code] NVARCHAR(50) NOT NULL, -- VD: SALE50
    [DiscountType] NVARCHAR(20) DEFAULT 'PERCENTAGE', -- 'FIXED_AMOUNT' hoặc 'PERCENTAGE'
    [Value] DECIMAL(18, 2) NOT NULL, -- Giá trị giảm (VD: 10% hoặc 50k)
    [MinOrderValue] DECIMAL(18, 2) DEFAULT 0,
    [UsageLimit] INT DEFAULT 100, -- Tổng số mã
    [UsedCount] INT DEFAULT 0, -- Số mã đã dùng
    [ValidFrom] DATETIMEOFFSET NOT NULL,
    [ValidTo] DATETIMEOFFSET NOT NULL,
    [IsActive] BIT DEFAULT 1,
    
    -- CƠ CHẾ OPTIMISTIC LOCKING CHỐNG RACE CONDITION KHI APPLICATON UPDATE
    [RowVersion] ROWVERSION NOT NULL
);

-- ==============================================================================
-- MODULE 5: TÀI CHÍNH & ĐỐI SOÁT (FINANCIAL LEDGER)
-- ==============================================================================

/* BẢNG 12: SellerWallets (Ví tiền)
--------------------------------
- Nghiệp vụ: Quản lý số dư hiện tại của Shop.
- Logic Escrow: 
  + PendingBalance: Tiền từ đơn hàng đang giao (chưa được rút).
  + AvailableBalance: Tiền sạch (đã giao hàng xong + hết hạn khiếu nại), được phép rút.
*/
CREATE TABLE [SellerWallets] (
    [ShopId] UNIQUEIDENTIFIER PRIMARY KEY REFERENCES [Shops]([Id]),
    [AvailableBalance] DECIMAL(18, 2) DEFAULT 0 CHECK ([AvailableBalance] >= 0),
    [PendingBalance] DECIMAL(18, 2) DEFAULT 0 CHECK ([PendingBalance] >= 0),
    [UpdatedAt] DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET()
);

/* BẢNG 13: WalletTransactions (Sổ cái giao dịch)
----------------------------------------------
- Nghiệp vụ: Lịch sử dòng tiền bất biến. Bảng này CẤM UPDATE/DELETE.
- Tác dụng: Dùng để đối soát khi Seller thắc mắc "Tại sao tiền tôi bị trừ?".
*/
CREATE TABLE [WalletTransactions] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [ShopId] UNIQUEIDENTIFIER NOT NULL REFERENCES [Shops]([Id]),
    
    [Amount] DECIMAL(18, 2) NOT NULL, -- (+) Tiền vào, (-) Tiền ra
    [Type] NVARCHAR(50) NOT NULL, -- 'ORDER_INCOME', 'WITHDRAW', 'REFUND', 'PLATFORM_FEE'
    [ReferenceId] UNIQUEIDENTIFIER, -- Link tới ID cụ thể
    [ReferenceType] NVARCHAR(50), -- ('ORDER', 'WITHDRAW_REQUEST') Để code dễ Audit Trail
    [Description] NVARCHAR(255), -- VD: "Thu nhập đơn hàng #123"
    
    [BalanceAfter] DECIMAL(18, 2) NOT NULL, -- Snapshot số dư sau giao dịch
    [CreatedAt] DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET()
);

-- ==============================================================================
-- MODULE 6: BÁO CÁO & TƯƠNG TÁC (ANALYTICS & EXTRAS)
-- ==============================================================================

/* BẢNG 14: ShopAnalyticsDaily (Báo cáo tổng hợp)
----------------------------------------------
- Nghiệp vụ: Bảng này dùng để vẽ Dashboard.
- Cơ chế: Hệ thống (Job) sẽ chạy mỗi đêm, tính tổng doanh thu/đơn hàng trong ngày và ghi 1 dòng vào đây.
- Lợi ích: Load Dashboard cực nhanh (dưới 1s) vì không phải Query bảng Orders khổng lồ.
*/
CREATE TABLE [ShopAnalyticsDaily] (
    [ShopId] UNIQUEIDENTIFIER NOT NULL REFERENCES [Shops]([Id]),
    [ReportDate] DATE NOT NULL,
    
    [TotalRevenue] DECIMAL(18, 2) DEFAULT 0,
    [TotalOrders] INT DEFAULT 0,
    [ItemsSold] INT DEFAULT 0,
    [ViewsCount] INT DEFAULT 0, -- Số lượt xem Shop
    
    PRIMARY KEY ([ShopId], [ReportDate])
);

/* BẢNG 15: Reviews (Đánh giá & Phản hồi)
---------------------------------------
- Nghiệp vụ: Hệ thống uy tín.
- Lưu ý: 'SellerReply' cho phép Seller giải trình khi bị đánh giá xấu (Tăng Trust).
*/
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

/* BẢNG 16: ProductViewLogs (Khắc phục cổ chai ROW LOCK)
---------------------------------------
- Nghiệp vụ: Ghi nhận lượt xem sản phẩm dưới dạng chuỗi sự kiện.
- Đặc tả: Khách vào xem thì Insert trực tiếp 1 dòng (Append-Only) thay vì Update `ViewsCount`.
- Tối ưu RAM: Một Job hàng đêm sẽ gom (Aggregate) lượt view từ bảng này dội vào `ShopAnalyticsDaily` và có thể xóa đi để giải phóng bộ nhớ.
*/
CREATE TABLE [ProductViewLogs] (
    [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
    [ShopId] UNIQUEIDENTIFIER NOT NULL REFERENCES [Shops]([Id]),
    [ProductId] UNIQUEIDENTIFIER NOT NULL REFERENCES [Products]([Id]),
    [ViewerIP] NVARCHAR(50) NULL,
    [ViewedAt] DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET()
);

GO
