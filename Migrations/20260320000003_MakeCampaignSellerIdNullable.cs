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
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'Campaigns' AND COLUMN_NAME = 'SellerId'
                      AND IS_NULLABLE = 'NO'
                )
                BEGIN
                    -- Drop existing FK constraint if any
                    DECLARE @FkName NVARCHAR(255);
                    SELECT @FkName = fk.name
                    FROM sys.foreign_keys fk
                    INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
                    INNER JOIN sys.columns c ON fkc.parent_object_id = c.object_id AND fkc.parent_column_id = c.column_id
                    WHERE OBJECT_NAME(fk.parent_object_id) = 'Campaigns' AND c.name = 'SellerId';

                    IF @FkName IS NOT NULL
                        EXEC('ALTER TABLE Campaigns DROP CONSTRAINT ' + @FkName);

                    -- Alter column to nullable
                    ALTER TABLE Campaigns ALTER COLUMN SellerId int NULL;

                    -- Re-add FK constraint
                    ALTER TABLE Campaigns ADD CONSTRAINT FK_Campaigns_Users_SellerId
                        FOREIGN KEY (SellerId) REFERENCES Users(Id);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- Update any NULL SellerId to 0 before making NOT NULL (best effort)
                UPDATE Campaigns SET SellerId = 0 WHERE SellerId IS NULL;

                DECLARE @FkName NVARCHAR(255);
                SELECT @FkName = fk.name
                FROM sys.foreign_keys fk
                INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
                INNER JOIN sys.columns c ON fkc.parent_object_id = c.object_id AND fkc.parent_column_id = c.column_id
                WHERE OBJECT_NAME(fk.parent_object_id) = 'Campaigns' AND c.name = 'SellerId';

                IF @FkName IS NOT NULL
                    EXEC('ALTER TABLE Campaigns DROP CONSTRAINT ' + @FkName);

                ALTER TABLE Campaigns ALTER COLUMN SellerId int NOT NULL;

                ALTER TABLE Campaigns ADD CONSTRAINT FK_Campaigns_Users_SellerId
                    FOREIGN KEY (SellerId) REFERENCES Users(Id);
            ");
        }
    }
}
