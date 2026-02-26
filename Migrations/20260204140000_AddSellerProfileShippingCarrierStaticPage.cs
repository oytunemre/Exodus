using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Exodus.Migrations
{
    /// <inheritdoc />
    [Migration("20260204140000_AddSellerProfileShippingCarrierStaticPage")]
    public partial class AddSellerProfileShippingCarrierStaticPage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Seller Profiles
            migrationBuilder.CreateTable(
                name: "SellerProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    BusinessName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TaxNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BusinessAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BusinessPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    VerificationStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifiedByAdminId = table.Column<int>(type: "int", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TaxDocumentUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IdentityDocumentUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SignatureCircularUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CustomCommissionRate = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    BankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IBAN = table.Column<string>(type: "nvarchar(34)", maxLength: 34, nullable: true),
                    AccountHolderName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Rating = table.Column<decimal>(type: "decimal(3,2)", nullable: false),
                    RatingCount = table.Column<int>(type: "int", nullable: false),
                    TotalSales = table.Column<int>(type: "int", nullable: false),
                    TotalRevenue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WarningCount = table.Column<int>(type: "int", nullable: false),
                    SuspendedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SuspensionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellerProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SellerProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Shipping Carriers
            migrationBuilder.CreateTable(
                name: "ShippingCarriers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TrackingUrlTemplate = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SupportsApi = table.Column<bool>(type: "bit", nullable: false),
                    ApiEndpoint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ApiKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DefaultRate = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FreeShippingThreshold = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingCarriers", x => x.Id);
                });

            // Static Pages
            migrationBuilder.CreateTable(
                name: "StaticPages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MetaTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MetaDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MetaKeywords = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    ShowInFooter = table.Column<bool>(type: "bit", nullable: false),
                    ShowInHeader = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    PageType = table.Column<int>(type: "int", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastEditedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaticPages", x => x.Id);
                });

            // Indexes
            migrationBuilder.CreateIndex(
                name: "IX_SellerProfiles_UserId",
                table: "SellerProfiles",
                column: "UserId",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SellerProfiles_VerificationStatus",
                table: "SellerProfiles",
                column: "VerificationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingCarriers_Code",
                table: "ShippingCarriers",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_StaticPages_Slug",
                table: "StaticPages",
                column: "Slug",
                unique: true,
                filter: "[IsDeleted] = 0");

            // Seed default shipping carriers
            migrationBuilder.InsertData(
                table: "ShippingCarriers",
                columns: new[] { "Name", "Code", "TrackingUrlTemplate", "IsActive", "SupportsApi", "DefaultRate", "DisplayOrder", "CreatedAt", "UpdatedAt", "IsDeleted" },
                values: new object[] { "Yurtiçi Kargo", "YURTICI", "https://www.yurticikargo.com/tr/online-servisler/gonderi-sorgula?code={tracking}", true, false, 29.90m, 1, DateTime.UtcNow, DateTime.UtcNow, false });

            migrationBuilder.InsertData(
                table: "ShippingCarriers",
                columns: new[] { "Name", "Code", "TrackingUrlTemplate", "IsActive", "SupportsApi", "DefaultRate", "DisplayOrder", "CreatedAt", "UpdatedAt", "IsDeleted" },
                values: new object[] { "Aras Kargo", "ARAS", "https://www.araskargo.com.tr/trs/kargo-takip/?tracking={tracking}", true, false, 27.90m, 2, DateTime.UtcNow, DateTime.UtcNow, false });

            migrationBuilder.InsertData(
                table: "ShippingCarriers",
                columns: new[] { "Name", "Code", "TrackingUrlTemplate", "IsActive", "SupportsApi", "DefaultRate", "DisplayOrder", "CreatedAt", "UpdatedAt", "IsDeleted" },
                values: new object[] { "MNG Kargo", "MNG", "https://www.mngkargo.com.tr/gonderi-takip/?barcode={tracking}", true, false, 28.90m, 3, DateTime.UtcNow, DateTime.UtcNow, false });

            // Seed default static pages
            migrationBuilder.InsertData(
                table: "StaticPages",
                columns: new[] { "Title", "Slug", "Content", "MetaTitle", "MetaDescription", "IsPublished", "ShowInFooter", "ShowInHeader", "DisplayOrder", "PageType", "PublishedAt", "CreatedAt", "UpdatedAt", "IsDeleted" },
                values: new object[] { "Hakkımızda", "hakkimizda", "<h1>Hakkımızda</h1><p>Farmazon, Türkiye'nin önde gelen online pazaryeri platformudur.</p>", "Hakkımızda - Farmazon", "Farmazon hakkında bilgi edinin", true, true, false, 1, 3, DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, false });

            migrationBuilder.InsertData(
                table: "StaticPages",
                columns: new[] { "Title", "Slug", "Content", "MetaTitle", "MetaDescription", "IsPublished", "ShowInFooter", "ShowInHeader", "DisplayOrder", "PageType", "PublishedAt", "CreatedAt", "UpdatedAt", "IsDeleted" },
                values: new object[] { "Gizlilik Politikası", "gizlilik-politikasi", "<h1>Gizlilik Politikası</h1><p>Kişisel verilerinizin korunması bizim için önemlidir.</p>", "Gizlilik Politikası - Farmazon", "Farmazon gizlilik politikası", true, true, false, 2, 1, DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, false });

            migrationBuilder.InsertData(
                table: "StaticPages",
                columns: new[] { "Title", "Slug", "Content", "MetaTitle", "MetaDescription", "IsPublished", "ShowInFooter", "ShowInHeader", "DisplayOrder", "PageType", "PublishedAt", "CreatedAt", "UpdatedAt", "IsDeleted" },
                values: new object[] { "Kullanım Şartları", "kullanim-sartlari", "<h1>Kullanım Şartları</h1><p>Platformumuzu kullanmadan önce lütfen okuyunuz.</p>", "Kullanım Şartları - Farmazon", "Farmazon kullanım şartları", true, true, false, 3, 1, DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, false });

            migrationBuilder.InsertData(
                table: "StaticPages",
                columns: new[] { "Title", "Slug", "Content", "MetaTitle", "MetaDescription", "IsPublished", "ShowInFooter", "ShowInHeader", "DisplayOrder", "PageType", "PublishedAt", "CreatedAt", "UpdatedAt", "IsDeleted" },
                values: new object[] { "İletişim", "iletisim", "<h1>İletişim</h1><p>Bize ulaşmak için: destek@farmazon.com</p>", "İletişim - Farmazon", "Farmazon iletişim bilgileri", true, true, true, 4, 3, DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, false });

            migrationBuilder.InsertData(
                table: "StaticPages",
                columns: new[] { "Title", "Slug", "Content", "MetaTitle", "MetaDescription", "IsPublished", "ShowInFooter", "ShowInHeader", "DisplayOrder", "PageType", "PublishedAt", "CreatedAt", "UpdatedAt", "IsDeleted" },
                values: new object[] { "Sıkça Sorulan Sorular", "sss", "<h1>Sıkça Sorulan Sorular</h1><h2>Nasıl sipariş verebilirim?</h2><p>Ürünü sepete ekleyip ödeme adımlarını takip edebilirsiniz.</p>", "SSS - Farmazon", "Farmazon sıkça sorulan sorular", true, true, false, 5, 2, DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StaticPages");

            migrationBuilder.DropTable(
                name: "ShippingCarriers");

            migrationBuilder.DropTable(
                name: "SellerProfiles");
        }
    }
}
