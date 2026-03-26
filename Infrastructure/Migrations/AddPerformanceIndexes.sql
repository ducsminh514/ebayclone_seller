-- ============================================================
-- Performance Indexes — Phase 1
-- Chạy script này trên SQL Server để thêm các index tối ưu query
-- ============================================================

-- 1. Orders: ShopId cho tất cả queries filter theo shop
-- Hỗ trợ: GetOrdersByShopIdAsync, GetPagedOrdersByShopIdAsync, CountByStatusAsync
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Orders_ShopId_Status_CreatedAt' AND object_id = OBJECT_ID('Orders'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Orders_ShopId_Status_CreatedAt 
    ON Orders(ShopId, Status) 
    INCLUDE (CreatedAt, TotalAmount, BuyerId);
    PRINT 'Created: IX_Orders_ShopId_Status_CreatedAt';
END
GO

-- 2. Orders: IdempotencyKey cho duplicate detection
-- IdempotencyKey là NVARCHAR(MAX) → không thể tạo index trực tiếp
-- Giải pháp: tạo computed column CHECKSUM rồi index trên đó
-- CHECKSUM nhanh hơn HASHBYTES nhưng có collision risk nhỏ → đủ cho duplicate detection heuristic
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = 'IdempotencyKeyHash' AND object_id = OBJECT_ID('Orders'))
BEGIN
    ALTER TABLE Orders ADD IdempotencyKeyHash AS CHECKSUM(IdempotencyKey) PERSISTED;
    PRINT 'Created: IdempotencyKeyHash computed column';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Orders_IdempotencyKeyHash' AND object_id = OBJECT_ID('Orders'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Orders_IdempotencyKeyHash 
    ON Orders(IdempotencyKeyHash);
    PRINT 'Created: IX_Orders_IdempotencyKeyHash';
END
GO

-- 3. Orders: Fund release query optimization
-- Hỗ trợ: GetOrdersEligibleForFundReleaseAsync (filter Status=DELIVERED, IsEscrowReleased=0)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Orders_FundRelease' AND object_id = OBJECT_ID('Orders'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Orders_FundRelease 
    ON Orders(Status, IsEscrowReleased, DeliveredAt) 
    INCLUDE (ShopId, TotalAmount)
    WHERE Status = 'DELIVERED' AND IsEscrowReleased = 0;
    PRINT 'Created: IX_Orders_FundRelease';
END
GO

-- 4. ProductViewLogs: Daily analytics aggregation
-- Hỗ trợ: ComputeDailyAnalyticsService (GROUP BY ShopId WHERE ViewedAt.Date = targetDate)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProductViewLogs_ViewedAt_ShopId' AND object_id = OBJECT_ID('ProductViewLogs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ProductViewLogs_ViewedAt_ShopId 
    ON ProductViewLogs(ViewedAt) 
    INCLUDE (ShopId, ProductId);
    PRINT 'Created: IX_ProductViewLogs_ViewedAt_ShopId';
END
GO

-- 5. ProductViewLogs: Rate limit check (HasRecentView)
-- Hỗ trợ: TrackProductViewUseCase — kiểm tra xem IP đã view gần đây chưa
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProductViewLogs_RateLimit' AND object_id = OBJECT_ID('ProductViewLogs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ProductViewLogs_RateLimit 
    ON ProductViewLogs(ProductId, ViewerIP, ViewedAt);
    PRINT 'Created: IX_ProductViewLogs_RateLimit';
END
GO

-- 6. ProductViewLogs: ShopId + ViewedAt cho traffic stats per shop
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProductViewLogs_ShopId_ViewedAt' AND object_id = OBJECT_ID('ProductViewLogs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ProductViewLogs_ShopId_ViewedAt 
    ON ProductViewLogs(ShopId, ViewedAt);
    PRINT 'Created: IX_ProductViewLogs_ShopId_ViewedAt';
END
GO

PRINT 'All performance indexes created successfully.';
