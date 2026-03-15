using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbayClone.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddManagedPaymentsToShop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankAccountHolderName",
                table: "Shops",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankAccountNumber",
                table: "Shops",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "Shops",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankVerificationStatus",
                table: "Shops",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdentityImageUrl",
                table: "Shops",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsIdentityVerified",
                table: "Shops",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MicroDepositAmount1",
                table: "Shops",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MicroDepositAmount2",
                table: "Shops",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankAccountHolderName",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "BankAccountNumber",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "BankVerificationStatus",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "IdentityImageUrl",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "IsIdentityVerified",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "MicroDepositAmount1",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "MicroDepositAmount2",
                table: "Shops");
        }
    }
}
