-- ============================================================
-- MIGRATION: Performance & Analytics Phase 1
-- Date: 2026-03-22
-- Author: Auto-generated from Phase 1 Implementation Plan
-- Description: 
--   1. Tạo bảng SellerDefects (ghi nhận defect seller)
--   2. Thêm IsLateShipment vào Orders
--   3. Thêm denormalized counts vào Shops
--   4. Seed denormalized counts từ data hiện có
-- ============================================================

-- === 1. BẢNG SELLER DEFECTS ===
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SellerDefects')
BEGIN
    CREATE TABLE SellerDefects (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        ShopId UNIQUEIDENTIFIER NOT NULL,
        OrderId UNIQUEIDENTIFIER NOT NULL,
        BuyerId UNIQUEIDENTIFIER NOT NULL,
        DefectType NVARCHAR(30) NOT NULL,
        Description NVARCHAR(500) NULL,
        CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        
        CONSTRAINT FK_SellerDefects_Shops FOREIGN KEY (ShopId) REFERENCES Shops(Id),
        CONSTRAINT FK_SellerDefects_Orders FOREIGN KEY (OrderId) REFERENCES Orders(Id)
    );
    
    -- [PERFORMANCE] Composite index cho fast count trong evaluation period
    CREATE NONCLUSTERED INDEX IX_SellerDefects_ShopId_CreatedAt 
        ON SellerDefects(ShopId, CreatedAt DESC);
    
    PRINT 'Created table SellerDefects with composite index';
END
GO

-- === 2. THÊM CỘT IsLateShipment VÀO ORDERS ===
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'IsLateShipment')
BEGIN
    ALTER TABLE Orders ADD IsLateShipment BIT NOT NULL DEFAULT 0;
    PRINT 'Added IsLateShipment column to Orders';
END
GO

-- === 3. THÊM CỘT DENORMALIZED VÀO SHOPS ===
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Shops') AND name = 'ActiveListingCount')
BEGIN
    ALTER TABLE Shops ADD ActiveListingCount INT NOT NULL DEFAULT 0;
    PRINT 'Added ActiveListingCount column to Shops';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Shops') AND name = 'DraftListingCount')
BEGIN
    ALTER TABLE Shops ADD DraftListingCount INT NOT NULL DEFAULT 0;
    PRINT 'Added DraftListingCount column to Shops';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Shops') AND name = 'AwaitingShipmentCount')
BEGIN
    ALTER TABLE Shops ADD AwaitingShipmentCount INT NOT NULL DEFAULT 0;
    PRINT 'Added AwaitingShipmentCount column to Shops';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Shops') AND name = 'LateShipmentCount')
BEGIN
    ALTER TABLE Shops ADD LateShipmentCount INT NOT NULL DEFAULT 0;
    PRINT 'Added LateShipmentCount column to Shops';
END
GO

-- === 4. SEED DENORMALIZED COUNTS TỪ DATA HIỆN CÓ ===
-- Đây là one-time seed để denormalized counts khớp với data thực tế
UPDATE s SET 
    s.ActiveListingCount = ISNULL((
        SELECT COUNT(*) FROM Products p 
        WHERE p.ShopId = s.Id AND p.Status = 'ACTIVE' AND p.IsDeleted = 0
    ), 0),
    s.DraftListingCount = ISNULL((
        SELECT COUNT(*) FROM Products p 
        WHERE p.ShopId = s.Id AND p.Status = 'DRAFT' AND p.IsDeleted = 0
    ), 0),
    s.AwaitingShipmentCount = ISNULL((
        SELECT COUNT(*) FROM Orders o 
        WHERE o.ShopId = s.Id AND o.Status = 'PAID'
    ), 0),
    s.LateShipmentCount = 0
FROM Shops s;

PRINT 'Seeded denormalized counts from existing data';
GO

-- === 5. XÓA BẢNG REVIEWS (đã thay thế bởi Feedbacks) ===
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Reviews')
BEGIN
    DROP TABLE Reviews;
    PRINT 'Dropped legacy Reviews table (replaced by Feedbacks)';
END
GO

PRINT '=== Migration Phase 1 Performance & Analytics COMPLETED ===';
GO
