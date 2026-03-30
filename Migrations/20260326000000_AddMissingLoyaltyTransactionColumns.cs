using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Exodus.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingLoyaltyTransactionColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'[LoyaltyTransactions]') AND name = 'ReferenceCode'
                )
                BEGIN
                    ALTER TABLE [LoyaltyTransactions] ADD [ReferenceCode] nvarchar(50) NULL;
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'[LoyaltyTransactions]') AND name = 'ExpiresAt'
                )
                BEGIN
                    ALTER TABLE [LoyaltyTransactions] ADD [ExpiresAt] datetime2 NULL;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'[LoyaltyTransactions]') AND name = 'ExpiresAt'
                )
                BEGIN
                    ALTER TABLE [LoyaltyTransactions] DROP COLUMN [ExpiresAt];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'[LoyaltyTransactions]') AND name = 'ReferenceCode'
                )
                BEGIN
                    ALTER TABLE [LoyaltyTransactions] DROP COLUMN [ReferenceCode];
                END
            ");
        }
    }
}
