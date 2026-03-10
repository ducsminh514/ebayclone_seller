USE [master]
GO
/****** Object:  Database [EbaySellerClone]    Script Date: 10/03/2026 4:17:34 CH ******/
CREATE DATABASE [EbaySellerClone]
 
GO
ALTER DATABASE [EbaySellerClone] SET COMPATIBILITY_LEVEL = 160
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [EbaySellerClone].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [EbaySellerClone] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [EbaySellerClone] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [EbaySellerClone] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [EbaySellerClone] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [EbaySellerClone] SET ARITHABORT OFF 
GO
ALTER DATABASE [EbaySellerClone] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [EbaySellerClone] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [EbaySellerClone] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [EbaySellerClone] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [EbaySellerClone] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [EbaySellerClone] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [EbaySellerClone] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [EbaySellerClone] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [EbaySellerClone] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [EbaySellerClone] SET  ENABLE_BROKER 
GO
ALTER DATABASE [EbaySellerClone] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [EbaySellerClone] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [EbaySellerClone] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [EbaySellerClone] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [EbaySellerClone] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [EbaySellerClone] SET READ_COMMITTED_SNAPSHOT OFF 
GO
ALTER DATABASE [EbaySellerClone] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [EbaySellerClone] SET RECOVERY FULL 
GO
ALTER DATABASE [EbaySellerClone] SET  MULTI_USER 
GO
ALTER DATABASE [EbaySellerClone] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [EbaySellerClone] SET DB_CHAINING OFF 
GO
ALTER DATABASE [EbaySellerClone] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [EbaySellerClone] SET TARGET_RECOVERY_TIME = 60 SECONDS 
GO
ALTER DATABASE [EbaySellerClone] SET DELAYED_DURABILITY = DISABLED 
GO
ALTER DATABASE [EbaySellerClone] SET ACCELERATED_DATABASE_RECOVERY = OFF  
GO
EXEC sys.sp_db_vardecimal_storage_format N'EbaySellerClone', N'ON'
GO
ALTER DATABASE [EbaySellerClone] SET QUERY_STORE = ON
GO
ALTER DATABASE [EbaySellerClone] SET QUERY_STORE (OPERATION_MODE = READ_WRITE, CLEANUP_POLICY = (STALE_QUERY_THRESHOLD_DAYS = 30), DATA_FLUSH_INTERVAL_SECONDS = 900, INTERVAL_LENGTH_MINUTES = 60, MAX_STORAGE_SIZE_MB = 1000, QUERY_CAPTURE_MODE = AUTO, SIZE_BASED_CLEANUP_MODE = AUTO, MAX_PLANS_PER_QUERY = 200, WAIT_STATS_CAPTURE_MODE = ON)
GO
USE [EbaySellerClone]
GO
/****** Object:  Table [dbo].[__EFMigrationsHistory]    Script Date: 10/03/2026 4:17:34 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[__EFMigrationsHistory](
	[MigrationId] [nvarchar](150) NOT NULL,
	[ProductVersion] [nvarchar](32) NOT NULL,
 CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY CLUSTERED 
(
	[MigrationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Categories]    Script Date: 10/03/2026 4:17:34 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Categories](
	[Id] [uniqueidentifier] NOT NULL,
	[ParentId] [uniqueidentifier] NULL,
	[Name] [nvarchar](255) NOT NULL,
	[Slug] [nvarchar](255) NOT NULL,
	[IsActive] [bit] NULL,
	[AttributeHints] [nvarchar](max) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Files]    Script Date: 10/03/2026 4:17:34 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Files](
	[Id] [uniqueidentifier] NOT NULL,
	[OwnerId] [uniqueidentifier] NOT NULL,
	[Url] [nvarchar](max) NOT NULL,
	[Type] [nvarchar](50) NULL,
	[CreatedAt] [datetimeoffset](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[OrderItems]    Script Date: 10/03/2026 4:17:34 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[OrderItems](
	[Id] [uniqueidentifier] NOT NULL,
	[OrderId] [uniqueidentifier] NOT NULL,
	[ProductId] [uniqueidentifier] NOT NULL,
	[VariantId] [uniqueidentifier] NOT NULL,
	[ProductNameSnapshot] [nvarchar](255) NULL,
	[Quantity] [int] NOT NULL,
	[PriceAtPurchase] [decimal](18, 2) NOT NULL,
	[TotalLineAmount]  AS ([Quantity]*[PriceAtPurchase]) PERSISTED,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Orders]    Script Date: 10/03/2026 4:17:34 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Orders](
	[Id] [uniqueidentifier] NOT NULL,
	[OrderNumber] [nvarchar](50) NOT NULL,
	[ShopId] [uniqueidentifier] NOT NULL,
	[BuyerId] [uniqueidentifier] NOT NULL,
	[TotalAmount] [decimal](18, 2) NOT NULL,
	[ShippingFee] [decimal](18, 2) NOT NULL,
	[PlatformFee] [decimal](18, 2) NULL,
	[Status] [nvarchar](50) NOT NULL,
	[PaymentStatus] [nvarchar](50) NULL,
	[ShippingCarrier] [nvarchar](100) NULL,
	[TrackingCode] [nvarchar](100) NULL,
	[ReceiverInfo] [nvarchar](max) NULL,
	[CreatedAt] [datetimeoffset](7) NULL,
	[PaidAt] [datetimeoffset](7) NULL,
	[ShippedAt] [datetimeoffset](7) NULL,
	[CompletedAt] [datetimeoffset](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[OrderNumber] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Products]    Script Date: 10/03/2026 4:17:34 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Products](
	[Id] [uniqueidentifier] NOT NULL,
	[ShopId] [uniqueidentifier] NOT NULL,
	[CategoryId] [uniqueidentifier] NOT NULL,
	[ShippingPolicyId] [uniqueidentifier] NULL,
	[ReturnPolicyId] [uniqueidentifier] NULL,
	[Name] [nvarchar](255) NOT NULL,
	[Description] [nvarchar](max) NULL,
	[Brand] [nvarchar](100) NULL,
	[Status] [nvarchar](20) NULL,
	[BasePrice] [decimal](18, 2) NULL,
	[CreatedAt] [datetimeoffset](7) NULL,
	[UpdatedAt] [datetimeoffset](7) NULL,
	[PrimaryImageUrl] [nvarchar](500) NULL,
	[ImageUrls] [nvarchar](max) NULL,
	[IsDeleted] [bit] NOT NULL,
	[ScheduledAt] [datetimeoffset](7) NULL,
	[RowVersion] [timestamp] NOT NULL,
	[LastModifiedBy] [nvarchar](255) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProductVariants]    Script Date: 10/03/2026 4:17:34 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProductVariants](
	[Id] [uniqueidentifier] NOT NULL,
	[ProductId] [uniqueidentifier] NOT NULL,
	[SkuCode] [nvarchar](100) NOT NULL,
	[Price] [decimal](18, 2) NOT NULL,
	[Attributes] [nvarchar](max) NULL,
	[Quantity] [int] NOT NULL,
	[ReservedQuantity] [int] NOT NULL,
	[AvailableStock]  AS ([Quantity]-[ReservedQuantity]),
	[ImageUrl] [nvarchar](max) NULL,
	[WeightGram] [int] NULL,
	[CreatedAt] [datetimeoffset](7) NOT NULL,
	[UpdatedAt] [datetimeoffset](7) NULL,
	[RowVersion] [timestamp] NOT NULL,
	[LastModifiedBy] [nvarchar](255) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProductViewLogs]    Script Date: 10/03/2026 4:17:34 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProductViewLogs](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[ShopId] [uniqueidentifier] NOT NULL,
	[ProductId] [uniqueidentifier] NOT NULL,
	[ViewerIP] [nvarchar](50) NULL,
	[ViewedAt] [datetimeoffset](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ReturnPolicies]    Script Date: 10/03/2026 4:17:34 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ReturnPolicies](
	[Id] [uniqueidentifier] NOT NULL,
	[ShopId] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[ReturnDays] [int] NULL,
	[ShippingPaidBy] [nvarchar](20) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Reviews]    Script Date: 10/03/2026 4:17:34 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Reviews](
	[Id] [uniqueidentifier] NOT NULL,
	[OrderId] [uniqueidentifier] NOT NULL,
	[ProductId] [uniqueidentifier] NOT NULL,
	[BuyerId] [uniqueidentifier] NOT NULL,
	[Rating] [int] NULL,
	[Comment] [nvarchar](max) NULL,
	[SellerReply] [nvarchar](max) NULL,
	[RepliedAt] [datetimeoffset](7) NULL,
	[CreatedAt] [datetimeoffset](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SellerWallets]    Script Date: 10/03/2026 4:17:34 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SellerWallets](
	[ShopId] [uniqueidentifier] NOT NULL,
	[AvailableBalance] [decimal](18, 2) NULL,
	[PendingBalance] [decimal](18, 2) NULL,
	[UpdatedAt] [datetimeoffset](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[ShopId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ShippingPolicies]    Script Date: 10/03/2026 4:17:34 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ShippingPolicies](
	[Id] [uniqueidentifier] NOT NULL,
	[ShopId] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[HandlingTimeDays] [int] NULL,
	[Cost] [decimal](18, 2) NULL,
	[IsDefault] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ShopAnalyticsDaily]    Script Date: 10/03/2026 4:17:34 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ShopAnalyticsDaily](
	[ShopId] [uniqueidentifier] NOT NULL,
	[ReportDate] [date] NOT NULL,
	[TotalRevenue] [decimal](18, 2) NULL,
	[TotalOrders] [int] NULL,
	[ItemsSold] [int] NULL,
	[ViewsCount] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[ShopId] ASC,
	[ReportDate] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Shops]    Script Date: 10/03/2026 4:17:34 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Shops](
	[Id] [uniqueidentifier] NOT NULL,
	[OwnerId] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](255) NOT NULL,
	[Description] [nvarchar](max) NULL,
	[AvatarUrl] [nvarchar](max) NULL,
	[BannerUrl] [nvarchar](max) NULL,
	[IsActive] [bit] NULL,
	[RatingAvg] [decimal](3, 2) NULL,
	[CreatedAt] [datetimeoffset](7) NULL,
	[TaxCode] [nvarchar](20) NULL,
	[Address] [nvarchar](255) NULL,
	[IsVerified] [bit] NULL,
	[TotalShippingPolicies] [int] NULL,
	[TotalReturnPolicies] [int] NULL,
	[MonthlyListingLimit] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Users]    Script Date: 10/03/2026 4:17:34 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Users](
	[Id] [uniqueidentifier] NOT NULL,
	[Username] [nvarchar](100) NOT NULL,
	[Email] [nvarchar](255) NOT NULL,
	[PasswordHash] [nvarchar](max) NOT NULL,
	[FullName] [nvarchar](200) NULL,
	[IsEmailVerified] [bit] NULL,
	[IsIdentityVerified] [bit] NULL,
	[Role] [nvarchar](50) NULL,
	[CreatedAt] [datetimeoffset](7) NULL,
	[UpdatedAt] [datetimeoffset](7) NULL,
	[EmailVerificationToken] [nvarchar](max) NULL,
	[EmailVerificationTokenExpiresAt] [datetimeoffset](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[Email] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Vouchers]    Script Date: 10/03/2026 4:17:34 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Vouchers](
	[Id] [uniqueidentifier] NOT NULL,
	[ShopId] [uniqueidentifier] NOT NULL,
	[Code] [nvarchar](50) NOT NULL,
	[DiscountType] [nvarchar](20) NULL,
	[Value] [decimal](18, 2) NOT NULL,
	[MinOrderValue] [decimal](18, 2) NULL,
	[UsageLimit] [int] NULL,
	[UsedCount] [int] NULL,
	[ValidFrom] [datetimeoffset](7) NOT NULL,
	[ValidTo] [datetimeoffset](7) NOT NULL,
	[IsActive] [bit] NULL,
	[RowVersion] [timestamp] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[WalletTransactions]    Script Date: 10/03/2026 4:17:34 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[WalletTransactions](
	[Id] [uniqueidentifier] NOT NULL,
	[ShopId] [uniqueidentifier] NOT NULL,
	[Amount] [decimal](18, 2) NOT NULL,
	[Type] [nvarchar](50) NOT NULL,
	[ReferenceId] [uniqueidentifier] NULL,
	[ReferenceType] [nvarchar](50) NULL,
	[Description] [nvarchar](255) NULL,
	[BalanceAfter] [decimal](18, 2) NOT NULL,
	[CreatedAt] [datetimeoffset](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Variants_Sku]    Script Date: 10/03/2026 4:17:34 CH ******/
CREATE NONCLUSTERED INDEX [IX_Variants_Sku] ON [dbo].[ProductVariants]
(
	[SkuCode] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Shops_OwnerId]    Script Date: 10/03/2026 4:17:34 CH ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_Shops_OwnerId] ON [dbo].[Shops]
(
	[OwnerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Categories] ADD  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[Categories] ADD  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[Files] ADD  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[Files] ADD  DEFAULT (sysdatetimeoffset()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[OrderItems] ADD  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[Orders] ADD  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[Orders] ADD  DEFAULT ((0)) FOR [PlatformFee]
GO
ALTER TABLE [dbo].[Orders] ADD  DEFAULT ('PENDING_PAYMENT') FOR [Status]
GO
ALTER TABLE [dbo].[Orders] ADD  DEFAULT ('UNPAID') FOR [PaymentStatus]
GO
ALTER TABLE [dbo].[Orders] ADD  DEFAULT (sysdatetimeoffset()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Products] ADD  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[Products] ADD  DEFAULT ('DRAFT') FOR [Status]
GO
ALTER TABLE [dbo].[Products] ADD  DEFAULT (sysdatetimeoffset()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Products] ADD  DEFAULT ((0)) FOR [IsDeleted]
GO
ALTER TABLE [dbo].[ProductVariants] ADD  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[ProductVariants] ADD  DEFAULT ((0)) FOR [Quantity]
GO
ALTER TABLE [dbo].[ProductVariants] ADD  DEFAULT ((0)) FOR [ReservedQuantity]
GO
ALTER TABLE [dbo].[ProductVariants] ADD  DEFAULT (sysdatetimeoffset()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[ProductViewLogs] ADD  DEFAULT (sysdatetimeoffset()) FOR [ViewedAt]
GO
ALTER TABLE [dbo].[ReturnPolicies] ADD  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[ReturnPolicies] ADD  DEFAULT ((0)) FOR [ReturnDays]
GO
ALTER TABLE [dbo].[Reviews] ADD  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[Reviews] ADD  DEFAULT (sysdatetimeoffset()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[SellerWallets] ADD  DEFAULT ((0)) FOR [AvailableBalance]
GO
ALTER TABLE [dbo].[SellerWallets] ADD  DEFAULT ((0)) FOR [PendingBalance]
GO
ALTER TABLE [dbo].[SellerWallets] ADD  DEFAULT (sysdatetimeoffset()) FOR [UpdatedAt]
GO
ALTER TABLE [dbo].[ShippingPolicies] ADD  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[ShippingPolicies] ADD  DEFAULT ((2)) FOR [HandlingTimeDays]
GO
ALTER TABLE [dbo].[ShippingPolicies] ADD  DEFAULT ((0)) FOR [Cost]
GO
ALTER TABLE [dbo].[ShippingPolicies] ADD  DEFAULT ((0)) FOR [IsDefault]
GO
ALTER TABLE [dbo].[ShopAnalyticsDaily] ADD  DEFAULT ((0)) FOR [TotalRevenue]
GO
ALTER TABLE [dbo].[ShopAnalyticsDaily] ADD  DEFAULT ((0)) FOR [TotalOrders]
GO
ALTER TABLE [dbo].[ShopAnalyticsDaily] ADD  DEFAULT ((0)) FOR [ItemsSold]
GO
ALTER TABLE [dbo].[ShopAnalyticsDaily] ADD  DEFAULT ((0)) FOR [ViewsCount]
GO
ALTER TABLE [dbo].[Shops] ADD  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[Shops] ADD  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[Shops] ADD  DEFAULT ((0)) FOR [RatingAvg]
GO
ALTER TABLE [dbo].[Shops] ADD  DEFAULT (sysdatetimeoffset()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Shops] ADD  DEFAULT ((0)) FOR [IsVerified]
GO
ALTER TABLE [dbo].[Shops] ADD  DEFAULT ((0)) FOR [TotalShippingPolicies]
GO
ALTER TABLE [dbo].[Shops] ADD  DEFAULT ((0)) FOR [TotalReturnPolicies]
GO
ALTER TABLE [dbo].[Shops] ADD  DEFAULT ((10)) FOR [MonthlyListingLimit]
GO
ALTER TABLE [dbo].[Users] ADD  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[Users] ADD  DEFAULT ((0)) FOR [IsEmailVerified]
GO
ALTER TABLE [dbo].[Users] ADD  DEFAULT ((0)) FOR [IsIdentityVerified]
GO
ALTER TABLE [dbo].[Users] ADD  DEFAULT ('SELLER') FOR [Role]
GO
ALTER TABLE [dbo].[Users] ADD  DEFAULT (sysdatetimeoffset()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Vouchers] ADD  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[Vouchers] ADD  DEFAULT ('PERCENTAGE') FOR [DiscountType]
GO
ALTER TABLE [dbo].[Vouchers] ADD  DEFAULT ((0)) FOR [MinOrderValue]
GO
ALTER TABLE [dbo].[Vouchers] ADD  DEFAULT ((100)) FOR [UsageLimit]
GO
ALTER TABLE [dbo].[Vouchers] ADD  DEFAULT ((0)) FOR [UsedCount]
GO
ALTER TABLE [dbo].[Vouchers] ADD  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[WalletTransactions] ADD  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[WalletTransactions] ADD  DEFAULT (sysdatetimeoffset()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Categories]  WITH CHECK ADD FOREIGN KEY([ParentId])
REFERENCES [dbo].[Categories] ([Id])
GO
ALTER TABLE [dbo].[Files]  WITH CHECK ADD FOREIGN KEY([OwnerId])
REFERENCES [dbo].[Users] ([Id])
GO
ALTER TABLE [dbo].[OrderItems]  WITH CHECK ADD FOREIGN KEY([OrderId])
REFERENCES [dbo].[Orders] ([Id])
GO
ALTER TABLE [dbo].[OrderItems]  WITH CHECK ADD FOREIGN KEY([ProductId])
REFERENCES [dbo].[Products] ([Id])
GO
ALTER TABLE [dbo].[OrderItems]  WITH CHECK ADD FOREIGN KEY([VariantId])
REFERENCES [dbo].[ProductVariants] ([Id])
GO
ALTER TABLE [dbo].[Orders]  WITH CHECK ADD FOREIGN KEY([BuyerId])
REFERENCES [dbo].[Users] ([Id])
GO
ALTER TABLE [dbo].[Orders]  WITH CHECK ADD FOREIGN KEY([ShopId])
REFERENCES [dbo].[Shops] ([Id])
GO
ALTER TABLE [dbo].[Products]  WITH CHECK ADD FOREIGN KEY([CategoryId])
REFERENCES [dbo].[Categories] ([Id])
GO
ALTER TABLE [dbo].[Products]  WITH CHECK ADD FOREIGN KEY([ReturnPolicyId])
REFERENCES [dbo].[ReturnPolicies] ([Id])
GO
ALTER TABLE [dbo].[Products]  WITH CHECK ADD FOREIGN KEY([ShippingPolicyId])
REFERENCES [dbo].[ShippingPolicies] ([Id])
GO
ALTER TABLE [dbo].[Products]  WITH CHECK ADD FOREIGN KEY([ShopId])
REFERENCES [dbo].[Shops] ([Id])
GO
ALTER TABLE [dbo].[ProductVariants]  WITH CHECK ADD FOREIGN KEY([ProductId])
REFERENCES [dbo].[Products] ([Id])
GO
ALTER TABLE [dbo].[ProductViewLogs]  WITH CHECK ADD FOREIGN KEY([ProductId])
REFERENCES [dbo].[Products] ([Id])
GO
ALTER TABLE [dbo].[ProductViewLogs]  WITH CHECK ADD FOREIGN KEY([ShopId])
REFERENCES [dbo].[Shops] ([Id])
GO
ALTER TABLE [dbo].[ReturnPolicies]  WITH CHECK ADD FOREIGN KEY([ShopId])
REFERENCES [dbo].[Shops] ([Id])
GO
ALTER TABLE [dbo].[Reviews]  WITH CHECK ADD FOREIGN KEY([BuyerId])
REFERENCES [dbo].[Users] ([Id])
GO
ALTER TABLE [dbo].[Reviews]  WITH CHECK ADD FOREIGN KEY([OrderId])
REFERENCES [dbo].[Orders] ([Id])
GO
ALTER TABLE [dbo].[Reviews]  WITH CHECK ADD FOREIGN KEY([ProductId])
REFERENCES [dbo].[Products] ([Id])
GO
ALTER TABLE [dbo].[SellerWallets]  WITH CHECK ADD FOREIGN KEY([ShopId])
REFERENCES [dbo].[Shops] ([Id])
GO
ALTER TABLE [dbo].[ShippingPolicies]  WITH CHECK ADD FOREIGN KEY([ShopId])
REFERENCES [dbo].[Shops] ([Id])
GO
ALTER TABLE [dbo].[ShopAnalyticsDaily]  WITH CHECK ADD FOREIGN KEY([ShopId])
REFERENCES [dbo].[Shops] ([Id])
GO
ALTER TABLE [dbo].[Shops]  WITH CHECK ADD FOREIGN KEY([OwnerId])
REFERENCES [dbo].[Users] ([Id])
GO
ALTER TABLE [dbo].[Vouchers]  WITH CHECK ADD FOREIGN KEY([ShopId])
REFERENCES [dbo].[Shops] ([Id])
GO
ALTER TABLE [dbo].[WalletTransactions]  WITH CHECK ADD FOREIGN KEY([ShopId])
REFERENCES [dbo].[Shops] ([Id])
GO
ALTER TABLE [dbo].[Orders]  WITH CHECK ADD CHECK  ((isjson([ReceiverInfo])=(1)))
GO
ALTER TABLE [dbo].[Orders]  WITH CHECK ADD  CONSTRAINT [CK_OrderStatus] CHECK  (([Status]='RETURNED' OR [Status]='CANCELLED' OR [Status]='DELIVERED' OR [Status]='SHIPPED' OR [Status]='PROCESSING' OR [Status]='READY_TO_SHIP' OR [Status]='PENDING_PAYMENT'))
GO
ALTER TABLE [dbo].[Orders] CHECK CONSTRAINT [CK_OrderStatus]
GO
ALTER TABLE [dbo].[Orders]  WITH CHECK ADD  CONSTRAINT [CK_PaymentStatus] CHECK  (([PaymentStatus]='REFUNDED' OR [PaymentStatus]='PAID' OR [PaymentStatus]='UNPAID'))
GO
ALTER TABLE [dbo].[Orders] CHECK CONSTRAINT [CK_PaymentStatus]
GO
ALTER TABLE [dbo].[ProductVariants]  WITH CHECK ADD CHECK  ((isjson([Attributes])=(1)))
GO
ALTER TABLE [dbo].[ProductVariants]  WITH CHECK ADD  CONSTRAINT [CK_Stock_Valid] CHECK  (([Quantity]>=(0) AND [ReservedQuantity]>=(0)))
GO
ALTER TABLE [dbo].[ProductVariants] CHECK CONSTRAINT [CK_Stock_Valid]
GO
ALTER TABLE [dbo].[ReturnPolicies]  WITH CHECK ADD CHECK  (([ShippingPaidBy]='SELLER' OR [ShippingPaidBy]='BUYER'))
GO
ALTER TABLE [dbo].[Reviews]  WITH CHECK ADD CHECK  (([Rating]>=(1) AND [Rating]<=(5)))
GO
ALTER TABLE [dbo].[SellerWallets]  WITH CHECK ADD CHECK  (([AvailableBalance]>=(0)))
GO
ALTER TABLE [dbo].[SellerWallets]  WITH CHECK ADD CHECK  (([PendingBalance]>=(0)))
GO
USE [master]
GO
ALTER DATABASE [EbaySellerClone] SET  READ_WRITE 
GO


