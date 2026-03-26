using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Exodus.Migrations
{
    /// <inheritdoc />
    public partial class FixMissingColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // =====================================================================
            // ORDERS: rename Total -> TotalAmount, add 19 missing columns
            // =====================================================================
            migrationBuilder.RenameColumn(
                name: "Total",
                table: "Orders",
                newName: "TotalAmount");

            migrationBuilder.AddColumn<string>(
                name: "AdminNote",
                table: "Orders",
                type: "nvarchar(1000)",
                maxLength: 1000,
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

            migrationBuilder.AddColumn<string>(
                name: "CancellationNote",
                table: "Orders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CancellationReason",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Orders",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "TRY");

            migrationBuilder.AddColumn<string>(
                name: "CustomerNote",
                table: "Orders",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "OrderNumber",
                table: "Orders",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

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

            migrationBuilder.AddColumn<decimal>(
                name: "ShippingCost",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SubTotal",
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

            // =====================================================================
            // PAYMENTINTENTS: add 13 missing columns
            // =====================================================================
            migrationBuilder.AddColumn<DateTime>(
                name: "AuthorizedAt",
                table: "PaymentIntents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardBrand",
                table: "PaymentIntents",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardLast4",
                table: "PaymentIntents",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CapturedAt",
                table: "PaymentIntents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "PaymentIntents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FailedAt",
                table: "PaymentIntents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InstallmentAmount",
                table: "PaymentIntents",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InstallmentCount",
                table: "PaymentIntents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "PaymentIntents",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RefundedAmount",
                table: "PaymentIntents",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundedAt",
                table: "PaymentIntents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Requires3DSecure",
                table: "PaymentIntents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ThreeDSecureUrl",
                table: "PaymentIntents",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            // =====================================================================
            // PAYMENTEVENTS: add 3 missing columns, fix PayloadJson length
            // =====================================================================
            migrationBuilder.AddColumn<string>(
                name: "EventType",
                table: "PaymentEvents",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "unknown");

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "PaymentEvents",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "PaymentEvents",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PayloadJson",
                table: "PaymentEvents",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "AdminNote", table: "Orders");
            migrationBuilder.DropColumn(name: "BillingAddressId", table: "Orders");
            migrationBuilder.DropColumn(name: "BillingAddressSnapshot", table: "Orders");
            migrationBuilder.DropColumn(name: "CancellationNote", table: "Orders");
            migrationBuilder.DropColumn(name: "CancellationReason", table: "Orders");
            migrationBuilder.DropColumn(name: "CancelledAt", table: "Orders");
            migrationBuilder.DropColumn(name: "CompletedAt", table: "Orders");
            migrationBuilder.DropColumn(name: "Currency", table: "Orders");
            migrationBuilder.DropColumn(name: "CustomerNote", table: "Orders");
            migrationBuilder.DropColumn(name: "DeliveredAt", table: "Orders");
            migrationBuilder.DropColumn(name: "DiscountAmount", table: "Orders");
            migrationBuilder.DropColumn(name: "OrderNumber", table: "Orders");
            migrationBuilder.DropColumn(name: "PaidAt", table: "Orders");
            migrationBuilder.DropColumn(name: "ShippedAt", table: "Orders");
            migrationBuilder.DropColumn(name: "ShippingAddressId", table: "Orders");
            migrationBuilder.DropColumn(name: "ShippingAddressSnapshot", table: "Orders");
            migrationBuilder.DropColumn(name: "ShippingCost", table: "Orders");
            migrationBuilder.DropColumn(name: "SubTotal", table: "Orders");
            migrationBuilder.DropColumn(name: "TaxAmount", table: "Orders");
            migrationBuilder.RenameColumn(name: "TotalAmount", table: "Orders", newName: "Total");

            migrationBuilder.DropColumn(name: "AuthorizedAt", table: "PaymentIntents");
            migrationBuilder.DropColumn(name: "CardBrand", table: "PaymentIntents");
            migrationBuilder.DropColumn(name: "CardLast4", table: "PaymentIntents");
            migrationBuilder.DropColumn(name: "CapturedAt", table: "PaymentIntents");
            migrationBuilder.DropColumn(name: "ExpiresAt", table: "PaymentIntents");
            migrationBuilder.DropColumn(name: "FailedAt", table: "PaymentIntents");
            migrationBuilder.DropColumn(name: "InstallmentAmount", table: "PaymentIntents");
            migrationBuilder.DropColumn(name: "InstallmentCount", table: "PaymentIntents");
            migrationBuilder.DropColumn(name: "Metadata", table: "PaymentIntents");
            migrationBuilder.DropColumn(name: "RefundedAmount", table: "PaymentIntents");
            migrationBuilder.DropColumn(name: "RefundedAt", table: "PaymentIntents");
            migrationBuilder.DropColumn(name: "Requires3DSecure", table: "PaymentIntents");
            migrationBuilder.DropColumn(name: "ThreeDSecureUrl", table: "PaymentIntents");

            migrationBuilder.DropColumn(name: "EventType", table: "PaymentEvents");
            migrationBuilder.DropColumn(name: "IpAddress", table: "PaymentEvents");
            migrationBuilder.DropColumn(name: "Source", table: "PaymentEvents");
            migrationBuilder.AlterColumn<string>(
                name: "PayloadJson",
                table: "PaymentEvents",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000,
                oldNullable: true);
        }
    }
}
