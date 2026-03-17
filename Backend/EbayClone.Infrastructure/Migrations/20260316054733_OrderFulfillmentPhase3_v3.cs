using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbayClone.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OrderFulfillmentPhase3_v3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvailableStock",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "RestockingFeePercent",
                table: "ReturnPolicies");

            migrationBuilder.DropColumn(
                name: "ReservedQuantity",
                table: "ProductVariants");

            migrationBuilder.AddColumn<string>(
                name: "CancelReason",
                table: "Orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelRequestedBy",
                table: "Orders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CancelledAt",
                table: "Orders",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeliveredAt",
                table: "Orders",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReturnDeadline",
                table: "Orders",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ShipByDate",
                table: "Orders",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrderCancellations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedBy = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "REQUESTED"),
                    IsDefect = table.Column<bool>(type: "bit", nullable: false),
                    IsFeeCredited = table.Column<bool>(type: "bit", nullable: false),
                    IsStockRestored = table.Column<bool>(type: "bit", nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RespondedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResponseDeadline = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderCancellations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderCancellations_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderDisputes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BuyerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BuyerMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BuyerEvidenceUrls = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SellerMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SellerEvidenceUrls = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "OPENED"),
                    Resolution = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsDefect = table.Column<bool>(type: "bit", nullable: false),
                    OpenedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SellerRespondedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EscalatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SellerResponseDeadline = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderDisputes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderDisputes_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderDisputes_Users_BuyerId",
                        column: x => x.BuyerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderReturns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BuyerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BuyerMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhotoUrls = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "REQUESTED"),
                    SellerResponseType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SellerMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RefundAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DeductionAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DeductionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReturnTrackingCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReturnCarrier = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReturnShippingPaidBy = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "BUYER"),
                    IsStockRestored = table.Column<bool>(type: "bit", nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RespondedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReturnShippedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReturnReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RefundedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SellerResponseDeadline = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderReturns_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderReturns_Users_BuyerId",
                        column: x => x.BuyerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ShipByDate",
                table: "Orders",
                column: "ShipByDate");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status",
                table: "Orders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OrderCancellations_OrderId",
                table: "OrderCancellations",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderCancellations_Status",
                table: "OrderCancellations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDisputes_BuyerId",
                table: "OrderDisputes",
                column: "BuyerId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDisputes_OrderId",
                table: "OrderDisputes",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDisputes_Status",
                table: "OrderDisputes",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OrderReturns_BuyerId",
                table: "OrderReturns",
                column: "BuyerId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderReturns_OrderId",
                table: "OrderReturns",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderReturns_Status",
                table: "OrderReturns",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderCancellations");

            migrationBuilder.DropTable(
                name: "OrderDisputes");

            migrationBuilder.DropTable(
                name: "OrderReturns");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ShipByDate",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Status",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CancelReason",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CancelRequestedBy",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ReturnDeadline",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShipByDate",
                table: "Orders");

            migrationBuilder.AddColumn<decimal>(
                name: "RestockingFeePercent",
                table: "ReturnPolicies",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ReservedQuantity",
                table: "ProductVariants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AvailableStock",
                table: "ProductVariants",
                type: "int",
                nullable: false,
                computedColumnSql: "[Quantity] - [ReservedQuantity]",
                stored: false);
        }
    }
}
