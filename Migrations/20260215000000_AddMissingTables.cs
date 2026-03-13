using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Exodus.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ─────────────────────────────────────────────────────────────────
            // BATCH 1: Tables with no external FK dependencies
            // ─────────────────────────────────────────────────────────────────

            // Categories (self-referencing)
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='Categories')
BEGIN
    CREATE TABLE [Categories] (
        [Id]               int            NOT NULL IDENTITY(1,1),
        [Name]             nvarchar(100)  NOT NULL,
        [Slug]             nvarchar(100)  NOT NULL,
        [Description]      nvarchar(500)  NULL,
        [ImageUrl]         nvarchar(500)  NULL,
        [IsActive]         bit            NOT NULL DEFAULT 1,
        [DisplayOrder]     int            NOT NULL DEFAULT 0,
        [ParentCategoryId] int            NULL,
        [IsDeleted]        bit            NOT NULL DEFAULT 0,
        [DeletedDate]      datetime2      NULL,
        [CreatedAt]        datetime2      NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]        datetime2      NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Categories] PRIMARY KEY ([Id])
    );
    CREATE INDEX [IX_Categories_ParentCategoryId] ON [Categories] ([ParentCategoryId]);
    CREATE UNIQUE INDEX [IX_Categories_Slug] ON [Categories] ([Slug]) WHERE [IsDeleted] = 0;
    ALTER TABLE [Categories] ADD CONSTRAINT [FK_Categories_Categories_ParentCategoryId]
        FOREIGN KEY ([ParentCategoryId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION;
END");

            // Brands
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='Brands')
BEGIN
    CREATE TABLE [Brands] (
        [Id]              int            NOT NULL IDENTITY(1,1),
        [Name]            nvarchar(100)  NOT NULL,
        [Slug]            nvarchar(100)  NOT NULL,
        [Description]     nvarchar(1000) NULL,
        [LogoUrl]         nvarchar(500)  NULL,
        [BannerUrl]       nvarchar(500)  NULL,
        [Website]         nvarchar(500)  NULL,
        [IsActive]        bit            NOT NULL DEFAULT 1,
        [IsFeatured]      bit            NOT NULL DEFAULT 0,
        [DisplayOrder]    int            NOT NULL DEFAULT 0,
        [MetaTitle]       nvarchar(200)  NULL,
        [MetaDescription] nvarchar(500)  NULL,
        [IsDeleted]       bit            NOT NULL DEFAULT 0,
        [DeletedDate]     datetime2      NULL,
        [CreatedAt]       datetime2      NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]       datetime2      NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Brands] PRIMARY KEY ([Id])
    );
    CREATE UNIQUE INDEX [IX_Brands_Slug] ON [Brands] ([Slug]) WHERE [IsDeleted] = 0;
END");

            // TaxRates
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='TaxRates')
BEGIN
    CREATE TABLE [TaxRates] (
        [Id]                      int           NOT NULL IDENTITY(1,1),
        [Name]                    nvarchar(100) NOT NULL,
        [Code]                    nvarchar(20)  NULL,
        [Rate]                    decimal(5,2)  NOT NULL DEFAULT 0,
        [IsDefault]               bit           NOT NULL DEFAULT 0,
        [IsActive]                bit           NOT NULL DEFAULT 1,
        [AppliesToAllCategories]  bit           NOT NULL DEFAULT 1,
        [ApplicableCategoryIds]   nvarchar(500) NULL,
        [DisplayOrder]            int           NOT NULL DEFAULT 0,
        [IsDeleted]               bit           NOT NULL DEFAULT 0,
        [DeletedDate]             datetime2     NULL,
        [CreatedAt]               datetime2     NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]               datetime2     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_TaxRates] PRIMARY KEY ([Id])
    );
END");

            // ShippingZones
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='ShippingZones')
BEGIN
    CREATE TABLE [ShippingZones] (
        [Id]                      int           NOT NULL IDENTITY(1,1),
        [Name]                    nvarchar(100) NOT NULL,
        [Description]             nvarchar(500) NULL,
        [BaseShippingCost]        decimal(18,2) NOT NULL DEFAULT 0,
        [FreeShippingThreshold]   decimal(18,2) NULL,
        [EstimatedDeliveryDays]   int           NOT NULL DEFAULT 3,
        [IsActive]                bit           NOT NULL DEFAULT 1,
        [DisplayOrder]            int           NOT NULL DEFAULT 0,
        [IsDeleted]               bit           NOT NULL DEFAULT 0,
        [DeletedDate]             datetime2     NULL,
        [CreatedAt]               datetime2     NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]               datetime2     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_ShippingZones] PRIMARY KEY ([Id])
    );
END");

            // Regions (self-referencing, depends on ShippingZones)
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='Regions')
BEGIN
    CREATE TABLE [Regions] (
        [Id]             int           NOT NULL IDENTITY(1,1),
        [Name]           nvarchar(100) NOT NULL,
        [Code]           nvarchar(10)  NULL,
        [Type]           nvarchar(20)  NOT NULL DEFAULT 'Country',
        [ParentId]       int           NULL,
        [IsActive]       bit           NOT NULL DEFAULT 1,
        [ShippingZoneId] int           NULL,
        [DisplayOrder]   int           NOT NULL DEFAULT 0,
        [IsDeleted]      bit           NOT NULL DEFAULT 0,
        [DeletedDate]    datetime2     NULL,
        [CreatedAt]      datetime2     NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]      datetime2     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Regions] PRIMARY KEY ([Id])
    );
    CREATE INDEX [IX_Regions_ParentId] ON [Regions] ([ParentId]);
    ALTER TABLE [Regions] ADD CONSTRAINT [FK_Regions_Regions_ParentId]
        FOREIGN KEY ([ParentId]) REFERENCES [Regions] ([Id]) ON DELETE RESTRICT;
END");

            // EmailTemplates
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='EmailTemplates')
BEGIN
    CREATE TABLE [EmailTemplates] (
        [Id]                 int           NOT NULL IDENTITY(1,1),
        [Name]               nvarchar(100) NOT NULL,
        [Code]               nvarchar(50)  NOT NULL,
        [Subject]            nvarchar(200) NOT NULL,
        [HtmlBody]           nvarchar(max) NOT NULL,
        [TextBody]           nvarchar(max) NULL,
        [Type]               nvarchar(30)  NOT NULL DEFAULT 'Custom',
        [IsActive]           bit           NOT NULL DEFAULT 1,
        [AvailableVariables] nvarchar(2000) NULL,
        [IsDeleted]          bit           NOT NULL DEFAULT 0,
        [DeletedDate]        datetime2     NULL,
        [CreatedAt]          datetime2     NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]          datetime2     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_EmailTemplates] PRIMARY KEY ([Id])
    );
    CREATE UNIQUE INDEX [IX_EmailTemplates_Code] ON [EmailTemplates] ([Code]) WHERE [Code] IS NOT NULL AND [IsDeleted] = 0;
END");

            // ProductAttributes
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='ProductAttributes')
BEGIN
    CREATE TABLE [ProductAttributes] (
        [Id]                    int           NOT NULL IDENTITY(1,1),
        [Name]                  nvarchar(100) NOT NULL,
        [Code]                  nvarchar(50)  NULL,
        [Type]                  nvarchar(20)  NOT NULL DEFAULT 'Select',
        [IsRequired]            bit           NOT NULL DEFAULT 0,
        [IsFilterable]          bit           NOT NULL DEFAULT 1,
        [IsVisibleOnProduct]    bit           NOT NULL DEFAULT 1,
        [DisplayOrder]          int           NOT NULL DEFAULT 0,
        [IsActive]              bit           NOT NULL DEFAULT 1,
        [ApplicableCategoryIds] nvarchar(500) NULL,
        [IsDeleted]             bit           NOT NULL DEFAULT 0,
        [DeletedDate]           datetime2     NULL,
        [CreatedAt]             datetime2     NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]             datetime2     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_ProductAttributes] PRIMARY KEY ([Id])
    );
    CREATE UNIQUE INDEX [IX_ProductAttributes_Code] ON [ProductAttributes] ([Code]) WHERE [Code] IS NOT NULL AND [IsDeleted] = 0;
END");

            // ProductImages
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='ProductImages')
BEGIN
    CREATE TABLE [ProductImages] (
        [Id]            int           NOT NULL IDENTITY(1,1),
        [Url]           nvarchar(500) NOT NULL,
        [ThumbnailUrl]  nvarchar(500) NULL,
        [AltText]       nvarchar(255) NULL,
        [DisplayOrder]  int           NOT NULL DEFAULT 0,
        [IsPrimary]     bit           NOT NULL DEFAULT 0,
        [FileSizeBytes] bigint        NOT NULL DEFAULT 0,
        [ContentType]   nvarchar(50)  NULL,
        [ProductId]     int           NOT NULL,
        [IsDeleted]     bit           NOT NULL DEFAULT 0,
        [DeletedDate]   datetime2     NULL,
        [CreatedAt]     datetime2     NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]     datetime2     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_ProductImages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductImages_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_ProductImages_ProductId] ON [ProductImages] ([ProductId]);
END");

            // Addresses
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='Addresses')
BEGIN
    CREATE TABLE [Addresses] (
        [Id]           int           NOT NULL IDENTITY(1,1),
        [UserId]       int           NOT NULL,
        [Title]        nvarchar(100) NOT NULL,
        [FullName]     nvarchar(100) NOT NULL,
        [Phone]        nvarchar(20)  NOT NULL,
        [City]         nvarchar(100) NOT NULL,
        [District]     nvarchar(100) NOT NULL,
        [Neighborhood] nvarchar(100) NULL,
        [AddressLine]  nvarchar(500) NOT NULL,
        [PostalCode]   nvarchar(10)  NULL,
        [IsDefault]    bit           NOT NULL DEFAULT 0,
        [Type]         nvarchar(20)  NOT NULL DEFAULT 'Shipping',
        [IsDeleted]    bit           NOT NULL DEFAULT 0,
        [DeletedDate]  datetime2     NULL,
        [CreatedAt]    datetime2     NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]    datetime2     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Addresses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Addresses_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_Addresses_UserId] ON [Addresses] ([UserId]);
END");

            // Notifications
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='Notifications')
BEGIN
    CREATE TABLE [Notifications] (
        [Id]                int           NOT NULL IDENTITY(1,1),
        [UserId]            int           NOT NULL,
        [Title]             nvarchar(200) NOT NULL,
        [Message]           nvarchar(1000) NOT NULL,
        [Type]              nvarchar(30)  NOT NULL DEFAULT 'Info',
        [IsRead]            bit           NOT NULL DEFAULT 0,
        [ReadAt]            datetime2     NULL,
        [ActionUrl]         nvarchar(500) NULL,
        [Icon]              nvarchar(100) NULL,
        [RelatedEntityType] nvarchar(50)  NULL,
        [RelatedEntityId]   int           NULL,
        [IsDeleted]         bit           NOT NULL DEFAULT 0,
        [DeletedDate]       datetime2     NULL,
        [CreatedAt]         datetime2     NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]         datetime2     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Notifications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_Notifications_UserId] ON [Notifications] ([UserId]);
END");

            // NotificationPreferences
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='NotificationPreferences')
BEGIN
    CREATE TABLE [NotificationPreferences] (
        [Id]                    int       NOT NULL IDENTITY(1,1),
        [UserId]                int       NOT NULL,
        [EmailOrderUpdates]     bit       NOT NULL DEFAULT 1,
        [EmailPaymentUpdates]   bit       NOT NULL DEFAULT 1,
        [EmailShipmentUpdates]  bit       NOT NULL DEFAULT 1,
        [EmailPromotions]       bit       NOT NULL DEFAULT 1,
        [EmailNewsletter]       bit       NOT NULL DEFAULT 0,
        [EmailPriceAlerts]      bit       NOT NULL DEFAULT 1,
        [EmailStockAlerts]      bit       NOT NULL DEFAULT 1,
        [PushOrderUpdates]      bit       NOT NULL DEFAULT 1,
        [PushPaymentUpdates]    bit       NOT NULL DEFAULT 1,
        [PushShipmentUpdates]   bit       NOT NULL DEFAULT 1,
        [PushPromotions]        bit       NOT NULL DEFAULT 0,
        [PushPriceAlerts]       bit       NOT NULL DEFAULT 1,
        [PushStockAlerts]       bit       NOT NULL DEFAULT 1,
        [SmsOrderUpdates]       bit       NOT NULL DEFAULT 0,
        [SmsShipmentUpdates]    bit       NOT NULL DEFAULT 1,
        [SmsPromotions]         bit       NOT NULL DEFAULT 0,
        [IsDeleted]             bit       NOT NULL DEFAULT 0,
        [DeletedDate]           datetime2 NULL,
        [CreatedAt]             datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]             datetime2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_NotificationPreferences] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_NotificationPreferences_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX [IX_NotificationPreferences_UserId] ON [NotificationPreferences] ([UserId]) WHERE [IsDeleted] = 0;
END");

            // OrderEvents
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='OrderEvents')
BEGIN
    CREATE TABLE [OrderEvents] (
        [Id]          int            NOT NULL IDENTITY(1,1),
        [OrderId]     int            NOT NULL,
        [Status]      nvarchar(30)   NOT NULL DEFAULT 'Pending',
        [Title]       nvarchar(200)  NOT NULL,
        [Description] nvarchar(1000) NULL,
        [UserId]      int            NULL,
        [UserType]    nvarchar(50)   NULL,
        [Metadata]    nvarchar(2000) NULL,
        [IsDeleted]   bit            NOT NULL DEFAULT 0,
        [DeletedDate] datetime2      NULL,
        [CreatedAt]   datetime2      NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]   datetime2      NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_OrderEvents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrderEvents_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_OrderEvents_OrderId] ON [OrderEvents] ([OrderId]);
END");

            // Invoices
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='Invoices')
BEGIN
    CREATE TABLE [Invoices] (
        [Id]              int           NOT NULL IDENTITY(1,1),
        [InvoiceNumber]   nvarchar(30)  NOT NULL,
        [OrderId]         int           NOT NULL,
        [SellerOrderId]   int           NULL,
        [Type]            nvarchar(20)  NOT NULL DEFAULT 'Sale',
        [Status]          nvarchar(20)  NOT NULL DEFAULT 'Draft',
        [BuyerName]       nvarchar(200) NOT NULL,
        [BuyerEmail]      nvarchar(200) NULL,
        [BuyerPhone]      nvarchar(20)  NULL,
        [BuyerAddress]    nvarchar(500) NULL,
        [BuyerTaxNumber]  nvarchar(20)  NULL,
        [SellerName]      nvarchar(200) NULL,
        [SellerAddress]   nvarchar(500) NULL,
        [SellerTaxNumber] nvarchar(20)  NULL,
        [SubTotal]        decimal(18,2) NOT NULL DEFAULT 0,
        [TaxAmount]       decimal(18,2) NOT NULL DEFAULT 0,
        [DiscountAmount]  decimal(18,2) NOT NULL DEFAULT 0,
        [TotalAmount]     decimal(18,2) NOT NULL DEFAULT 0,
        [Currency]        nvarchar(3)   NOT NULL DEFAULT 'TRY',
        [InvoiceDate]     datetime2     NOT NULL DEFAULT GETUTCDATE(),
        [DueDate]         datetime2     NULL,
        [PaidDate]        datetime2     NULL,
        [PdfUrl]          nvarchar(500) NULL,
        [Notes]           nvarchar(1000) NULL,
        [IsDeleted]       bit           NOT NULL DEFAULT 0,
        [DeletedDate]     datetime2     NULL,
        [CreatedAt]       datetime2     NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]       datetime2     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Invoices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Invoices_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE RESTRICT,
        CONSTRAINT [FK_Invoices_SellerOrders_SellerOrderId] FOREIGN KEY ([SellerOrderId]) REFERENCES [SellerOrders] ([Id]) ON DELETE NO ACTION
    );
    CREATE UNIQUE INDEX [IX_Invoices_InvoiceNumber] ON [Invoices] ([InvoiceNumber]) WHERE [IsDeleted] = 0;
    CREATE INDEX [IX_Invoices_OrderId] ON [Invoices] ([OrderId]);
    CREATE INDEX [IX_Invoices_SellerOrderId] ON [Invoices] ([SellerOrderId]);
END");

            // Refunds
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='Refunds')
BEGIN
    CREATE TABLE [Refunds] (
        [Id]                  int           NOT NULL IDENTITY(1,1),
        [RefundNumber]        nvarchar(20)  NOT NULL,
        [OrderId]             int           NOT NULL,
        [SellerOrderId]       int           NULL,
        [Status]              nvarchar(20)  NOT NULL DEFAULT 'Pending',
        [Type]                nvarchar(20)  NOT NULL DEFAULT 'Full',
        [Reason]              nvarchar(500) NOT NULL,
        [Description]         nvarchar(1000) NULL,
        [Amount]              decimal(18,2) NOT NULL DEFAULT 0,
        [Currency]            nvarchar(3)   NOT NULL DEFAULT 'TRY',
        [Method]              nvarchar(30)  NOT NULL DEFAULT 'OriginalPayment',
        [ExternalReference]   nvarchar(100) NULL,
        [ProcessedByUserId]   int           NULL,
        [ProcessedAt]         datetime2     NULL,
        [AdminNote]           nvarchar(500) NULL,
        [RejectionReason]     nvarchar(500) NULL,
        [IsDeleted]           bit           NOT NULL DEFAULT 0,
        [DeletedDate]         datetime2     NULL,
        [CreatedAt]           datetime2     NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]           datetime2     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Refunds] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Refunds_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE RESTRICT,
        CONSTRAINT [FK_Refunds_SellerOrders_SellerOrderId] FOREIGN KEY ([SellerOrderId]) REFERENCES [SellerOrders] ([Id]) ON DELETE NO ACTION
    );
    CREATE UNIQUE INDEX [IX_Refunds_RefundNumber] ON [Refunds] ([RefundNumber]) WHERE [IsDeleted] = 0;
    CREATE INDEX [IX_Refunds_OrderId] ON [Refunds] ([OrderId]);
    CREATE INDEX [IX_Refunds_SellerOrderId] ON [Refunds] ([SellerOrderId]);
END");

            // Reviews
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='Reviews')
BEGIN
    CREATE TABLE [Reviews] (
        [Id]                  int            NOT NULL IDENTITY(1,1),
        [UserId]              int            NOT NULL,
        [ProductId]           int            NULL,
        [SellerId]            int            NULL,
        [OrderId]             int            NULL,
        [SellerOrderId]       int            NULL,
        [Type]                nvarchar(20)   NOT NULL DEFAULT 'Product',
        [Rating]              int            NOT NULL DEFAULT 5,
        [Comment]             nvarchar(2000) NULL,
        [Pros]                nvarchar(500)  NULL,
        [Cons]                nvarchar(500)  NULL,
        [ImageUrls]           nvarchar(2000) NULL,
        [Status]              nvarchar(20)   NOT NULL DEFAULT 'Pending',
        [ModeratedByUserId]   int            NULL,
        [ModeratedAt]         datetime2      NULL,
        [ModerationNote]      nvarchar(500)  NULL,
        [HelpfulCount]        int            NOT NULL DEFAULT 0,
        [NotHelpfulCount]     int            NOT NULL DEFAULT 0,
        [ReportCount]         int            NOT NULL DEFAULT 0,
        [IsVerifiedPurchase]  bit            NOT NULL DEFAULT 0,
        [SellerResponse]      nvarchar(1000) NULL,
        [SellerRespondedAt]   datetime2      NULL,
        [IsDeleted]           bit            NOT NULL DEFAULT 0,
        [DeletedDate]         datetime2      NULL,
        [CreatedAt]           datetime2      NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]           datetime2      NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Reviews] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Reviews_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE RESTRICT,
        CONSTRAINT [FK_Reviews_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Reviews_Users_SellerId] FOREIGN KEY ([SellerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Reviews_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Reviews_SellerOrders_SellerOrderId] FOREIGN KEY ([SellerOrderId]) REFERENCES [SellerOrders] ([Id]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_Reviews_UserId] ON [Reviews] ([UserId]);
    CREATE INDEX [IX_Reviews_ProductId] ON [Reviews] ([ProductId]);
    CREATE INDEX [IX_Reviews_SellerId] ON [Reviews] ([SellerId]);
    CREATE INDEX [IX_Reviews_OrderId] ON [Reviews] ([OrderId]);
    CREATE INDEX [IX_Reviews_SellerOrderId] ON [Reviews] ([SellerOrderId]);
END");

            // SellerPayouts
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='SellerPayouts')
BEGIN
    CREATE TABLE [SellerPayouts] (
        [Id]                   int           NOT NULL IDENTITY(1,1),
        [PayoutNumber]         nvarchar(30)  NOT NULL,
        [SellerId]             int           NOT NULL,
        [PeriodStart]          datetime2     NOT NULL,
        [PeriodEnd]            datetime2     NOT NULL,
        [GrossAmount]          decimal(18,2) NOT NULL DEFAULT 0,
        [CommissionAmount]     decimal(18,2) NOT NULL DEFAULT 0,
        [RefundDeductions]     decimal(18,2) NOT NULL DEFAULT 0,
        [OtherDeductions]      decimal(18,2) NOT NULL DEFAULT 0,
        [NetAmount]            decimal(18,2) NOT NULL DEFAULT 0,
        [Currency]             nvarchar(3)   NOT NULL DEFAULT 'TRY',
        [Status]               nvarchar(20)  NOT NULL DEFAULT 'Pending',
        [OrderCount]           int           NOT NULL DEFAULT 0,
        [ItemCount]            int           NOT NULL DEFAULT 0,
        [BankName]             nvarchar(100) NULL,
        [IBAN]                 nvarchar(34)  NULL,
        [AccountHolderName]    nvarchar(100) NULL,
        [TransferReference]    nvarchar(100) NULL,
        [ApprovedAt]           datetime2     NULL,
        [ApprovedByUserId]     int           NULL,
        [PaidAt]               datetime2     NULL,
        [PaidByUserId]         int           NULL,
        [Notes]                nvarchar(1000) NULL,
        [IsDeleted]            bit           NOT NULL DEFAULT 0,
        [DeletedDate]          datetime2     NULL,
        [CreatedAt]            datetime2     NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]            datetime2     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_SellerPayouts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SellerPayouts_Users_SellerId] FOREIGN KEY ([SellerId]) REFERENCES [Users] ([Id]) ON DELETE RESTRICT
    );
    CREATE INDEX [IX_SellerPayouts_SellerId] ON [SellerPayouts] ([SellerId]);
    CREATE UNIQUE INDEX [IX_SellerPayouts_PayoutNumber] ON [SellerPayouts] ([PayoutNumber]) WHERE [IsDeleted] = 0;
END");

            // HomeWidgets
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='HomeWidgets')
BEGIN
    CREATE TABLE [HomeWidgets] (
        [Id]             int           NOT NULL IDENTITY(1,1),
        [Name]           nvarchar(100) NOT NULL,
        [Code]           nvarchar(50)  NULL,
        [Type]           nvarchar(30)  NOT NULL DEFAULT 'ProductSlider',
        [Title]          nvarchar(200) NULL,
        [Subtitle]       nvarchar(500) NULL,
        [Configuration]  nvarchar(max) NULL,
        [CategoryId]     int           NULL,
        [BrandId]        int           NULL,
        [CampaignId]     int           NULL,
        [ProductIds]     nvarchar(500) NULL,
        [ItemCount]      int           NOT NULL DEFAULT 10,
        [Position]       nvarchar(20)  NOT NULL DEFAULT 'Main',
        [DisplayOrder]   int           NOT NULL DEFAULT 0,
        [IsActive]       bit           NOT NULL DEFAULT 1,
        [ShowOnMobile]   bit           NOT NULL DEFAULT 1,
        [ShowOnDesktop]  bit           NOT NULL DEFAULT 1,
        [StartDate]      datetime2     NULL,
        [EndDate]        datetime2     NULL,
        [IsDeleted]      bit           NOT NULL DEFAULT 0,
        [DeletedDate]    datetime2     NULL,
        [CreatedAt]      datetime2     NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]      datetime2     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_HomeWidgets] PRIMARY KEY ([Id])
    );
    CREATE UNIQUE INDEX [IX_HomeWidgets_Code] ON [HomeWidgets] ([Code]) WHERE [Code] IS NOT NULL AND [IsDeleted] = 0;
END");

            // RecentlyViewed
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='RecentlyViewed')
BEGIN
    CREATE TABLE [RecentlyViewed] (
        [Id]          int       NOT NULL IDENTITY(1,1),
        [UserId]      int       NOT NULL,
        [ProductId]   int       NOT NULL,
        [ViewedAt]    datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [ViewCount]   int       NOT NULL DEFAULT 1,
        [IsDeleted]   bit       NOT NULL DEFAULT 0,
        [DeletedDate] datetime2 NULL,
        [CreatedAt]   datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]   datetime2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_RecentlyViewed] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RecentlyViewed_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RecentlyViewed_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_RecentlyViewed_UserId] ON [RecentlyViewed] ([UserId]);
    CREATE INDEX [IX_RecentlyViewed_ProductId] ON [RecentlyViewed] ([ProductId]);
    CREATE UNIQUE INDEX [IX_RecentlyViewed_UserId_ProductId] ON [RecentlyViewed] ([UserId], [ProductId]) WHERE [IsDeleted] = 0;
END");

            // Wishlists
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='Wishlists')
BEGIN
    CREATE TABLE [Wishlists] (
        [Id]          int           NOT NULL IDENTITY(1,1),
        [UserId]      int           NOT NULL,
        [Name]        nvarchar(100) NOT NULL DEFAULT 'Favorilerim',
        [IsDefault]   bit           NOT NULL DEFAULT 1,
        [IsPublic]    bit           NOT NULL DEFAULT 0,
        [IsDeleted]   bit           NOT NULL DEFAULT 0,
        [DeletedDate] datetime2     NULL,
        [CreatedAt]   datetime2     NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]   datetime2     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Wishlists] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Wishlists_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_Wishlists_UserId] ON [Wishlists] ([UserId]);
END");

            // GiftCards
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='GiftCards')
BEGIN
    CREATE TABLE [GiftCards] (
        [Id]                  int           NOT NULL IDENTITY(1,1),
        [Code]                nvarchar(20)  NOT NULL,
        [InitialBalance]      decimal(18,2) NOT NULL DEFAULT 0,
        [CurrentBalance]      decimal(18,2) NOT NULL DEFAULT 0,
        [Currency]            nvarchar(3)   NOT NULL DEFAULT 'TRY',
        [Status]              nvarchar(20)  NOT NULL DEFAULT 'Active',
        [ExpiresAt]           datetime2     NULL,
        [PurchasedByUserId]   int           NULL,
        [OrderId]             int           NULL,
        [RecipientUserId]     int           NULL,
        [RecipientEmail]      nvarchar(200) NULL,
        [RecipientName]       nvarchar(100) NULL,
        [PersonalMessage]     nvarchar(500) NULL,
        [IsSentToRecipient]   bit           NOT NULL DEFAULT 0,
        [SentAt]              datetime2     NULL,
        [RedeemedAt]          datetime2     NULL,
        [RedeemedByUserId]    int           NULL,
        [AdminNotes]          nvarchar(500) NULL,
        [IsDeleted]           bit           NOT NULL DEFAULT 0,
        [DeletedDate]         datetime2     NULL,
        [CreatedAt]           datetime2     NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]           datetime2     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_GiftCards] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_GiftCards_Users_PurchasedByUserId] FOREIGN KEY ([PurchasedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_GiftCards_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_GiftCards_Users_RecipientUserId] FOREIGN KEY ([RecipientUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
    CREATE UNIQUE INDEX [IX_GiftCards_Code] ON [GiftCards] ([Code]) WHERE [IsDeleted] = 0;
    CREATE INDEX [IX_GiftCards_PurchasedByUserId] ON [GiftCards] ([PurchasedByUserId]);
    CREATE INDEX [IX_GiftCards_RecipientUserId] ON [GiftCards] ([RecipientUserId]);
END");

            // Affiliates
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='Affiliates')
BEGIN
    CREATE TABLE [Affiliates] (
        [Id]                   int           NOT NULL IDENTITY(1,1),
        [UserId]               int           NOT NULL,
        [ReferralCode]         nvarchar(20)  NOT NULL,
        [CommissionRate]       decimal(5,2)  NOT NULL DEFAULT 5,
        [MinPayoutAmount]      decimal(18,2) NOT NULL DEFAULT 100,
        [TotalReferrals]       int           NOT NULL DEFAULT 0,
        [SuccessfulReferrals]  int           NOT NULL DEFAULT 0,
        [TotalEarnings]        decimal(18,2) NOT NULL DEFAULT 0,
        [PendingEarnings]      decimal(18,2) NOT NULL DEFAULT 0,
        [PaidEarnings]         decimal(18,2) NOT NULL DEFAULT 0,
        [Status]               nvarchar(20)  NOT NULL DEFAULT 'Pending',
        [ApprovedAt]           datetime2     NULL,
        [ApprovedByUserId]     int           NULL,
        [BankName]             nvarchar(100) NULL,
        [IBAN]                 nvarchar(34)  NULL,
        [AccountHolderName]    nvarchar(100) NULL,
        [IsDeleted]            bit           NOT NULL DEFAULT 0,
        [DeletedDate]          datetime2     NULL,
        [CreatedAt]            datetime2     NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]            datetime2     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Affiliates] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Affiliates_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE RESTRICT
    );
    CREATE UNIQUE INDEX [IX_Affiliates_UserId] ON [Affiliates] ([UserId]) WHERE [IsDeleted] = 0;
    CREATE UNIQUE INDEX [IX_Affiliates_ReferralCode] ON [Affiliates] ([ReferralCode]) WHERE [IsDeleted] = 0;
END");

            // ProductQuestions
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='ProductQuestions')
BEGIN
    CREATE TABLE [ProductQuestions] (
        [Id]             int            NOT NULL IDENTITY(1,1),
        [ProductId]      int            NOT NULL,
        [AskedByUserId]  int            NOT NULL,
        [QuestionText]   nvarchar(1000) NOT NULL,
        [Status]         nvarchar(20)   NOT NULL DEFAULT 'Pending',
        [UpvoteCount]    int            NOT NULL DEFAULT 0,
        [IsDeleted]      bit            NOT NULL DEFAULT 0,
        [DeletedDate]    datetime2      NULL,
        [CreatedAt]      datetime2      NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]      datetime2      NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_ProductQuestions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductQuestions_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProductQuestions_Users_AskedByUserId] FOREIGN KEY ([AskedByUserId]) REFERENCES [Users] ([Id]) ON DELETE RESTRICT
    );
    CREATE INDEX [IX_ProductQuestions_ProductId] ON [ProductQuestions] ([ProductId]);
    CREATE INDEX [IX_ProductQuestions_AskedByUserId] ON [ProductQuestions] ([AskedByUserId]);
END");

            // LoyaltyPoints
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='LoyaltyPoints')
BEGIN
    CREATE TABLE [LoyaltyPoints] (
        [Id]              int       NOT NULL IDENTITY(1,1),
        [UserId]          int       NOT NULL,
        [TotalPoints]     int       NOT NULL DEFAULT 0,
        [AvailablePoints] int       NOT NULL DEFAULT 0,
        [SpentPoints]     int       NOT NULL DEFAULT 0,
        [PendingPoints]   int       NOT NULL DEFAULT 0,
        [Tier]            nvarchar(20) NOT NULL DEFAULT 'Bronze',
        [IsDeleted]       bit       NOT NULL DEFAULT 0,
        [DeletedDate]     datetime2 NULL,
        [CreatedAt]       datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]       datetime2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_LoyaltyPoints] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LoyaltyPoints_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX [IX_LoyaltyPoints_UserId] ON [LoyaltyPoints] ([UserId]) WHERE [IsDeleted] = 0;
END");

            // SellerReviews
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='SellerReviews')
BEGIN
    CREATE TABLE [SellerReviews] (
        [Id]                  int            NOT NULL IDENTITY(1,1),
        [SellerId]            int            NOT NULL,
        [UserId]              int            NOT NULL,
        [OrderId]             int            NULL,
        [Rating]              int            NOT NULL DEFAULT 5,
        [Comment]             nvarchar(1000) NULL,
        [ShippingRating]      int            NULL,
        [CommunicationRating] int            NULL,
        [PackagingRating]     int            NULL,
        [Status]              nvarchar(20)   NOT NULL DEFAULT 'Active',
        [SellerReply]         nvarchar(1000) NULL,
        [SellerReplyDate]     datetime2      NULL,
        [IsDeleted]           bit            NOT NULL DEFAULT 0,
        [DeletedDate]         datetime2      NULL,
        [CreatedAt]           datetime2      NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]           datetime2      NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_SellerReviews] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SellerReviews_Users_SellerId] FOREIGN KEY ([SellerId]) REFERENCES [Users] ([Id]) ON DELETE RESTRICT,
        CONSTRAINT [FK_SellerReviews_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SellerReviews_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_SellerReviews_SellerId] ON [SellerReviews] ([SellerId]);
    CREATE INDEX [IX_SellerReviews_UserId] ON [SellerReviews] ([UserId]);
    CREATE INDEX [IX_SellerReviews_OrderId] ON [SellerReviews] ([OrderId]);
END");

            // ProductComparisons
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='ProductComparisons')
BEGIN
    CREATE TABLE [ProductComparisons] (
        [Id]          int           NOT NULL IDENTITY(1,1),
        [UserId]      int           NOT NULL,
        [Name]        nvarchar(200) NULL,
        [IsDeleted]   bit           NOT NULL DEFAULT 0,
        [DeletedDate] datetime2     NULL,
        [CreatedAt]   datetime2     NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]   datetime2     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_ProductComparisons] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductComparisons_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_ProductComparisons_UserId] ON [ProductComparisons] ([UserId]);
END");

            // ─────────────────────────────────────────────────────────────────
            // BATCH 2: Tables depending on Batch 1 tables
            // ─────────────────────────────────────────────────────────────────

            // ProductAttributeValues
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='ProductAttributeValues')
BEGIN
    CREATE TABLE [ProductAttributeValues] (
        [Id]           int           NOT NULL IDENTITY(1,1),
        [AttributeId]  int           NOT NULL,
        [Value]        nvarchar(100) NOT NULL,
        [Code]         nvarchar(50)  NULL,
        [ColorHex]     nvarchar(7)   NULL,
        [ImageUrl]     nvarchar(500) NULL,
        [DisplayOrder] int           NOT NULL DEFAULT 0,
        [IsActive]     bit           NOT NULL DEFAULT 1,
        [IsDeleted]    bit           NOT NULL DEFAULT 0,
        [DeletedDate]  datetime2     NULL,
        [CreatedAt]    datetime2     NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]    datetime2     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_ProductAttributeValues] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductAttributeValues_ProductAttributes_AttributeId] FOREIGN KEY ([AttributeId]) REFERENCES [ProductAttributes] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_ProductAttributeValues_AttributeId] ON [ProductAttributeValues] ([AttributeId]);
END");

            // ReviewVotes
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='ReviewVotes')
BEGIN
    CREATE TABLE [ReviewVotes] (
        [Id]          int       NOT NULL IDENTITY(1,1),
        [ReviewId]    int       NOT NULL,
        [UserId]      int       NOT NULL,
        [IsHelpful]   bit       NOT NULL DEFAULT 1,
        [IsDeleted]   bit       NOT NULL DEFAULT 0,
        [DeletedDate] datetime2 NULL,
        [CreatedAt]   datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]   datetime2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_ReviewVotes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ReviewVotes_Reviews_ReviewId] FOREIGN KEY ([ReviewId]) REFERENCES [Reviews] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ReviewVotes_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE RESTRICT
    );
    CREATE INDEX [IX_ReviewVotes_ReviewId] ON [ReviewVotes] ([ReviewId]);
    CREATE INDEX [IX_ReviewVotes_UserId] ON [ReviewVotes] ([UserId]);
    CREATE UNIQUE INDEX [IX_ReviewVotes_ReviewId_UserId] ON [ReviewVotes] ([ReviewId], [UserId]) WHERE [IsDeleted] = 0;
END");

            // ReviewReports
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='ReviewReports')
BEGIN
    CREATE TABLE [ReviewReports] (
        [Id]                int           NOT NULL IDENTITY(1,1),
        [ReviewId]          int           NOT NULL,
        [UserId]            int           NOT NULL,
        [Reason]            nvarchar(30)  NOT NULL DEFAULT 'Other',
        [Description]       nvarchar(500) NULL,
        [IsResolved]        bit           NOT NULL DEFAULT 0,
        [ResolvedByUserId]  int           NULL,
        [ResolvedAt]        datetime2     NULL,
        [IsDeleted]         bit           NOT NULL DEFAULT 0,
        [DeletedDate]       datetime2     NULL,
        [CreatedAt]         datetime2     NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]         datetime2     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_ReviewReports] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ReviewReports_Reviews_ReviewId] FOREIGN KEY ([ReviewId]) REFERENCES [Reviews] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ReviewReports_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE RESTRICT
    );
    CREATE INDEX [IX_ReviewReports_ReviewId] ON [ReviewReports] ([ReviewId]);
    CREATE INDEX [IX_ReviewReports_UserId] ON [ReviewReports] ([UserId]);
END");

            // SellerPayoutItems
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='SellerPayoutItems')
BEGIN
    CREATE TABLE [SellerPayoutItems] (
        [Id]               int           NOT NULL IDENTITY(1,1),
        [PayoutId]         int           NOT NULL,
        [SellerOrderId]    int           NOT NULL,
        [OrderAmount]      decimal(18,2) NOT NULL DEFAULT 0,
        [CommissionAmount] decimal(18,2) NOT NULL DEFAULT 0,
        [NetAmount]        decimal(18,2) NOT NULL DEFAULT 0,
        [IsDeleted]        bit           NOT NULL DEFAULT 0,
        [DeletedDate]      datetime2     NULL,
        [CreatedAt]        datetime2     NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]        datetime2     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_SellerPayoutItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SellerPayoutItems_SellerPayouts_PayoutId] FOREIGN KEY ([PayoutId]) REFERENCES [SellerPayouts] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_SellerPayoutItems_SellerOrders_SellerOrderId] FOREIGN KEY ([SellerOrderId]) REFERENCES [SellerOrders] ([Id]) ON DELETE RESTRICT
    );
    CREATE INDEX [IX_SellerPayoutItems_PayoutId] ON [SellerPayoutItems] ([PayoutId]);
    CREATE INDEX [IX_SellerPayoutItems_SellerOrderId] ON [SellerPayoutItems] ([SellerOrderId]);
END");

            // WishlistItems
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='WishlistItems')
BEGIN
    CREATE TABLE [WishlistItems] (
        [Id]                  int           NOT NULL IDENTITY(1,1),
        [WishlistId]          int           NOT NULL,
        [ProductId]           int           NOT NULL,
        [ListingId]           int           NULL,
        [Note]                nvarchar(500) NULL,
        [NotifyOnPriceDrop]   bit           NOT NULL DEFAULT 0,
        [PriceAtAdd]          decimal(18,2) NULL,
        [IsDeleted]           bit           NOT NULL DEFAULT 0,
        [DeletedDate]         datetime2     NULL,
        [CreatedAt]           datetime2     NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]           datetime2     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_WishlistItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WishlistItems_Wishlists_WishlistId] FOREIGN KEY ([WishlistId]) REFERENCES [Wishlists] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_WishlistItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE RESTRICT,
        CONSTRAINT [FK_WishlistItems_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_WishlistItems_WishlistId] ON [WishlistItems] ([WishlistId]);
    CREATE INDEX [IX_WishlistItems_ProductId] ON [WishlistItems] ([ProductId]);
    CREATE INDEX [IX_WishlistItems_ListingId] ON [WishlistItems] ([ListingId]);
END");

            // GiftCardUsages
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='GiftCardUsages')
BEGIN
    CREATE TABLE [GiftCardUsages] (
        [Id]           int           NOT NULL IDENTITY(1,1),
        [GiftCardId]   int           NOT NULL,
        [OrderId]      int           NULL,
        [UserId]       int           NOT NULL,
        [Amount]       decimal(18,2) NOT NULL DEFAULT 0,
        [BalanceAfter] decimal(18,2) NOT NULL DEFAULT 0,
        [Type]         nvarchar(20)  NOT NULL DEFAULT 'Purchase',
        [Description]  nvarchar(500) NULL,
        [IsDeleted]    bit           NOT NULL DEFAULT 0,
        [DeletedDate]  datetime2     NULL,
        [CreatedAt]    datetime2     NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]    datetime2     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_GiftCardUsages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_GiftCardUsages_GiftCards_GiftCardId] FOREIGN KEY ([GiftCardId]) REFERENCES [GiftCards] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_GiftCardUsages_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_GiftCardUsages_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE RESTRICT
    );
    CREATE INDEX [IX_GiftCardUsages_GiftCardId] ON [GiftCardUsages] ([GiftCardId]);
    CREATE INDEX [IX_GiftCardUsages_OrderId] ON [GiftCardUsages] ([OrderId]);
    CREATE INDEX [IX_GiftCardUsages_UserId] ON [GiftCardUsages] ([UserId]);
END");

            // AffiliateReferrals
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='AffiliateReferrals')
BEGIN
    CREATE TABLE [AffiliateReferrals] (
        [Id]               int           NOT NULL IDENTITY(1,1),
        [AffiliateId]      int           NOT NULL,
        [ReferredUserId]   int           NOT NULL,
        [OrderId]          int           NULL,
        [OrderAmount]      decimal(18,2) NOT NULL DEFAULT 0,
        [CommissionAmount] decimal(18,2) NOT NULL DEFAULT 0,
        [Status]           nvarchar(20)  NOT NULL DEFAULT 'Pending',
        [ReferralUrl]      nvarchar(500) NULL,
        [UtmSource]        nvarchar(100) NULL,
        [UtmMedium]        nvarchar(100) NULL,
        [UtmCampaign]      nvarchar(100) NULL,
        [IsDeleted]        bit           NOT NULL DEFAULT 0,
        [DeletedDate]      datetime2     NULL,
        [CreatedAt]        datetime2     NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]        datetime2     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_AffiliateReferrals] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AffiliateReferrals_Affiliates_AffiliateId] FOREIGN KEY ([AffiliateId]) REFERENCES [Affiliates] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AffiliateReferrals_Users_ReferredUserId] FOREIGN KEY ([ReferredUserId]) REFERENCES [Users] ([Id]) ON DELETE RESTRICT,
        CONSTRAINT [FK_AffiliateReferrals_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_AffiliateReferrals_AffiliateId] ON [AffiliateReferrals] ([AffiliateId]);
    CREATE INDEX [IX_AffiliateReferrals_ReferredUserId] ON [AffiliateReferrals] ([ReferredUserId]);
    CREATE INDEX [IX_AffiliateReferrals_OrderId] ON [AffiliateReferrals] ([OrderId]);
END");

            // AffiliatePayouts
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='AffiliatePayouts')
BEGIN
    CREATE TABLE [AffiliatePayouts] (
        [Id]                int           NOT NULL IDENTITY(1,1),
        [AffiliateId]       int           NOT NULL,
        [PayoutNumber]      nvarchar(30)  NOT NULL,
        [Amount]            decimal(18,2) NOT NULL DEFAULT 0,
        [Currency]          nvarchar(3)   NOT NULL DEFAULT 'TRY',
        [Status]            nvarchar(20)  NOT NULL DEFAULT 'Pending',
        [TransferReference] nvarchar(100) NULL,
        [PaidAt]            datetime2     NULL,
        [PaidByUserId]      int           NULL,
        [Notes]             nvarchar(500) NULL,
        [IsDeleted]         bit           NOT NULL DEFAULT 0,
        [DeletedDate]       datetime2     NULL,
        [CreatedAt]         datetime2     NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]         datetime2     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_AffiliatePayouts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AffiliatePayouts_Affiliates_AffiliateId] FOREIGN KEY ([AffiliateId]) REFERENCES [Affiliates] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_AffiliatePayouts_AffiliateId] ON [AffiliatePayouts] ([AffiliateId]);
END");

            // ProductAnswers
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='ProductAnswers')
BEGIN
    CREATE TABLE [ProductAnswers] (
        [Id]               int            NOT NULL IDENTITY(1,1),
        [QuestionId]       int            NOT NULL,
        [AnsweredByUserId] int            NOT NULL,
        [AnswerText]       nvarchar(2000) NOT NULL,
        [IsSellerAnswer]   bit            NOT NULL DEFAULT 0,
        [IsAccepted]       bit            NOT NULL DEFAULT 0,
        [UpvoteCount]      int            NOT NULL DEFAULT 0,
        [IsDeleted]        bit            NOT NULL DEFAULT 0,
        [DeletedDate]      datetime2      NULL,
        [CreatedAt]        datetime2      NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]        datetime2      NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_ProductAnswers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductAnswers_ProductQuestions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [ProductQuestions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProductAnswers_Users_AnsweredByUserId] FOREIGN KEY ([AnsweredByUserId]) REFERENCES [Users] ([Id]) ON DELETE RESTRICT
    );
    CREATE INDEX [IX_ProductAnswers_QuestionId] ON [ProductAnswers] ([QuestionId]);
    CREATE INDEX [IX_ProductAnswers_AnsweredByUserId] ON [ProductAnswers] ([AnsweredByUserId]);
END");

            // LoyaltyTransactions
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='LoyaltyTransactions')
BEGIN
    CREATE TABLE [LoyaltyTransactions] (
        [Id]              int           NOT NULL IDENTITY(1,1),
        [LoyaltyPointId]  int           NOT NULL,
        [Points]          int           NOT NULL DEFAULT 0,
        [Type]            nvarchar(20)  NOT NULL DEFAULT 'Earned',
        [Description]     nvarchar(500) NOT NULL DEFAULT '',
        [OrderId]         int           NULL,
        [ReferenceCode]   nvarchar(50)  NULL,
        [ExpiresAt]       datetime2     NULL,
        [IsDeleted]       bit           NOT NULL DEFAULT 0,
        [DeletedDate]     datetime2     NULL,
        [CreatedAt]       datetime2     NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]       datetime2     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_LoyaltyTransactions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LoyaltyTransactions_LoyaltyPoints_LoyaltyPointId] FOREIGN KEY ([LoyaltyPointId]) REFERENCES [LoyaltyPoints] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_LoyaltyTransactions_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_LoyaltyTransactions_LoyaltyPointId] ON [LoyaltyTransactions] ([LoyaltyPointId]);
    CREATE INDEX [IX_LoyaltyTransactions_OrderId] ON [LoyaltyTransactions] ([OrderId]);
END");

            // ProductComparisonItems
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='ProductComparisonItems')
BEGIN
    CREATE TABLE [ProductComparisonItems] (
        [Id]           int       NOT NULL IDENTITY(1,1),
        [ComparisonId] int       NOT NULL,
        [ProductId]    int       NOT NULL,
        [DisplayOrder] int       NOT NULL DEFAULT 0,
        [IsDeleted]    bit       NOT NULL DEFAULT 0,
        [DeletedDate]  datetime2 NULL,
        [CreatedAt]    datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]    datetime2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_ProductComparisonItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductComparisonItems_ProductComparisons_ComparisonId] FOREIGN KEY ([ComparisonId]) REFERENCES [ProductComparisons] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProductComparisonItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE RESTRICT
    );
    CREATE INDEX [IX_ProductComparisonItems_ComparisonId] ON [ProductComparisonItems] ([ComparisonId]);
    CREATE INDEX [IX_ProductComparisonItems_ProductId] ON [ProductComparisonItems] ([ProductId]);
    CREATE UNIQUE INDEX [IX_ProductComparisonItems_ComparisonId_ProductId] ON [ProductComparisonItems] ([ComparisonId], [ProductId]) WHERE [IsDeleted] = 0;
END");

            // ─────────────────────────────────────────────────────────────────
            // BATCH 3: Tables depending on Batch 2 tables
            // ─────────────────────────────────────────────────────────────────

            // ProductAttributeMappings
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='ProductAttributeMappings')
BEGIN
    CREATE TABLE [ProductAttributeMappings] (
        [Id]                 int       NOT NULL IDENTITY(1,1),
        [ProductId]          int       NOT NULL,
        [AttributeId]        int       NOT NULL,
        [AttributeValueId]   int       NOT NULL,
        [IsDeleted]          bit       NOT NULL DEFAULT 0,
        [DeletedDate]        datetime2 NULL,
        [CreatedAt]          datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]          datetime2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_ProductAttributeMappings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductAttributeMappings_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProductAttributeMappings_ProductAttributes_AttributeId] FOREIGN KEY ([AttributeId]) REFERENCES [ProductAttributes] ([Id]) ON DELETE RESTRICT,
        CONSTRAINT [FK_ProductAttributeMappings_ProductAttributeValues_AttributeValueId] FOREIGN KEY ([AttributeValueId]) REFERENCES [ProductAttributeValues] ([Id]) ON DELETE RESTRICT
    );
    CREATE INDEX [IX_ProductAttributeMappings_ProductId] ON [ProductAttributeMappings] ([ProductId]);
    CREATE INDEX [IX_ProductAttributeMappings_AttributeId] ON [ProductAttributeMappings] ([AttributeId]);
    CREATE INDEX [IX_ProductAttributeMappings_AttributeValueId] ON [ProductAttributeMappings] ([AttributeValueId]);
END");

            // ─────────────────────────────────────────────────────────────────
            // Fix Campaign.SellerId - make it nullable for platform campaigns
            // ─────────────────────────────────────────────────────────────────
            migrationBuilder.Sql(@"
IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='Campaigns')
    AND EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Campaigns') AND name = 'SellerId' AND is_nullable = 0)
BEGIN
    -- Drop existing FK
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Campaigns_Users_SellerId')
        ALTER TABLE [Campaigns] DROP CONSTRAINT [FK_Campaigns_Users_SellerId];

    -- Make column nullable
    ALTER TABLE [Campaigns] ALTER COLUMN [SellerId] int NULL;

    -- Set 0 values to NULL (platform campaigns)
    UPDATE [Campaigns] SET [SellerId] = NULL WHERE [SellerId] = 0;

    -- Re-add FK as optional
    ALTER TABLE [Campaigns] ADD CONSTRAINT [FK_Campaigns_Users_SellerId]
        FOREIGN KEY ([SellerId]) REFERENCES [Users] ([Id]) ON DELETE RESTRICT;
END");

            // ─────────────────────────────────────────────────────────────────
            // Add missing FKs for existing tables (if tables now exist)
            // ─────────────────────────────────────────────────────────────────

            // CampaignCategories FK to Categories (if not already added)
            migrationBuilder.Sql(@"
IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='CampaignCategories')
    AND EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='Categories')
    AND NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_CampaignCategories_Categories_CategoryId')
BEGIN
    ALTER TABLE [CampaignCategories] ADD CONSTRAINT [FK_CampaignCategories_Categories_CategoryId]
        FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE RESTRICT;
END");

            // ReturnShipments FK to Refunds (if not already added)
            migrationBuilder.Sql(@"
IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='ReturnShipments')
    AND EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='Refunds')
    AND NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_ReturnShipments_Refunds_RefundId')
BEGIN
    ALTER TABLE [ReturnShipments] ADD CONSTRAINT [FK_ReturnShipments_Refunds_RefundId]
        FOREIGN KEY ([RefundId]) REFERENCES [Refunds] ([Id]) ON DELETE NO ACTION;
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove FKs added to existing tables
            migrationBuilder.Sql(@"
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_CampaignCategories_Categories_CategoryId')
    ALTER TABLE [CampaignCategories] DROP CONSTRAINT [FK_CampaignCategories_Categories_CategoryId];
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_ReturnShipments_Refunds_RefundId')
    ALTER TABLE [ReturnShipments] DROP CONSTRAINT [FK_ReturnShipments_Refunds_RefundId];");

            // Revert Campaign.SellerId to NOT NULL
            migrationBuilder.Sql(@"
IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='Campaigns')
BEGIN
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Campaigns_Users_SellerId')
        ALTER TABLE [Campaigns] DROP CONSTRAINT [FK_Campaigns_Users_SellerId];
    UPDATE [Campaigns] SET [SellerId] = 0 WHERE [SellerId] IS NULL;
    ALTER TABLE [Campaigns] ALTER COLUMN [SellerId] int NOT NULL;
    ALTER TABLE [Campaigns] ADD CONSTRAINT [FK_Campaigns_Users_SellerId]
        FOREIGN KEY ([SellerId]) REFERENCES [Users] ([Id]) ON DELETE RESTRICT;
END");

            // Drop tables in reverse dependency order
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='ProductAttributeMappings') DROP TABLE [ProductAttributeMappings];");
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='ProductComparisonItems') DROP TABLE [ProductComparisonItems];");
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='LoyaltyTransactions') DROP TABLE [LoyaltyTransactions];");
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='ProductAnswers') DROP TABLE [ProductAnswers];");
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='AffiliatePayouts') DROP TABLE [AffiliatePayouts];");
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='AffiliateReferrals') DROP TABLE [AffiliateReferrals];");
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='GiftCardUsages') DROP TABLE [GiftCardUsages];");
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='WishlistItems') DROP TABLE [WishlistItems];");
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='SellerPayoutItems') DROP TABLE [SellerPayoutItems];");
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='ReviewReports') DROP TABLE [ReviewReports];");
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='ReviewVotes') DROP TABLE [ReviewVotes];");
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='ProductAttributeValues') DROP TABLE [ProductAttributeValues];");
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='ProductComparisons') DROP TABLE [ProductComparisons];");
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='SellerReviews') DROP TABLE [SellerReviews];");
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='LoyaltyPoints') DROP TABLE [LoyaltyPoints];");
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='ProductQuestions') DROP TABLE [ProductQuestions];");
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='Affiliates') DROP TABLE [Affiliates];");
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='GiftCards') DROP TABLE [GiftCards];");
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='Wishlists') DROP TABLE [Wishlists];");
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='RecentlyViewed') DROP TABLE [RecentlyViewed];");
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='HomeWidgets') DROP TABLE [HomeWidgets];");
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='SellerPayouts') DROP TABLE [SellerPayouts];");
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='Reviews') DROP TABLE [Reviews];");
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='Refunds') DROP TABLE [Refunds];");
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='Invoices') DROP TABLE [Invoices];");
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='OrderEvents') DROP TABLE [OrderEvents];");
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='NotificationPreferences') DROP TABLE [NotificationPreferences];");
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='Notifications') DROP TABLE [Notifications];");
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='Addresses') DROP TABLE [Addresses];");
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='ProductImages') DROP TABLE [ProductImages];");
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='ProductAttributes') DROP TABLE [ProductAttributes];");
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='EmailTemplates') DROP TABLE [EmailTemplates];");
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='Regions') DROP TABLE [Regions];");
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='ShippingZones') DROP TABLE [ShippingZones];");
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='TaxRates') DROP TABLE [TaxRates];");
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='Brands') DROP TABLE [Brands];");
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='Categories') DROP TABLE [Categories];");
        }
    }
}
