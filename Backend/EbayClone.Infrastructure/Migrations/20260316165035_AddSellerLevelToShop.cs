using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbayClone.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSellerLevelToShop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DefectCount",
                table: "Shops",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LevelEvaluatedAt",
                table: "Shops",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerLevel",
                table: "Shops",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalSalesAmount",
                table: "Shops",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TotalTransactions",
                table: "Shops",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefectCount",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "LevelEvaluatedAt",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "SellerLevel",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "TotalSalesAmount",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "TotalTransactions",
                table: "Shops");
        }
    }
}
