using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Exodus.Migrations
{
    /// <inheritdoc />
    public partial class RefactorListingStockManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Stock -> StockQuantity
            migrationBuilder.RenameColumn(
                name: "Stock",
                table: "Listings",
                newName: "StockQuantity");

            // LowStockThreshold
            migrationBuilder.AddColumn<int>(
                name: "LowStockThreshold",
                table: "Listings",
                type: "int",
                nullable: false,
                defaultValue: 5);

            // TrackInventory
            migrationBuilder.AddColumn<bool>(
                name: "TrackInventory",
                table: "Listings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            // StockStatus
            migrationBuilder.AddColumn<string>(
                name: "StockStatus",
                table: "Listings",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "InStock");

            // SKU
            migrationBuilder.AddColumn<string>(
                name: "SKU",
                table: "Listings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "LowStockThreshold", table: "Listings");
            migrationBuilder.DropColumn(name: "TrackInventory", table: "Listings");
            migrationBuilder.DropColumn(name: "StockStatus", table: "Listings");
            migrationBuilder.DropColumn(name: "SKU", table: "Listings");

            migrationBuilder.RenameColumn(
                name: "StockQuantity",
                table: "Listings",
                newName: "Stock");
        }
    }
}
