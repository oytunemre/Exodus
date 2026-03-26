using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Exodus.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingListingColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LowStockThreshold",
                table: "Listings",
                type: "int",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.AddColumn<string>(
                name: "SKU",
                table: "Listings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StockQuantity",
                table: "Listings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StockStatus",
                table: "Listings",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "InStock");

            migrationBuilder.AddColumn<bool>(
                name: "TrackInventory",
                table: "Listings",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "LowStockThreshold", table: "Listings");
            migrationBuilder.DropColumn(name: "SKU", table: "Listings");
            migrationBuilder.DropColumn(name: "StockQuantity", table: "Listings");
            migrationBuilder.DropColumn(name: "StockStatus", table: "Listings");
            migrationBuilder.DropColumn(name: "TrackInventory", table: "Listings");
        }
    }
}
