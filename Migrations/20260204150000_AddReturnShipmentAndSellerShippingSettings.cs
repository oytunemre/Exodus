using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Exodus.Migrations
{
    /// <inheritdoc />
    [Migration("20260204150000_AddReturnShipmentAndSellerShippingSettings")]
    public partial class AddReturnShipmentAndSellerShippingSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Return Shipments
            migrationBuilder.CreateTable(
                name: "ReturnShipments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReturnCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TicketId = table.Column<int>(type: "int", nullable: true),
                    RefundId = table.Column<int>(type: "int", nullable: true),
                    SellerOrderId = table.Column<int>(type: "int", nullable: false),
                    CarrierId = table.Column<int>(type: "int", nullable: true),
                    CarrierName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TrackingNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReasonDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ShippingCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaidBy = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CodeGeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ShippedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsPickupRequested = table.Column<bool>(type: "bit", nullable: false),
                    PickupAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AdminNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnShipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReturnShipments_SupportTickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "SupportTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ReturnShipments_Refunds_RefundId",
                        column: x => x.RefundId,
                        principalTable: "Refunds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ReturnShipments_SellerOrders_SellerOrderId",
                        column: x => x.SellerOrderId,
                        principalTable: "SellerOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReturnShipments_ShippingCarriers_CarrierId",
                        column: x => x.CarrierId,
                        principalTable: "ShippingCarriers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            // Seller Shipping Settings
            migrationBuilder.CreateTable(
                name: "SellerShippingSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SellerId = table.Column<int>(type: "int", nullable: false),
                    FreeShippingThreshold = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DefaultShippingCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PreferredCarrierId = table.Column<int>(type: "int", nullable: true),
                    UsesMultipleCarriers = table.Column<bool>(type: "bit", nullable: false),
                    OffersStorePickup = table.Column<bool>(type: "bit", nullable: false),
                    PickupAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OffersSameDayShipping = table.Column<bool>(type: "bit", nullable: false),
                    SameDayShippingCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    EstimatedShippingDays = table.Column<int>(type: "int", nullable: false),
                    AcceptsReturns = table.Column<bool>(type: "bit", nullable: false),
                    ReturnDaysLimit = table.Column<int>(type: "int", nullable: false),
                    OffersFreeReturns = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellerShippingSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SellerShippingSettings_Users_SellerId",
                        column: x => x.SellerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SellerShippingSettings_ShippingCarriers_PreferredCarrierId",
                        column: x => x.PreferredCarrierId,
                        principalTable: "ShippingCarriers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            // Indexes for ReturnShipments
            migrationBuilder.CreateIndex(
                name: "IX_ReturnShipments_ReturnCode",
                table: "ReturnShipments",
                column: "ReturnCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnShipments_Status_CreatedAt",
                table: "ReturnShipments",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReturnShipments_TicketId",
                table: "ReturnShipments",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnShipments_RefundId",
                table: "ReturnShipments",
                column: "RefundId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnShipments_SellerOrderId",
                table: "ReturnShipments",
                column: "SellerOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnShipments_CarrierId",
                table: "ReturnShipments",
                column: "CarrierId");

            // Indexes for SellerShippingSettings
            migrationBuilder.CreateIndex(
                name: "IX_SellerShippingSettings_SellerId",
                table: "SellerShippingSettings",
                column: "SellerId",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SellerShippingSettings_PreferredCarrierId",
                table: "SellerShippingSettings",
                column: "PreferredCarrierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SellerShippingSettings");

            migrationBuilder.DropTable(
                name: "ReturnShipments");
        }
    }
}
