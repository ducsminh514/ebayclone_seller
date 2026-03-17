using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbayClone.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncSnapshotAfterPhan1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReturnPolicies_Shops_ShopId",
                table: "ReturnPolicies");

            migrationBuilder.DropForeignKey(
                name: "FK_ShippingPolicies_Shops_ShopId",
                table: "ShippingPolicies");

            // === WalletTransactions: Safe AddColumn ===
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('WalletTransactions') AND name = 'Status')
                    ALTER TABLE [WalletTransactions] ADD [Status] nvarchar(max) NOT NULL DEFAULT '';
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('WalletTransactions') AND name = 'WalletId')
                    ALTER TABLE [WalletTransactions] ADD [WalletId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
            ");

            // === Shops: Safe AddColumn ===
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Shops') AND name = 'BankVerificationAttempts')
                    ALTER TABLE [Shops] ADD [BankVerificationAttempts] int NOT NULL DEFAULT 0;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Shops') AND name = 'IsPolicyOptedIn')
                    ALTER TABLE [Shops] ADD [IsPolicyOptedIn] bit NOT NULL DEFAULT 0;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Shops') AND name = 'TotalPaymentPolicies')
                    ALTER TABLE [Shops] ADD [TotalPaymentPolicies] int NOT NULL DEFAULT 0;
            ");

            // === ShippingPolicies: AlterColumn (ShopId nullable) ===
            migrationBuilder.AlterColumn<Guid>(
                name: "ShopId",
                table: "ShippingPolicies",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            // === ShippingPolicies: Safe AddColumn ===
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ShippingPolicies') AND name = 'HandlingTimeCutoff')
                    ALTER TABLE [ShippingPolicies] ADD [HandlingTimeCutoff] nvarchar(max) NOT NULL DEFAULT '';
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ShippingPolicies') AND name = 'OfferCombinedShippingDiscount')
                    ALTER TABLE [ShippingPolicies] ADD [OfferCombinedShippingDiscount] bit NOT NULL DEFAULT 0;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ShippingPolicies') AND name = 'OfferFreeShipping')
                    ALTER TABLE [ShippingPolicies] ADD [OfferFreeShipping] bit NOT NULL DEFAULT 0;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ShippingPolicies') AND name = 'PackageDimensionsJson')
                    ALTER TABLE [ShippingPolicies] ADD [PackageDimensionsJson] nvarchar(max) NOT NULL DEFAULT '';
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ShippingPolicies') AND name = 'PackageType')
                    ALTER TABLE [ShippingPolicies] ADD [PackageType] nvarchar(max) NOT NULL DEFAULT '';
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ShippingPolicies') AND name = 'PackageWeightOz')
                    ALTER TABLE [ShippingPolicies] ADD [PackageWeightOz] decimal(18,2) NOT NULL DEFAULT 0;
            ");

            // === SellerWallets: Safe AddColumn ===
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('SellerWallets') AND name = 'Currency')
                    ALTER TABLE [SellerWallets] ADD [Currency] nvarchar(max) NOT NULL DEFAULT '';
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('SellerWallets') AND name = 'RowVersion')
                    ALTER TABLE [SellerWallets] ADD [RowVersion] rowversion NOT NULL;
            ");

            // === ReturnPolicies: AlterColumn (ShopId nullable) ===
            migrationBuilder.AlterColumn<Guid>(
                name: "ShopId",
                table: "ReturnPolicies",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            // === ReturnPolicies: Safe AddColumn ===
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ReturnPolicies') AND name = 'AutoAcceptReturns')
                    ALTER TABLE [ReturnPolicies] ADD [AutoAcceptReturns] bit NOT NULL DEFAULT 0;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ReturnPolicies') AND name = 'RestockingFeePercent')
                    ALTER TABLE [ReturnPolicies] ADD [RestockingFeePercent] decimal(18,2) NOT NULL DEFAULT 0;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ReturnPolicies') AND name = 'ReturnAddressJson')
                    ALTER TABLE [ReturnPolicies] ADD [ReturnAddressJson] nvarchar(max) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ReturnPolicies') AND name = 'SendImmediateRefund')
                    ALTER TABLE [ReturnPolicies] ADD [SendImmediateRefund] bit NOT NULL DEFAULT 0;
            ");

            // === Products: Safe AddColumn ===
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'PaymentPolicyId')
                    ALTER TABLE [Products] ADD [PaymentPolicyId] uniqueidentifier NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'ReferenceId')
                    ALTER TABLE [Products] ADD [ReferenceId] nvarchar(max) NULL;
            ");

            // === Orders: Safe AddColumn ===
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'IdempotencyKey')
                    ALTER TABLE [Orders] ADD [IdempotencyKey] nvarchar(max) NOT NULL DEFAULT '';
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'IsEscrowReleased')
                    ALTER TABLE [Orders] ADD [IsEscrowReleased] bit NOT NULL DEFAULT 0;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'RowVersion')
                    ALTER TABLE [Orders] ADD [RowVersion] rowversion NOT NULL;
            ");

            // === PaymentPolicies: CreateTable (safe - IF NOT EXISTS) ===
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PaymentPolicies')
                BEGIN
                    CREATE TABLE [PaymentPolicies] (
                        [Id] uniqueidentifier NOT NULL,
                        [ShopId] uniqueidentifier NULL,
                        [Name] nvarchar(100) NOT NULL,
                        [Description] nvarchar(250) NOT NULL,
                        [ImmediatePaymentRequired] bit NOT NULL,
                        [DaysToPayment] int NOT NULL,
                        [PaymentMethod] nvarchar(50) NOT NULL,
                        [PaymentInstructions] nvarchar(max) NULL,
                        [IsDefault] bit NOT NULL,
                        [IsArchived] bit NOT NULL,
                        [RowVersion] rowversion NULL,
                        CONSTRAINT [PK_PaymentPolicies] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_PaymentPolicies_Shops_ShopId] FOREIGN KEY ([ShopId]) REFERENCES [Shops]([Id])
                    );
                END
            ");

            // === Indexes (safe - use IF NOT EXISTS) ===
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_PaymentPolicyId' AND object_id = OBJECT_ID('Products'))
                    CREATE INDEX [IX_Products_PaymentPolicyId] ON [Products]([PaymentPolicyId]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PaymentPolicies_ShopId' AND object_id = OBJECT_ID('PaymentPolicies'))
                    CREATE INDEX [IX_PaymentPolicies_ShopId] ON [PaymentPolicies]([ShopId]);
            ");

            // === Foreign Keys (safe - use IF NOT EXISTS) ===
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Products_PaymentPolicies_PaymentPolicyId')
                    ALTER TABLE [Products] ADD CONSTRAINT [FK_Products_PaymentPolicies_PaymentPolicyId] FOREIGN KEY ([PaymentPolicyId]) REFERENCES [PaymentPolicies]([Id]);
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ReturnPolicies_Shops_ShopId')
                    ALTER TABLE [ReturnPolicies] ADD CONSTRAINT [FK_ReturnPolicies_Shops_ShopId] FOREIGN KEY ([ShopId]) REFERENCES [Shops]([Id]);
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ShippingPolicies_Shops_ShopId')
                    ALTER TABLE [ShippingPolicies] ADD CONSTRAINT [FK_ShippingPolicies_Shops_ShopId] FOREIGN KEY ([ShopId]) REFERENCES [Shops]([Id]);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_PaymentPolicies_PaymentPolicyId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_ReturnPolicies_Shops_ShopId",
                table: "ReturnPolicies");

            migrationBuilder.DropForeignKey(
                name: "FK_ShippingPolicies_Shops_ShopId",
                table: "ShippingPolicies");

            migrationBuilder.DropTable(
                name: "PaymentPolicies");

            migrationBuilder.DropIndex(
                name: "IX_Products_PaymentPolicyId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "WalletId",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "BankVerificationAttempts",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "IsPolicyOptedIn",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "TotalPaymentPolicies",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "HandlingTimeCutoff",
                table: "ShippingPolicies");

            migrationBuilder.DropColumn(
                name: "OfferCombinedShippingDiscount",
                table: "ShippingPolicies");

            migrationBuilder.DropColumn(
                name: "OfferFreeShipping",
                table: "ShippingPolicies");

            migrationBuilder.DropColumn(
                name: "PackageDimensionsJson",
                table: "ShippingPolicies");

            migrationBuilder.DropColumn(
                name: "PackageType",
                table: "ShippingPolicies");

            migrationBuilder.DropColumn(
                name: "PackageWeightOz",
                table: "ShippingPolicies");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "SellerWallets");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SellerWallets");

            migrationBuilder.DropColumn(
                name: "AutoAcceptReturns",
                table: "ReturnPolicies");

            migrationBuilder.DropColumn(
                name: "RestockingFeePercent",
                table: "ReturnPolicies");

            migrationBuilder.DropColumn(
                name: "ReturnAddressJson",
                table: "ReturnPolicies");

            migrationBuilder.DropColumn(
                name: "SendImmediateRefund",
                table: "ReturnPolicies");

            migrationBuilder.DropColumn(
                name: "PaymentPolicyId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ReferenceId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IsEscrowReleased",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Orders");

            migrationBuilder.AlterColumn<Guid>(
                name: "ShopId",
                table: "ShippingPolicies",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ShopId",
                table: "ReturnPolicies",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ReturnPolicies_Shops_ShopId",
                table: "ReturnPolicies",
                column: "ShopId",
                principalTable: "Shops",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShippingPolicies_Shops_ShopId",
                table: "ShippingPolicies",
                column: "ShopId",
                principalTable: "Shops",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
