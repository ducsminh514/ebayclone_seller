using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbayClone.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorShippingPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cost",
                table: "ShippingPolicies");

            migrationBuilder.AddColumn<string>(
                name: "DomesticCostType",
                table: "ShippingPolicies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DomesticServicesJson",
                table: "ShippingPolicies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExcludedLocationsJson",
                table: "ShippingPolicies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "InternationalCostType",
                table: "ShippingPolicies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "InternationalServicesJson",
                table: "ShippingPolicies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsInternationalShippingAllowed",
                table: "ShippingPolicies",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DomesticCostType",
                table: "ShippingPolicies");

            migrationBuilder.DropColumn(
                name: "DomesticServicesJson",
                table: "ShippingPolicies");

            migrationBuilder.DropColumn(
                name: "ExcludedLocationsJson",
                table: "ShippingPolicies");

            migrationBuilder.DropColumn(
                name: "InternationalCostType",
                table: "ShippingPolicies");

            migrationBuilder.DropColumn(
                name: "InternationalServicesJson",
                table: "ShippingPolicies");

            migrationBuilder.DropColumn(
                name: "IsInternationalShippingAllowed",
                table: "ShippingPolicies");

            migrationBuilder.AddColumn<decimal>(
                name: "Cost",
                table: "ShippingPolicies",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
