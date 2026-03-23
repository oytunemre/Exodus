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
            // Drop FK if exists, alter column to nullable, re-add FK
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Campaigns_Users_SellerId')
                    ALTER TABLE [Campaigns] DROP CONSTRAINT [FK_Campaigns_Users_SellerId];
                ALTER TABLE [Campaigns] ALTER COLUMN [SellerId] int NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Campaigns_Users_SellerId')
                    ALTER TABLE [Campaigns] ADD CONSTRAINT [FK_Campaigns_Users_SellerId]
                        FOREIGN KEY ([SellerId]) REFERENCES [Users]([Id]);
            ");
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
