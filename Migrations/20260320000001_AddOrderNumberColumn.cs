using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Exodus.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderNumberColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add OrderNumber if not exists, or widen it if it's too narrow
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'OrderNumber'
                )
                BEGIN
                    ALTER TABLE Orders ADD OrderNumber NVARCHAR(30) NOT NULL DEFAULT ''
                END
                ELSE IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'OrderNumber'
                      AND (CHARACTER_MAXIMUM_LENGTH IS NULL OR CHARACTER_MAXIMUM_LENGTH < 30)
                )
                BEGIN
                    ALTER TABLE Orders ALTER COLUMN OrderNumber NVARCHAR(30) NOT NULL
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'OrderNumber'
                )
                BEGIN
                    ALTER TABLE Orders ALTER COLUMN OrderNumber NVARCHAR(20) NOT NULL
                END
            ");
        }
    }
}
