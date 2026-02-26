using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Exodus.Migrations
{
    /// <inheritdoc />
    [Migration("20260204120000_AddBannerAndSiteSettings")]
    public partial class AddBannerAndSiteSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Banners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    MobileImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TargetUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Position = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClickCount = table.Column<int>(type: "int", nullable: false),
                    ViewCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Banners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SiteSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Banners_Position_DisplayOrder",
                table: "Banners",
                columns: new[] { "Position", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_SiteSettings_Key",
                table: "SiteSettings",
                column: "Key",
                unique: true,
                filter: "[IsDeleted] = 0");

            // Seed default site settings
            migrationBuilder.InsertData(
                table: "SiteSettings",
                columns: new[] { "Key", "Value", "Description", "Category", "IsPublic", "CreatedAt", "UpdatedAt", "IsDeleted" },
                values: new object[,]
                {
                    { "Shipping.DefaultCost", "29.90", "Varsayılan kargo ücreti (TL)", "Shipping", false, DateTime.UtcNow, DateTime.UtcNow, false },
                    { "Shipping.FreeThreshold", "500", "Ücretsiz kargo limiti (TL)", "Shipping", true, DateTime.UtcNow, DateTime.UtcNow, false },
                    { "Commission.DefaultRate", "10", "Varsayılan komisyon oranı (%)", "Commission", false, DateTime.UtcNow, DateTime.UtcNow, false },
                    { "Commission.MinAmount", "1", "Minimum komisyon tutarı (TL)", "Commission", false, DateTime.UtcNow, DateTime.UtcNow, false },
                    { "General.SiteName", "Farmazon", "Site adı", "General", true, DateTime.UtcNow, DateTime.UtcNow, false },
                    { "Payment.MinOrderAmount", "50", "Minimum sipariş tutarı (TL)", "Payment", true, DateTime.UtcNow, DateTime.UtcNow, false }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SiteSettings");
            migrationBuilder.DropTable(name: "Banners");
        }
    }
}
