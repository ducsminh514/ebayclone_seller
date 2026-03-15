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

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "WalletTransactions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "WalletId",
                table: "WalletTransactions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "BankVerificationAttempts",
                table: "Shops",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsPolicyOptedIn",
                table: "Shops",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TotalPaymentPolicies",
                table: "Shops",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<Guid>(
                name: "ShopId",
                table: "ShippingPolicies",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "HandlingTimeCutoff",
                table: "ShippingPolicies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "OfferCombinedShippingDiscount",
                table: "ShippingPolicies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OfferFreeShipping",
                table: "ShippingPolicies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PackageDimensionsJson",
                table: "ShippingPolicies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PackageType",
                table: "ShippingPolicies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PackageWeightOz",
                table: "ShippingPolicies",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "SellerWallets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SellerWallets",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AlterColumn<Guid>(
                name: "ShopId",
                table: "ReturnPolicies",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<bool>(
                name: "AutoAcceptReturns",
                table: "ReturnPolicies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "RestockingFeePercent",
                table: "ReturnPolicies",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ReturnAddressJson",
                table: "ReturnPolicies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SendImmediateRefund",
                table: "ReturnPolicies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentPolicyId",
                table: "Products",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceId",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsEscrowReleased",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Orders",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "PaymentPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShopId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ImmediatePaymentRequired = table.Column<bool>(type: "bit", nullable: false),
                    DaysToPayment = table.Column<int>(type: "int", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PaymentInstructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentPolicies_Shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "Shops",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_PaymentPolicyId",
                table: "Products",
                column: "PaymentPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentPolicies_ShopId",
                table: "PaymentPolicies",
                column: "ShopId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_PaymentPolicies_PaymentPolicyId",
                table: "Products",
                column: "PaymentPolicyId",
                principalTable: "PaymentPolicies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ReturnPolicies_Shops_ShopId",
                table: "ReturnPolicies",
                column: "ShopId",
                principalTable: "Shops",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ShippingPolicies_Shops_ShopId",
                table: "ShippingPolicies",
                column: "ShopId",
                principalTable: "Shops",
                principalColumn: "Id");
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
