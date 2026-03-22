using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbayClone.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShippingMethodField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "OfferLocalPickup",
                table: "ShippingPolicies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ShippingMethod",
                table: "ShippingPolicies",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Standard");

            // Data migration: chuyển Freight/NoShipping từ DomesticCostType → ShippingMethod
            migrationBuilder.Sql(@"
                UPDATE ShippingPolicies 
                SET ShippingMethod = DomesticCostType 
                WHERE DomesticCostType IN ('Freight', 'NoShipping');

                UPDATE ShippingPolicies 
                SET DomesticCostType = 'Flat' 
                WHERE ShippingMethod IN ('Freight', 'NoShipping');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OfferLocalPickup",
                table: "ShippingPolicies");

            migrationBuilder.DropColumn(
                name: "ShippingMethod",
                table: "ShippingPolicies");
        }
    }
}
