using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Exodus.Migrations
{
    /// <inheritdoc />
    public partial class FixOrderNumberLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // OrderNumber kolonu yoksa ekle, varsa uzunluğunu genişlet
            migrationBuilder.Sql(@"
                IF COL_LENGTH('Orders', 'OrderNumber') IS NULL
                BEGIN
                    ALTER TABLE [Orders] ADD [OrderNumber] nvarchar(25) NOT NULL DEFAULT ''
                END
                ELSE
                BEGIN
                    ALTER TABLE [Orders] ALTER COLUMN [OrderNumber] nvarchar(25) NOT NULL
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF COL_LENGTH('Orders', 'OrderNumber') IS NOT NULL
                BEGIN
                    ALTER TABLE [Orders] ALTER COLUMN [OrderNumber] nvarchar(20) NOT NULL
                END
            ");
        }
    }
}
