using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Exodus.Migrations
{
    /// <inheritdoc />
    public partial class MakeCampaignSellerIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Campaigns_Users_SellerId",
                table: "Campaigns");

            migrationBuilder.AlterColumn<int>(
                name: "SellerId",
                table: "Campaigns",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Campaigns_Users_SellerId",
                table: "Campaigns",
                column: "SellerId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Campaigns_Users_SellerId",
                table: "Campaigns");

            migrationBuilder.AlterColumn<int>(
                name: "SellerId",
                table: "Campaigns",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldNullable: true,
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Campaigns_Users_SellerId",
                table: "Campaigns",
                column: "SellerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
