using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Exodus.Migrations
{
    /// <inheritdoc />
    [Migration("20260226000001_AddOrderNumberAndMissingOrderFields")]
    public partial class AddOrderNumberAndMissingOrderFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop old Total column (replaced by TotalAmount) - conditional in case it was already removed
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'Total'
                )
                BEGIN
                    ALTER TABLE Orders DROP COLUMN Total
                END");

            // Add OrderNumber (nvarchar(50)) - conditional
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'OrderNumber')
                BEGIN
                    ALTER TABLE Orders ADD OrderNumber nvarchar(50) NOT NULL DEFAULT 'LEGACY-0'
                END");

            // Add financial columns - conditional
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'SubTotal')
                BEGIN ALTER TABLE Orders ADD SubTotal decimal(18,2) NOT NULL DEFAULT 0 END");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'ShippingCost')
                BEGIN ALTER TABLE Orders ADD ShippingCost decimal(18,2) NOT NULL DEFAULT 0 END");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'TaxAmount')
                BEGIN ALTER TABLE Orders ADD TaxAmount decimal(18,2) NOT NULL DEFAULT 0 END");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'DiscountAmount')
                BEGIN ALTER TABLE Orders ADD DiscountAmount decimal(18,2) NOT NULL DEFAULT 0 END");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'TotalAmount')
                BEGIN ALTER TABLE Orders ADD TotalAmount decimal(18,2) NOT NULL DEFAULT 0 END");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'Currency')
                BEGIN ALTER TABLE Orders ADD Currency nvarchar(3) NOT NULL DEFAULT 'TRY' END");

            // Add address snapshot columns - conditional
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'ShippingAddressId')
                BEGIN ALTER TABLE Orders ADD ShippingAddressId int NULL END");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'ShippingAddressSnapshot')
                BEGIN ALTER TABLE Orders ADD ShippingAddressSnapshot nvarchar(500) NULL END");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'BillingAddressId')
                BEGIN ALTER TABLE Orders ADD BillingAddressId int NULL END");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'BillingAddressSnapshot')
                BEGIN ALTER TABLE Orders ADD BillingAddressSnapshot nvarchar(500) NULL END");

            // Add note columns - conditional
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'CustomerNote')
                BEGIN ALTER TABLE Orders ADD CustomerNote nvarchar(1000) NULL END");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'AdminNote')
                BEGIN ALTER TABLE Orders ADD AdminNote nvarchar(1000) NULL END");

            // Add cancellation columns - conditional
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'CancellationReason')
                BEGIN ALTER TABLE Orders ADD CancellationReason int NULL END");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'CancellationNote')
                BEGIN ALTER TABLE Orders ADD CancellationNote nvarchar(500) NULL END");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'CancelledAt')
                BEGIN ALTER TABLE Orders ADD CancelledAt datetime2 NULL END");

            // Add timestamp columns - conditional
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'PaidAt')
                BEGIN ALTER TABLE Orders ADD PaidAt datetime2 NULL END");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'ShippedAt')
                BEGIN ALTER TABLE Orders ADD ShippedAt datetime2 NULL END");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'DeliveredAt')
                BEGIN ALTER TABLE Orders ADD DeliveredAt datetime2 NULL END");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'CompletedAt')
                BEGIN ALTER TABLE Orders ADD CompletedAt datetime2 NULL END");

            // Add unique index on OrderNumber - conditional
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Orders_OrderNumber' AND object_id = OBJECT_ID('Orders'))
                BEGIN
                    CREATE UNIQUE INDEX IX_Orders_OrderNumber ON Orders(OrderNumber)
                END");

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
