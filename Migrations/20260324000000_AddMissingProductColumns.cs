using Exodus.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Exodus.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260324000000_AddMissingProductColumns")]
    public partial class AddMissingProductColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Brand",
                table: "Products",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Manufacturer",
                table: "Products",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Products_Categories_CategoryId", table: "Products");
            migrationBuilder.DropIndex(name: "IX_Products_CategoryId", table: "Products");
            migrationBuilder.DropColumn(name: "Brand", table: "Products");
            migrationBuilder.DropColumn(name: "Manufacturer", table: "Products");
            migrationBuilder.DropColumn(name: "CategoryId", table: "Products");
        }
    }
}
