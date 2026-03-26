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
            migrationBuilder.Sql(@"
                CREATE TABLE [TaxRates] (
                    [Id] int NOT NULL IDENTITY,
                    [Name] nvarchar(100) NOT NULL,
                    [Code] nvarchar(20) NULL,
                    [Rate] decimal(5,2) NOT NULL,
                    [IsDefault] bit NOT NULL,
                    [IsActive] bit NOT NULL,
                    [AppliesToAllCategories] bit NOT NULL,
                    [ApplicableCategoryIds] nvarchar(500) NULL,
                    [DisplayOrder] int NOT NULL,
                    [IsDeleted] bit NOT NULL,
                    [DeletedDate] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_TaxRates] PRIMARY KEY ([Id])
                );
                CREATE UNIQUE INDEX [IX_TaxRates_Code] ON [TaxRates] ([Code]) WHERE [Code] IS NOT NULL AND [IsDeleted] = 0;

                CREATE TABLE [Brands] (
                    [Id] int NOT NULL IDENTITY,
                    [Name] nvarchar(100) NOT NULL,
                    [Slug] nvarchar(100) NOT NULL,
                    [Description] nvarchar(1000) NULL,
                    [LogoUrl] nvarchar(500) NULL,
                    [BannerUrl] nvarchar(500) NULL,
                    [Website] nvarchar(500) NULL,
                    [IsActive] bit NOT NULL,
                    [IsFeatured] bit NOT NULL,
                    [DisplayOrder] int NOT NULL,
                    [MetaTitle] nvarchar(200) NULL,
                    [MetaDescription] nvarchar(500) NULL,
                    [IsDeleted] bit NOT NULL,
                    [DeletedDate] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_Brands] PRIMARY KEY ([Id])
                );
                CREATE UNIQUE INDEX [IX_Brands_Slug] ON [Brands] ([Slug]) WHERE [IsDeleted] = 0;

                CREATE TABLE [EmailTemplates] (
                    [Id] int NOT NULL IDENTITY,
                    [Name] nvarchar(100) NOT NULL,
                    [Code] nvarchar(50) NOT NULL,
                    [Subject] nvarchar(200) NOT NULL,
                    [HtmlBody] nvarchar(max) NOT NULL,
                    [TextBody] nvarchar(max) NULL,
                    [Type] nvarchar(30) NOT NULL,
                    [IsActive] bit NOT NULL,
                    [AvailableVariables] nvarchar(2000) NULL,
                    [IsDeleted] bit NOT NULL,
                    [DeletedDate] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_EmailTemplates] PRIMARY KEY ([Id])
                );
                CREATE UNIQUE INDEX [IX_EmailTemplates_Code] ON [EmailTemplates] ([Code]) WHERE [IsDeleted] = 0;

                CREATE TABLE [ShippingZones] (
                    [Id] int NOT NULL IDENTITY,
                    [Name] nvarchar(100) NOT NULL,
                    [Description] nvarchar(500) NULL,
                    [BaseShippingCost] decimal(18,2) NOT NULL,
                    [FreeShippingThreshold] decimal(18,2) NULL,
                    [EstimatedDeliveryDays] int NOT NULL,
                    [IsActive] bit NOT NULL,
                    [DisplayOrder] int NOT NULL,
                    [IsDeleted] bit NOT NULL,
                    [DeletedDate] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_ShippingZones] PRIMARY KEY ([Id])
                );

                CREATE TABLE [ProductAttributes] (
                    [Id] int NOT NULL IDENTITY,
                    [Name] nvarchar(100) NOT NULL,
                    [Code] nvarchar(50) NULL,
                    [Type] nvarchar(20) NOT NULL,
                    [IsRequired] bit NOT NULL,
                    [IsFilterable] bit NOT NULL,
                    [IsVisibleOnProduct] bit NOT NULL,
                    [DisplayOrder] int NOT NULL,
                    [IsActive] bit NOT NULL,
                    [ApplicableCategoryIds] nvarchar(500) NULL,
                    [IsDeleted] bit NOT NULL,
                    [DeletedDate] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_ProductAttributes] PRIMARY KEY ([Id])
                );
                CREATE UNIQUE INDEX [IX_ProductAttributes_Code] ON [ProductAttributes] ([Code]) WHERE [Code] IS NOT NULL AND [IsDeleted] = 0;

                CREATE TABLE [HomeWidgets] (
                    [Id] int NOT NULL IDENTITY,
                    [Name] nvarchar(100) NOT NULL,
                    [Code] nvarchar(50) NULL,
                    [Type] nvarchar(30) NOT NULL,
                    [Title] nvarchar(200) NULL,
                    [Subtitle] nvarchar(500) NULL,
                    [Configuration] nvarchar(max) NULL,
                    [CategoryId] int NULL,
                    [BrandId] int NULL,
                    [CampaignId] int NULL,
                    [ProductIds] nvarchar(500) NULL,
                    [ItemCount] int NOT NULL,
                    [Position] nvarchar(20) NOT NULL,
                    [DisplayOrder] int NOT NULL,
                    [IsActive] bit NOT NULL,
                    [ShowOnMobile] bit NOT NULL,
                    [ShowOnDesktop] bit NOT NULL,
                    [StartDate] datetime2 NULL,
                    [EndDate] datetime2 NULL,
                    [IsDeleted] bit NOT NULL,
                    [DeletedDate] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_HomeWidgets] PRIMARY KEY ([Id])
                );
                CREATE UNIQUE INDEX [IX_HomeWidgets_Code] ON [HomeWidgets] ([Code]) WHERE [Code] IS NOT NULL AND [IsDeleted] = 0;
                CREATE INDEX [IX_HomeWidgets_Position_DisplayOrder] ON [HomeWidgets] ([Position], [DisplayOrder]);

                CREATE TABLE [Addresses] (
                    [Id] int NOT NULL IDENTITY,
                    [UserId] int NOT NULL,
                    [Title] nvarchar(100) NOT NULL,
                    [FullName] nvarchar(100) NOT NULL,
                    [Phone] nvarchar(20) NOT NULL,
                    [City] nvarchar(100) NOT NULL,
                    [District] nvarchar(100) NOT NULL,
                    [Neighborhood] nvarchar(100) NULL,
                    [AddressLine] nvarchar(500) NOT NULL,
                    [PostalCode] nvarchar(10) NULL,
                    [IsDefault] bit NOT NULL,
                    [Type] nvarchar(20) NOT NULL,
                    [IsDeleted] bit NOT NULL,
                    [DeletedDate] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_Addresses] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_Addresses_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
                );
                CREATE INDEX [IX_Addresses_UserId] ON [Addresses] ([UserId]);

                CREATE TABLE [Notifications] (
                    [Id] int NOT NULL IDENTITY,
                    [UserId] int NOT NULL,
                    [Title] nvarchar(200) NOT NULL,
                    [Message] nvarchar(1000) NOT NULL,
                    [Type] nvarchar(30) NOT NULL,
                    [IsRead] bit NOT NULL,
                    [ReadAt] datetime2 NULL,
                    [ActionUrl] nvarchar(500) NULL,
                    [Icon] nvarchar(100) NULL,
                    [RelatedEntityType] nvarchar(50) NULL,
                    [RelatedEntityId] int NULL,
                    [IsDeleted] bit NOT NULL,
                    [DeletedDate] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_Notifications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
                );
                CREATE INDEX [IX_Notifications_UserId] ON [Notifications] ([UserId]);

                CREATE TABLE [NotificationPreferences] (
                    [Id] int NOT NULL IDENTITY,
                    [UserId] int NOT NULL,
                    [EmailOrderUpdates] bit NOT NULL,
                    [EmailPaymentUpdates] bit NOT NULL,
                    [EmailShipmentUpdates] bit NOT NULL,
                    [EmailPromotions] bit NOT NULL,
                    [EmailNewsletter] bit NOT NULL,
                    [EmailPriceAlerts] bit NOT NULL,
                    [EmailStockAlerts] bit NOT NULL,
                    [PushOrderUpdates] bit NOT NULL,
                    [PushPaymentUpdates] bit NOT NULL,
                    [PushShipmentUpdates] bit NOT NULL,
                    [PushPromotions] bit NOT NULL,
                    [PushPriceAlerts] bit NOT NULL,
                    [PushStockAlerts] bit NOT NULL,
                    [SmsOrderUpdates] bit NOT NULL,
                    [SmsShipmentUpdates] bit NOT NULL,
                    [SmsPromotions] bit NOT NULL,
                    [IsDeleted] bit NOT NULL,
                    [DeletedDate] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_NotificationPreferences] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_NotificationPreferences_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
                );
                CREATE UNIQUE INDEX [IX_NotificationPreferences_UserId] ON [NotificationPreferences] ([UserId]);

                CREATE TABLE [OrderEvents] (
                    [Id] int NOT NULL IDENTITY,
                    [OrderId] int NOT NULL,
                    [Status] nvarchar(30) NOT NULL,
                    [Title] nvarchar(200) NOT NULL,
                    [Description] nvarchar(1000) NULL,
                    [UserId] int NULL,
                    [UserType] nvarchar(50) NULL,
                    [Metadata] nvarchar(2000) NULL,
                    [IsDeleted] bit NOT NULL,
                    [DeletedDate] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_OrderEvents] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_OrderEvents_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION
                );
                CREATE INDEX [IX_OrderEvents_OrderId] ON [OrderEvents] ([OrderId]);

                CREATE TABLE [Refunds] (
                    [Id] int NOT NULL IDENTITY,
                    [RefundNumber] nvarchar(20) NOT NULL,
                    [OrderId] int NOT NULL,
                    [SellerOrderId] int NULL,
                    [Status] nvarchar(20) NOT NULL,
                    [Type] nvarchar(20) NOT NULL,
                    [Reason] nvarchar(500) NOT NULL,
                    [Description] nvarchar(1000) NULL,
                    [Amount] decimal(18,2) NOT NULL,
                    [Currency] nvarchar(3) NOT NULL,
                    [Method] nvarchar(30) NOT NULL,
                    [ExternalReference] nvarchar(100) NULL,
                    [ProcessedByUserId] int NULL,
                    [ProcessedAt] datetime2 NULL,
                    [AdminNote] nvarchar(500) NULL,
                    [RejectionReason] nvarchar(500) NULL,
                    [IsDeleted] bit NOT NULL,
                    [DeletedDate] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_Refunds] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_Refunds_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_Refunds_SellerOrders_SellerOrderId] FOREIGN KEY ([SellerOrderId]) REFERENCES [SellerOrders] ([Id]) ON DELETE NO ACTION
                );
                CREATE INDEX [IX_Refunds_OrderId] ON [Refunds] ([OrderId]);
                CREATE UNIQUE INDEX [IX_Refunds_RefundNumber] ON [Refunds] ([RefundNumber]) WHERE [IsDeleted] = 0;
                CREATE INDEX [IX_Refunds_SellerOrderId] ON [Refunds] ([SellerOrderId]);

                CREATE TABLE [Invoices] (
                    [Id] int NOT NULL IDENTITY,
                    [InvoiceNumber] nvarchar(30) NOT NULL,
                    [OrderId] int NOT NULL,
                    [SellerOrderId] int NULL,
                    [Type] nvarchar(20) NOT NULL,
                    [Status] nvarchar(20) NOT NULL,
                    [BuyerName] nvarchar(200) NOT NULL,
                    [BuyerEmail] nvarchar(200) NULL,
                    [BuyerPhone] nvarchar(20) NULL,
                    [BuyerAddress] nvarchar(500) NULL,
                    [BuyerTaxNumber] nvarchar(20) NULL,
                    [SellerName] nvarchar(200) NULL,
                    [SellerAddress] nvarchar(500) NULL,
                    [SellerTaxNumber] nvarchar(20) NULL,
                    [SubTotal] decimal(18,2) NOT NULL,
                    [TaxAmount] decimal(18,2) NOT NULL,
                    [DiscountAmount] decimal(18,2) NOT NULL,
                    [TotalAmount] decimal(18,2) NOT NULL,
                    [Currency] nvarchar(3) NOT NULL,
                    [InvoiceDate] datetime2 NOT NULL,
                    [DueDate] datetime2 NULL,
                    [PaidDate] datetime2 NULL,
                    [PdfUrl] nvarchar(500) NULL,
                    [Notes] nvarchar(1000) NULL,
                    [IsDeleted] bit NOT NULL,
                    [DeletedDate] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_Invoices] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_Invoices_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_Invoices_SellerOrders_SellerOrderId] FOREIGN KEY ([SellerOrderId]) REFERENCES [SellerOrders] ([Id]) ON DELETE NO ACTION
                );
                CREATE UNIQUE INDEX [IX_Invoices_InvoiceNumber] ON [Invoices] ([InvoiceNumber]) WHERE [IsDeleted] = 0;
                CREATE INDEX [IX_Invoices_OrderId] ON [Invoices] ([OrderId]);
                CREATE INDEX [IX_Invoices_SellerOrderId] ON [Invoices] ([SellerOrderId]);

                CREATE TABLE [ProductImages] (
                    [Id] int NOT NULL IDENTITY,
                    [Url] nvarchar(500) NOT NULL,
                    [ThumbnailUrl] nvarchar(500) NULL,
                    [AltText] nvarchar(255) NULL,
                    [DisplayOrder] int NOT NULL,
                    [IsPrimary] bit NOT NULL,
                    [FileSizeBytes] bigint NOT NULL,
                    [ContentType] nvarchar(50) NULL,
                    [ProductId] int NOT NULL,
                    [IsDeleted] bit NOT NULL,
                    [DeletedDate] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_ProductImages] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_ProductImages_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION
                );
                CREATE INDEX [IX_ProductImages_ProductId] ON [ProductImages] ([ProductId]);

                CREATE TABLE [Regions] (
                    [Id] int NOT NULL IDENTITY,
                    [Name] nvarchar(100) NOT NULL,
                    [Code] nvarchar(10) NULL,
                    [Type] nvarchar(20) NOT NULL,
                    [ParentId] int NULL,
                    [IsActive] bit NOT NULL,
                    [ShippingZoneId] int NULL,
                    [DisplayOrder] int NOT NULL,
                    [IsDeleted] bit NOT NULL,
                    [DeletedDate] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_Regions] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_Regions_Regions_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [Regions] ([Id]) ON DELETE NO ACTION
                );
                CREATE INDEX [IX_Regions_ParentId] ON [Regions] ([ParentId]);
                CREATE INDEX [IX_Regions_Type_ParentId] ON [Regions] ([Type], [ParentId]);

                CREATE TABLE [SellerPayouts] (
                    [Id] int NOT NULL IDENTITY,
                    [PayoutNumber] nvarchar(30) NOT NULL,
                    [SellerId] int NOT NULL,
                    [PeriodStart] datetime2 NOT NULL,
                    [PeriodEnd] datetime2 NOT NULL,
                    [GrossAmount] decimal(18,2) NOT NULL,
                    [CommissionAmount] decimal(18,2) NOT NULL,
                    [RefundDeductions] decimal(18,2) NOT NULL,
                    [OtherDeductions] decimal(18,2) NOT NULL,
                    [NetAmount] decimal(18,2) NOT NULL,
                    [Currency] nvarchar(3) NOT NULL,
                    [Status] nvarchar(20) NOT NULL,
                    [OrderCount] int NOT NULL,
                    [ItemCount] int NOT NULL,
                    [BankName] nvarchar(100) NULL,
                    [IBAN] nvarchar(34) NULL,
                    [AccountHolderName] nvarchar(100) NULL,
                    [TransferReference] nvarchar(100) NULL,
                    [ApprovedAt] datetime2 NULL,
                    [ApprovedByUserId] int NULL,
                    [PaidAt] datetime2 NULL,
                    [PaidByUserId] int NULL,
                    [Notes] nvarchar(1000) NULL,
                    [IsDeleted] bit NOT NULL,
                    [DeletedDate] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_SellerPayouts] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_SellerPayouts_Users_SellerId] FOREIGN KEY ([SellerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
                );
                CREATE UNIQUE INDEX [IX_SellerPayouts_PayoutNumber] ON [SellerPayouts] ([PayoutNumber]) WHERE [IsDeleted] = 0;
                CREATE INDEX [IX_SellerPayouts_SellerId_Status] ON [SellerPayouts] ([SellerId], [Status]);

                CREATE TABLE [Affiliates] (
                    [Id] int NOT NULL IDENTITY,
                    [UserId] int NOT NULL,
                    [ReferralCode] nvarchar(20) NOT NULL,
                    [CommissionRate] decimal(5,2) NOT NULL,
                    [MinPayoutAmount] decimal(18,2) NOT NULL,
                    [TotalReferrals] int NOT NULL,
                    [SuccessfulReferrals] int NOT NULL,
                    [TotalEarnings] decimal(18,2) NOT NULL,
                    [PendingEarnings] decimal(18,2) NOT NULL,
                    [PaidEarnings] decimal(18,2) NOT NULL,
                    [Status] nvarchar(20) NOT NULL,
                    [ApprovedAt] datetime2 NULL,
                    [ApprovedByUserId] int NULL,
                    [BankName] nvarchar(100) NULL,
                    [IBAN] nvarchar(34) NULL,
                    [AccountHolderName] nvarchar(100) NULL,
                    [IsDeleted] bit NOT NULL,
                    [DeletedDate] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_Affiliates] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_Affiliates_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
                );
                CREATE UNIQUE INDEX [IX_Affiliates_ReferralCode] ON [Affiliates] ([ReferralCode]) WHERE [IsDeleted] = 0;
                CREATE UNIQUE INDEX [IX_Affiliates_UserId] ON [Affiliates] ([UserId]) WHERE [IsDeleted] = 0;

                CREATE TABLE [LoyaltyPoints] (
                    [Id] int NOT NULL IDENTITY,
                    [UserId] int NOT NULL,
                    [TotalPoints] int NOT NULL,
                    [AvailablePoints] int NOT NULL,
                    [SpentPoints] int NOT NULL,
                    [PendingPoints] int NOT NULL,
                    [Tier] nvarchar(20) NOT NULL,
                    [IsDeleted] bit NOT NULL,
                    [DeletedDate] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_LoyaltyPoints] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_LoyaltyPoints_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
                );
                CREATE UNIQUE INDEX [IX_LoyaltyPoints_UserId] ON [LoyaltyPoints] ([UserId]) WHERE [IsDeleted] = 0;

                CREATE TABLE [GiftCards] (
                    [Id] int NOT NULL IDENTITY,
                    [Code] nvarchar(20) NOT NULL,
                    [InitialBalance] decimal(18,2) NOT NULL,
                    [CurrentBalance] decimal(18,2) NOT NULL,
                    [Currency] nvarchar(3) NOT NULL,
                    [Status] nvarchar(20) NOT NULL,
                    [ExpiresAt] datetime2 NULL,
                    [PurchasedByUserId] int NULL,
                    [OrderId] int NULL,
                    [RecipientUserId] int NULL,
                    [RecipientEmail] nvarchar(200) NULL,
                    [RecipientName] nvarchar(100) NULL,
                    [PersonalMessage] nvarchar(500) NULL,
                    [IsSentToRecipient] bit NOT NULL,
                    [SentAt] datetime2 NULL,
                    [RedeemedAt] datetime2 NULL,
                    [RedeemedByUserId] int NULL,
                    [AdminNotes] nvarchar(500) NULL,
                    [IsDeleted] bit NOT NULL,
                    [DeletedDate] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_GiftCards] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_GiftCards_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_GiftCards_Users_PurchasedByUserId] FOREIGN KEY ([PurchasedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_GiftCards_Users_RecipientUserId] FOREIGN KEY ([RecipientUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
                );
                CREATE UNIQUE INDEX [IX_GiftCards_Code] ON [GiftCards] ([Code]) WHERE [IsDeleted] = 0;
                CREATE INDEX [IX_GiftCards_OrderId] ON [GiftCards] ([OrderId]);
                CREATE INDEX [IX_GiftCards_PurchasedByUserId] ON [GiftCards] ([PurchasedByUserId]);
                CREATE INDEX [IX_GiftCards_RecipientUserId] ON [GiftCards] ([RecipientUserId]);

                CREATE TABLE [Wishlists] (
                    [Id] int NOT NULL IDENTITY,
                    [UserId] int NOT NULL,
                    [Name] nvarchar(100) NOT NULL,
                    [IsDefault] bit NOT NULL,
                    [IsPublic] bit NOT NULL,
                    [IsDeleted] bit NOT NULL,
                    [DeletedDate] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_Wishlists] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_Wishlists_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
                );
                CREATE INDEX [IX_Wishlists_UserId] ON [Wishlists] ([UserId]);

                CREATE TABLE [ProductComparisons] (
                    [Id] int NOT NULL IDENTITY,
                    [UserId] int NOT NULL,
                    [Name] nvarchar(200) NULL,
                    [IsDeleted] bit NOT NULL,
                    [DeletedDate] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_ProductComparisons] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_ProductComparisons_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
                );
                CREATE INDEX [IX_ProductComparisons_UserId] ON [ProductComparisons] ([UserId]);

                CREATE TABLE [ProductQuestions] (
                    [Id] int NOT NULL IDENTITY,
                    [ProductId] int NOT NULL,
                    [AskedByUserId] int NOT NULL,
                    [QuestionText] nvarchar(1000) NOT NULL,
                    [Status] nvarchar(20) NOT NULL,
                    [UpvoteCount] int NOT NULL,
                    [IsDeleted] bit NOT NULL,
                    [DeletedDate] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_ProductQuestions] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_ProductQuestions_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_ProductQuestions_Users_AskedByUserId] FOREIGN KEY ([AskedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
                );
                CREATE INDEX [IX_ProductQuestions_AskedByUserId] ON [ProductQuestions] ([AskedByUserId]);
                CREATE INDEX [IX_ProductQuestions_ProductId_Status] ON [ProductQuestions] ([ProductId], [Status]);

                CREATE TABLE [RecentlyViewed] (
                    [Id] int NOT NULL IDENTITY,
                    [UserId] int NOT NULL,
                    [ProductId] int NOT NULL,
                    [ViewedAt] datetime2 NOT NULL,
                    [ViewCount] int NOT NULL,
                    [IsDeleted] bit NOT NULL,
                    [DeletedDate] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_RecentlyViewed] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_RecentlyViewed_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_RecentlyViewed_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
                );
                CREATE INDEX [IX_RecentlyViewed_ProductId] ON [RecentlyViewed] ([ProductId]);
                CREATE UNIQUE INDEX [IX_RecentlyViewed_UserId_ProductId] ON [RecentlyViewed] ([UserId], [ProductId]) WHERE [IsDeleted] = 0;
                CREATE INDEX [IX_RecentlyViewed_UserId_ViewedAt] ON [RecentlyViewed] ([UserId], [ViewedAt]);

                CREATE TABLE [Reviews] (
                    [Id] int NOT NULL IDENTITY,
                    [UserId] int NOT NULL,
                    [ProductId] int NULL,
                    [SellerId] int NULL,
                    [OrderId] int NULL,
                    [SellerOrderId] int NULL,
                    [Type] nvarchar(20) NOT NULL,
                    [Rating] int NOT NULL,
                    [Comment] nvarchar(2000) NULL,
                    [Pros] nvarchar(500) NULL,
                    [Cons] nvarchar(500) NULL,
                    [ImageUrls] nvarchar(2000) NULL,
                    [Status] nvarchar(20) NOT NULL,
                    [ModeratedByUserId] int NULL,
                    [ModeratedAt] datetime2 NULL,
                    [ModerationNote] nvarchar(500) NULL,
                    [HelpfulCount] int NOT NULL,
                    [NotHelpfulCount] int NOT NULL,
                    [ReportCount] int NOT NULL,
                    [IsVerifiedPurchase] bit NOT NULL,
                    [SellerResponse] nvarchar(1000) NULL,
                    [SellerRespondedAt] datetime2 NULL,
                    [IsDeleted] bit NOT NULL,
                    [DeletedDate] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_Reviews] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_Reviews_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_Reviews_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_Reviews_SellerOrders_SellerOrderId] FOREIGN KEY ([SellerOrderId]) REFERENCES [SellerOrders] ([Id]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_Reviews_Users_SellerId] FOREIGN KEY ([SellerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_Reviews_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
                );
                CREATE INDEX [IX_Reviews_OrderId] ON [Reviews] ([OrderId]);
                CREATE INDEX [IX_Reviews_ProductId_Status] ON [Reviews] ([ProductId], [Status]);
                CREATE INDEX [IX_Reviews_SellerId_Status] ON [Reviews] ([SellerId], [Status]);
                CREATE INDEX [IX_Reviews_SellerOrderId] ON [Reviews] ([SellerOrderId]);
                CREATE INDEX [IX_Reviews_UserId] ON [Reviews] ([UserId]);

                CREATE TABLE [SellerReviews] (
                    [Id] int NOT NULL IDENTITY,
                    [SellerId] int NOT NULL,
                    [UserId] int NOT NULL,
                    [OrderId] int NULL,
                    [Rating] int NOT NULL,
                    [Comment] nvarchar(1000) NULL,
                    [ShippingRating] int NULL,
                    [CommunicationRating] int NULL,
                    [PackagingRating] int NULL,
                    [Status] nvarchar(20) NOT NULL,
                    [SellerReply] nvarchar(1000) NULL,
                    [SellerReplyDate] datetime2 NULL,
                    [IsDeleted] bit NOT NULL,
                    [DeletedDate] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_SellerReviews] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_SellerReviews_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_SellerReviews_Users_SellerId] FOREIGN KEY ([SellerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_SellerReviews_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
                );
                CREATE INDEX [IX_SellerReviews_OrderId] ON [SellerReviews] ([OrderId]);
                CREATE INDEX [IX_SellerReviews_SellerId_Status] ON [SellerReviews] ([SellerId], [Status]);
                CREATE UNIQUE INDEX [IX_SellerReviews_UserId_SellerId_OrderId] ON [SellerReviews] ([UserId], [SellerId], [OrderId]) WHERE [IsDeleted] = 0 AND [OrderId] IS NOT NULL;

                CREATE TABLE [ProductAttributeValues] (
                    [Id] int NOT NULL IDENTITY,
                    [AttributeId] int NOT NULL,
                    [Value] nvarchar(100) NOT NULL,
                    [Code] nvarchar(50) NULL,
                    [ColorHex] nvarchar(7) NULL,
                    [ImageUrl] nvarchar(500) NULL,
                    [DisplayOrder] int NOT NULL,
                    [IsActive] bit NOT NULL,
                    [IsDeleted] bit NOT NULL,
                    [DeletedDate] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_ProductAttributeValues] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_ProductAttributeValues_ProductAttributes_AttributeId] FOREIGN KEY ([AttributeId]) REFERENCES [ProductAttributes] ([Id]) ON DELETE NO ACTION
                );
                CREATE INDEX [IX_ProductAttributeValues_AttributeId] ON [ProductAttributeValues] ([AttributeId]);

                CREATE TABLE [LoyaltyTransactions] (
                    [Id] int NOT NULL IDENTITY,
                    [LoyaltyPointId] int NOT NULL,
                    [Points] int NOT NULL,
                    [OrderId] int NULL,
                    [Type] nvarchar(30) NOT NULL,
                    [Description] nvarchar(500) NULL,
                    [IsDeleted] bit NOT NULL,
                    [DeletedDate] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_LoyaltyTransactions] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_LoyaltyTransactions_LoyaltyPoints_LoyaltyPointId] FOREIGN KEY ([LoyaltyPointId]) REFERENCES [LoyaltyPoints] ([Id]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_LoyaltyTransactions_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION
                );
                CREATE INDEX [IX_LoyaltyTransactions_LoyaltyPointId_CreatedAt] ON [LoyaltyTransactions] ([LoyaltyPointId], [CreatedAt]);
                CREATE INDEX [IX_LoyaltyTransactions_OrderId] ON [LoyaltyTransactions] ([OrderId]);

                CREATE TABLE [GiftCardUsages] (
                    [Id] int NOT NULL IDENTITY,
                    [GiftCardId] int NOT NULL,
                    [OrderId] int NULL,
                    [UserId] int NOT NULL,
                    [Amount] decimal(18,2) NOT NULL,
                    [BalanceAfter] decimal(18,2) NOT NULL,
                    [Type] nvarchar(20) NOT NULL,
                    [Description] nvarchar(500) NULL,
                    [IsDeleted] bit NOT NULL,
                    [DeletedDate] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_GiftCardUsages] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_GiftCardUsages_GiftCards_GiftCardId] FOREIGN KEY ([GiftCardId]) REFERENCES [GiftCards] ([Id]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_GiftCardUsages_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_GiftCardUsages_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
                );
                CREATE INDEX [IX_GiftCardUsages_GiftCardId] ON [GiftCardUsages] ([GiftCardId]);
                CREATE INDEX [IX_GiftCardUsages_OrderId] ON [GiftCardUsages] ([OrderId]);
                CREATE INDEX [IX_GiftCardUsages_UserId] ON [GiftCardUsages] ([UserId]);

                CREATE TABLE [WishlistItems] (
                    [Id] int NOT NULL IDENTITY,
                    [WishlistId] int NOT NULL,
                    [ProductId] int NOT NULL,
                    [ListingId] int NULL,
                    [Note] nvarchar(500) NULL,
                    [NotifyOnPriceDrop] bit NOT NULL,
                    [PriceAtAdd] decimal(18,2) NULL,
                    [IsDeleted] bit NOT NULL,
                    [DeletedDate] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_WishlistItems] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_WishlistItems_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_WishlistItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_WishlistItems_Wishlists_WishlistId] FOREIGN KEY ([WishlistId]) REFERENCES [Wishlists] ([Id]) ON DELETE NO ACTION
                );
                CREATE INDEX [IX_WishlistItems_ListingId] ON [WishlistItems] ([ListingId]);
                CREATE INDEX [IX_WishlistItems_ProductId] ON [WishlistItems] ([ProductId]);
                CREATE UNIQUE INDEX [IX_WishlistItems_WishlistId_ProductId] ON [WishlistItems] ([WishlistId], [ProductId]) WHERE [IsDeleted] = 0;

                CREATE TABLE [ProductComparisonItems] (
                    [Id] int NOT NULL IDENTITY,
                    [ComparisonId] int NOT NULL,
                    [ProductId] int NOT NULL,
                    [DisplayOrder] int NOT NULL,
                    [IsDeleted] bit NOT NULL,
                    [DeletedDate] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_ProductComparisonItems] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_ProductComparisonItems_ProductComparisons_ComparisonId] FOREIGN KEY ([ComparisonId]) REFERENCES [ProductComparisons] ([Id]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_ProductComparisonItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION
                );
                CREATE UNIQUE INDEX [IX_ProductComparisonItems_ComparisonId_ProductId] ON [ProductComparisonItems] ([ComparisonId], [ProductId]) WHERE [IsDeleted] = 0;
                CREATE INDEX [IX_ProductComparisonItems_ProductId] ON [ProductComparisonItems] ([ProductId]);

                CREATE TABLE [ProductAnswers] (
                    [Id] int NOT NULL IDENTITY,
                    [QuestionId] int NOT NULL,
                    [AnsweredByUserId] int NOT NULL,
                    [AnswerText] nvarchar(2000) NOT NULL,
                    [IsSellerAnswer] bit NOT NULL,
                    [IsAccepted] bit NOT NULL,
                    [UpvoteCount] int NOT NULL,
                    [IsDeleted] bit NOT NULL,
                    [DeletedDate] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_ProductAnswers] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_ProductAnswers_ProductQuestions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [ProductQuestions] ([Id]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_ProductAnswers_Users_AnsweredByUserId] FOREIGN KEY ([AnsweredByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
                );
                CREATE INDEX [IX_ProductAnswers_AnsweredByUserId] ON [ProductAnswers] ([AnsweredByUserId]);
                CREATE INDEX [IX_ProductAnswers_QuestionId] ON [ProductAnswers] ([QuestionId]);

                CREATE TABLE [ReviewVotes] (
                    [Id] int NOT NULL IDENTITY,
                    [ReviewId] int NOT NULL,
                    [UserId] int NOT NULL,
                    [IsHelpful] bit NOT NULL,
                    [IsDeleted] bit NOT NULL,
                    [DeletedDate] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_ReviewVotes] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_ReviewVotes_Reviews_ReviewId] FOREIGN KEY ([ReviewId]) REFERENCES [Reviews] ([Id]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_ReviewVotes_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
                );
                CREATE UNIQUE INDEX [IX_ReviewVotes_ReviewId_UserId] ON [ReviewVotes] ([ReviewId], [UserId]) WHERE [IsDeleted] = 0;
                CREATE INDEX [IX_ReviewVotes_UserId] ON [ReviewVotes] ([UserId]);

                CREATE TABLE [ReviewReports] (
                    [Id] int NOT NULL IDENTITY,
                    [ReviewId] int NOT NULL,
                    [UserId] int NOT NULL,
                    [Reason] nvarchar(30) NOT NULL,
                    [Description] nvarchar(500) NULL,
                    [IsResolved] bit NOT NULL,
                    [ResolvedByUserId] int NULL,
                    [ResolvedAt] datetime2 NULL,
                    [IsDeleted] bit NOT NULL,
                    [DeletedDate] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_ReviewReports] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_ReviewReports_Reviews_ReviewId] FOREIGN KEY ([ReviewId]) REFERENCES [Reviews] ([Id]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_ReviewReports_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
                );
                CREATE INDEX [IX_ReviewReports_ReviewId] ON [ReviewReports] ([ReviewId]);
                CREATE INDEX [IX_ReviewReports_UserId] ON [ReviewReports] ([UserId]);

                CREATE TABLE [SellerPayoutItems] (
                    [Id] int NOT NULL IDENTITY,
                    [PayoutId] int NOT NULL,
                    [SellerOrderId] int NOT NULL,
                    [OrderAmount] decimal(18,2) NOT NULL,
                    [CommissionAmount] decimal(18,2) NOT NULL,
                    [NetAmount] decimal(18,2) NOT NULL,
                    [IsDeleted] bit NOT NULL,
                    [DeletedDate] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_SellerPayoutItems] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_SellerPayoutItems_SellerOrders_SellerOrderId] FOREIGN KEY ([SellerOrderId]) REFERENCES [SellerOrders] ([Id]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_SellerPayoutItems_SellerPayouts_PayoutId] FOREIGN KEY ([PayoutId]) REFERENCES [SellerPayouts] ([Id]) ON DELETE NO ACTION
                );
                CREATE INDEX [IX_SellerPayoutItems_PayoutId] ON [SellerPayoutItems] ([PayoutId]);
                CREATE INDEX [IX_SellerPayoutItems_SellerOrderId] ON [SellerPayoutItems] ([SellerOrderId]);

                CREATE TABLE [AffiliateReferrals] (
                    [Id] int NOT NULL IDENTITY,
                    [AffiliateId] int NOT NULL,
                    [ReferredUserId] int NOT NULL,
                    [OrderId] int NULL,
                    [OrderAmount] decimal(18,2) NOT NULL,
                    [CommissionAmount] decimal(18,2) NOT NULL,
                    [Status] nvarchar(20) NOT NULL,
                    [ReferralUrl] nvarchar(500) NULL,
                    [UtmSource] nvarchar(100) NULL,
                    [UtmMedium] nvarchar(100) NULL,
                    [UtmCampaign] nvarchar(100) NULL,
                    [IsDeleted] bit NOT NULL,
                    [DeletedDate] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_AffiliateReferrals] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_AffiliateReferrals_Affiliates_AffiliateId] FOREIGN KEY ([AffiliateId]) REFERENCES [Affiliates] ([Id]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_AffiliateReferrals_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_AffiliateReferrals_Users_ReferredUserId] FOREIGN KEY ([ReferredUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
                );
                CREATE INDEX [IX_AffiliateReferrals_AffiliateId] ON [AffiliateReferrals] ([AffiliateId]);
                CREATE INDEX [IX_AffiliateReferrals_OrderId] ON [AffiliateReferrals] ([OrderId]);
                CREATE INDEX [IX_AffiliateReferrals_ReferredUserId] ON [AffiliateReferrals] ([ReferredUserId]);

                CREATE TABLE [AffiliatePayouts] (
                    [Id] int NOT NULL IDENTITY,
                    [AffiliateId] int NOT NULL,
                    [PayoutNumber] nvarchar(30) NOT NULL,
                    [Amount] decimal(18,2) NOT NULL,
                    [Currency] nvarchar(3) NOT NULL,
                    [Status] nvarchar(20) NOT NULL,
                    [TransferReference] nvarchar(100) NULL,
                    [PaidAt] datetime2 NULL,
                    [PaidByUserId] int NULL,
                    [Notes] nvarchar(500) NULL,
                    [IsDeleted] bit NOT NULL,
                    [DeletedDate] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_AffiliatePayouts] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_AffiliatePayouts_Affiliates_AffiliateId] FOREIGN KEY ([AffiliateId]) REFERENCES [Affiliates] ([Id]) ON DELETE NO ACTION
                );
                CREATE INDEX [IX_AffiliatePayouts_AffiliateId] ON [AffiliatePayouts] ([AffiliateId]);
                CREATE UNIQUE INDEX [IX_AffiliatePayouts_PayoutNumber] ON [AffiliatePayouts] ([PayoutNumber]) WHERE [IsDeleted] = 0;

                CREATE TABLE [ProductAttributeMappings] (
                    [Id] int NOT NULL IDENTITY,
                    [ProductId] int NOT NULL,
                    [AttributeId] int NOT NULL,
                    [AttributeValueId] int NOT NULL,
                    [IsDeleted] bit NOT NULL,
                    [DeletedDate] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_ProductAttributeMappings] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_ProductAttributeMappings_ProductAttributeValues_AttributeValueId] FOREIGN KEY ([AttributeValueId]) REFERENCES [ProductAttributeValues] ([Id]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_ProductAttributeMappings_ProductAttributes_AttributeId] FOREIGN KEY ([AttributeId]) REFERENCES [ProductAttributes] ([Id]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_ProductAttributeMappings_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION
                );
                CREATE INDEX [IX_ProductAttributeMappings_AttributeId] ON [ProductAttributeMappings] ([AttributeId]);
                CREATE INDEX [IX_ProductAttributeMappings_AttributeValueId] ON [ProductAttributeMappings] ([AttributeValueId]);
                CREATE UNIQUE INDEX [IX_ProductAttributeMappings_ProductId_AttributeId_AttributeValueId] ON [ProductAttributeMappings] ([ProductId], [AttributeId], [AttributeValueId]) WHERE [IsDeleted] = 0;

                ALTER TABLE [ReturnShipments] ADD CONSTRAINT [FK_ReturnShipments_Refunds_RefundId]
                    FOREIGN KEY ([RefundId]) REFERENCES [Refunds] ([Id]) ON DELETE NO ACTION;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS [ProductAttributeMappings];
                DROP TABLE IF EXISTS [AffiliatePayouts];
                DROP TABLE IF EXISTS [AffiliateReferrals];
                DROP TABLE IF EXISTS [SellerPayoutItems];
                DROP TABLE IF EXISTS [ReviewReports];
                DROP TABLE IF EXISTS [ReviewVotes];
                DROP TABLE IF EXISTS [ProductAnswers];
                DROP TABLE IF EXISTS [ProductComparisonItems];
                DROP TABLE IF EXISTS [WishlistItems];
                DROP TABLE IF EXISTS [GiftCardUsages];
                DROP TABLE IF EXISTS [LoyaltyTransactions];
                DROP TABLE IF EXISTS [ProductAttributeValues];
                DROP TABLE IF EXISTS [SellerReviews];
                DROP TABLE IF EXISTS [Reviews];
                DROP TABLE IF EXISTS [ProductQuestions];
                DROP TABLE IF EXISTS [RecentlyViewed];
                DROP TABLE IF EXISTS [ProductComparisons];
                DROP TABLE IF EXISTS [Wishlists];
                DROP TABLE IF EXISTS [GiftCards];
                DROP TABLE IF EXISTS [LoyaltyPoints];
                DROP TABLE IF EXISTS [Affiliates];
                DROP TABLE IF EXISTS [SellerPayouts];
                DROP TABLE IF EXISTS [Regions];
                DROP TABLE IF EXISTS [ProductImages];
                DROP TABLE IF EXISTS [Invoices];
                DROP TABLE IF EXISTS [Refunds];
                DROP TABLE IF EXISTS [OrderEvents];
                DROP TABLE IF EXISTS [NotificationPreferences];
                DROP TABLE IF EXISTS [Notifications];
                DROP TABLE IF EXISTS [Addresses];
                DROP TABLE IF EXISTS [HomeWidgets];
                DROP TABLE IF EXISTS [ProductAttributes];
                DROP TABLE IF EXISTS [ShippingZones];
                DROP TABLE IF EXISTS [EmailTemplates];
                DROP TABLE IF EXISTS [Brands];
                DROP TABLE IF EXISTS [TaxRates];
            ");
        }
    }
}
