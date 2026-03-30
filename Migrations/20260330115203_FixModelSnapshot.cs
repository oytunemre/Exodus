using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Exodus.Migrations
{
    /// <inheritdoc />
    public partial class FixModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // All schema changes were applied in previous migrations.
            // This migration exists solely to update the EF model snapshot.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Listings_Products_ProductId",
                table: "Listings");

            migrationBuilder.DropForeignKey(
                name: "FK_Listings_Users_SellerId",
                table: "Listings");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentEvents_PaymentIntents_PaymentIntentId1",
                table: "PaymentEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "Addresses");

            migrationBuilder.DropTable(
                name: "AffiliatePayouts");

            migrationBuilder.DropTable(
                name: "AffiliateReferrals");

            migrationBuilder.DropTable(
                name: "Banners");

            migrationBuilder.DropTable(
                name: "Brands");

            migrationBuilder.DropTable(
                name: "CampaignCategories");

            migrationBuilder.DropTable(
                name: "CampaignProducts");

            migrationBuilder.DropTable(
                name: "CampaignUsages");

            migrationBuilder.DropTable(
                name: "EmailTemplates");

            migrationBuilder.DropTable(
                name: "GiftCardUsages");

            migrationBuilder.DropTable(
                name: "HomeWidgets");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "LoyaltyTransactions");

            migrationBuilder.DropTable(
                name: "NotificationPreferences");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "OrderEvents");

            migrationBuilder.DropTable(
                name: "ProductAnswers");

            migrationBuilder.DropTable(
                name: "ProductAttributeMappings");

            migrationBuilder.DropTable(
                name: "ProductComparisonItems");

            migrationBuilder.DropTable(
                name: "ProductImages");

            migrationBuilder.DropTable(
                name: "RecentlyViewed");

            migrationBuilder.DropTable(
                name: "Regions");

            migrationBuilder.DropTable(
                name: "ReturnShipments");

            migrationBuilder.DropTable(
                name: "ReviewReports");

            migrationBuilder.DropTable(
                name: "ReviewVotes");

            migrationBuilder.DropTable(
                name: "SellerPayoutItems");

            migrationBuilder.DropTable(
                name: "SellerProfiles");

            migrationBuilder.DropTable(
                name: "SellerReviews");

            migrationBuilder.DropTable(
                name: "SellerShippingSettings");

            migrationBuilder.DropTable(
                name: "ShippingZones");

            migrationBuilder.DropTable(
                name: "SiteSettings");

            migrationBuilder.DropTable(
                name: "StaticPages");

            migrationBuilder.DropTable(
                name: "SupportTicketMessages");

            migrationBuilder.DropTable(
                name: "TaxRates");

            migrationBuilder.DropTable(
                name: "WishlistItems");

            migrationBuilder.DropTable(
                name: "Affiliates");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Campaigns");

            migrationBuilder.DropTable(
                name: "GiftCards");

            migrationBuilder.DropTable(
                name: "LoyaltyPoints");

            migrationBuilder.DropTable(
                name: "ProductQuestions");

            migrationBuilder.DropTable(
                name: "ProductAttributeValues");

            migrationBuilder.DropTable(
                name: "ProductComparisons");

            migrationBuilder.DropTable(
                name: "Refunds");

            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DropTable(
                name: "SellerPayouts");

            migrationBuilder.DropTable(
                name: "ShippingCarriers");

            migrationBuilder.DropTable(
                name: "SupportTickets");

            migrationBuilder.DropTable(
                name: "Wishlists");

            migrationBuilder.DropTable(
                name: "ProductAttributes");

            migrationBuilder.DropIndex(
                name: "IX_Products_CategoryId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_PaymentEvents_PaymentIntentId1",
                table: "PaymentEvents");

            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastLoginAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Brand",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Manufacturer",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "AuthorizedAt",
                table: "PaymentIntents");

            migrationBuilder.DropColumn(
                name: "CapturedAt",
                table: "PaymentIntents");

            migrationBuilder.DropColumn(
                name: "CardBrand",
                table: "PaymentIntents");

            migrationBuilder.DropColumn(
                name: "CardLast4",
                table: "PaymentIntents");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "PaymentIntents");

            migrationBuilder.DropColumn(
                name: "FailedAt",
                table: "PaymentIntents");

            migrationBuilder.DropColumn(
                name: "InstallmentAmount",
                table: "PaymentIntents");

            migrationBuilder.DropColumn(
                name: "InstallmentCount",
                table: "PaymentIntents");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "PaymentIntents");

            migrationBuilder.DropColumn(
                name: "RefundedAmount",
                table: "PaymentIntents");

            migrationBuilder.DropColumn(
                name: "RefundedAt",
                table: "PaymentIntents");

            migrationBuilder.DropColumn(
                name: "Requires3DSecure",
                table: "PaymentIntents");

            migrationBuilder.DropColumn(
                name: "ThreeDSecureUrl",
                table: "PaymentIntents");

            migrationBuilder.DropColumn(
                name: "EventType",
                table: "PaymentEvents");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "PaymentEvents");

            migrationBuilder.DropColumn(
                name: "PaymentIntentId1",
                table: "PaymentEvents");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "PaymentEvents");

            migrationBuilder.DropColumn(
                name: "AdminNote",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "BillingAddressId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "BillingAddressSnapshot",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CancellationNote",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CustomerNote",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OrderNumber",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingAddressId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingAddressSnapshot",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingCost",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SubTotal",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "LowStockThreshold",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "SKU",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "StockStatus",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "TrackInventory",
                table: "Listings");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Orders') AND name = 'TotalAmount')
                    EXEC sp_rename 'Orders.TotalAmount', 'Total', 'COLUMN';
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Listings') AND name = 'StockQuantity')
                    EXEC sp_rename 'Listings.StockQuantity', 'Stock', 'COLUMN';
            ");

            migrationBuilder.AlterColumn<string>(
                name: "ProductName",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "ProductDescription",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<string>(
                name: "PayloadJson",
                table: "PaymentEvents",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Listings_Products_ProductId",
                table: "Listings",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Listings_Users_SellerId",
                table: "Listings",
                column: "SellerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
