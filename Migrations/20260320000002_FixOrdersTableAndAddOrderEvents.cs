using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Exodus.Migrations
{
    /// <inheritdoc />
    public partial class FixOrdersTableAndAddOrderEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add missing columns to Orders table
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'SubTotal')
                    ALTER TABLE Orders ADD SubTotal decimal(18,2) NOT NULL DEFAULT 0;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'ShippingCost')
                    ALTER TABLE Orders ADD ShippingCost decimal(18,2) NOT NULL DEFAULT 0;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'TaxAmount')
                    ALTER TABLE Orders ADD TaxAmount decimal(18,2) NOT NULL DEFAULT 0;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'DiscountAmount')
                    ALTER TABLE Orders ADD DiscountAmount decimal(18,2) NOT NULL DEFAULT 0;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'TotalAmount')
                    ALTER TABLE Orders ADD TotalAmount decimal(18,2) NOT NULL DEFAULT 0;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'Currency')
                    ALTER TABLE Orders ADD Currency nvarchar(3) NOT NULL DEFAULT 'TRY';
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'ShippingAddressId')
                    ALTER TABLE Orders ADD ShippingAddressId int NULL;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'ShippingAddressSnapshot')
                    ALTER TABLE Orders ADD ShippingAddressSnapshot nvarchar(500) NULL;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'BillingAddressId')
                    ALTER TABLE Orders ADD BillingAddressId int NULL;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'BillingAddressSnapshot')
                    ALTER TABLE Orders ADD BillingAddressSnapshot nvarchar(500) NULL;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'CustomerNote')
                    ALTER TABLE Orders ADD CustomerNote nvarchar(1000) NULL;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'AdminNote')
                    ALTER TABLE Orders ADD AdminNote nvarchar(1000) NULL;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'CancellationReason')
                    ALTER TABLE Orders ADD CancellationReason int NULL;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'CancellationNote')
                    ALTER TABLE Orders ADD CancellationNote nvarchar(500) NULL;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'CancelledAt')
                    ALTER TABLE Orders ADD CancelledAt datetime2 NULL;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'PaidAt')
                    ALTER TABLE Orders ADD PaidAt datetime2 NULL;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'ShippedAt')
                    ALTER TABLE Orders ADD ShippedAt datetime2 NULL;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'DeliveredAt')
                    ALTER TABLE Orders ADD DeliveredAt datetime2 NULL;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'CompletedAt')
                    ALTER TABLE Orders ADD CompletedAt datetime2 NULL;
            ");

            // Create OrderEvents table if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'OrderEvents')
                BEGIN
                    CREATE TABLE OrderEvents (
                        Id int IDENTITY(1,1) NOT NULL,
                        OrderId int NOT NULL,
                        Status nvarchar(30) NOT NULL,
                        Title nvarchar(200) NOT NULL,
                        Description nvarchar(1000) NULL,
                        UserId int NULL,
                        UserType nvarchar(50) NULL,
                        Metadata nvarchar(2000) NULL,
                        IsDeleted bit NOT NULL DEFAULT 0,
                        DeletedDate datetime2 NULL,
                        CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
                        UpdatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
                        CONSTRAINT PK_OrderEvents PRIMARY KEY (Id),
                        CONSTRAINT FK_OrderEvents_Orders_OrderId FOREIGN KEY (OrderId)
                            REFERENCES Orders (Id) ON DELETE NO ACTION
                    );
                    CREATE INDEX IX_OrderEvents_OrderId ON OrderEvents (OrderId);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'OrderEvents')
                    DROP TABLE OrderEvents;
            ");
        }
    }
}
