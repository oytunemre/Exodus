using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Exodus.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderNumberAndMissingOrderFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop old Total column (replaced by TotalAmount)
            migrationBuilder.DropColumn(
                name: "Total",
                table: "Orders");

            // Add OrderNumber (nvarchar(50))
            migrationBuilder.AddColumn<string>(
                name: "OrderNumber",
                table: "Orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "LEGACY-0");

            // Add financial columns
            migrationBuilder.AddColumn<decimal>(
                name: "SubTotal",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ShippingCost",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmount",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Orders",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "TRY");

            // Add address snapshot columns
            migrationBuilder.AddColumn<int>(
                name: "ShippingAddressId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingAddressSnapshot",
                table: "Orders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BillingAddressId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingAddressSnapshot",
                table: "Orders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            // Add note columns
            migrationBuilder.AddColumn<string>(
                name: "CustomerNote",
                table: "Orders",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdminNote",
                table: "Orders",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            // Add cancellation columns
            migrationBuilder.AddColumn<int>(
                name: "CancellationReason",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationNote",
                table: "Orders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            // Add timestamp columns
            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ShippedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            // Add unique index on OrderNumber
            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderNumber",
                table: "Orders",
                column: "OrderNumber",
                unique: true);

            // Create OrderEvents table
            migrationBuilder.CreateTable(
                name: "OrderEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    UserType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderEvents_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderEvents_OrderId",
                table: "OrderEvents",
                column: "OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderEvents");

            migrationBuilder.DropIndex(
                name: "IX_Orders_OrderNumber",
                table: "Orders");

            migrationBuilder.DropColumn(name: "OrderNumber", table: "Orders");
            migrationBuilder.DropColumn(name: "SubTotal", table: "Orders");
            migrationBuilder.DropColumn(name: "ShippingCost", table: "Orders");
            migrationBuilder.DropColumn(name: "TaxAmount", table: "Orders");
            migrationBuilder.DropColumn(name: "DiscountAmount", table: "Orders");
            migrationBuilder.DropColumn(name: "TotalAmount", table: "Orders");
            migrationBuilder.DropColumn(name: "Currency", table: "Orders");
            migrationBuilder.DropColumn(name: "ShippingAddressId", table: "Orders");
            migrationBuilder.DropColumn(name: "ShippingAddressSnapshot", table: "Orders");
            migrationBuilder.DropColumn(name: "BillingAddressId", table: "Orders");
            migrationBuilder.DropColumn(name: "BillingAddressSnapshot", table: "Orders");
            migrationBuilder.DropColumn(name: "CustomerNote", table: "Orders");
            migrationBuilder.DropColumn(name: "AdminNote", table: "Orders");
            migrationBuilder.DropColumn(name: "CancellationReason", table: "Orders");
            migrationBuilder.DropColumn(name: "CancellationNote", table: "Orders");
            migrationBuilder.DropColumn(name: "CancelledAt", table: "Orders");
            migrationBuilder.DropColumn(name: "PaidAt", table: "Orders");
            migrationBuilder.DropColumn(name: "ShippedAt", table: "Orders");
            migrationBuilder.DropColumn(name: "DeliveredAt", table: "Orders");
            migrationBuilder.DropColumn(name: "CompletedAt", table: "Orders");

            migrationBuilder.AddColumn<decimal>(
                name: "Total",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
