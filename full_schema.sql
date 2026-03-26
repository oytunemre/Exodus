CREATE TABLE [Banners] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(500) NULL,
    [ImageUrl] nvarchar(500) NOT NULL,
    [MobileImageUrl] nvarchar(500) NULL,
    [TargetUrl] nvarchar(500) NULL,
    [Position] nvarchar(30) NOT NULL,
    [DisplayOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    [StartDate] datetime2 NULL,
    [EndDate] datetime2 NULL,
    [ClickCount] int NOT NULL,
    [ViewCount] int NOT NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedDate] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Banners] PRIMARY KEY ([Id])
);
GO


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
GO


CREATE TABLE [Categories] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Slug] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NULL,
    [ImageUrl] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    [DisplayOrder] int NOT NULL,
    [ParentCategoryId] int NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedDate] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Categories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Categories_Categories_ParentCategoryId] FOREIGN KEY ([ParentCategoryId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION
);
GO


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
GO


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
GO


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
GO


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
GO


CREATE TABLE [ShippingCarriers] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Code] nvarchar(50) NULL,
    [LogoUrl] nvarchar(500) NULL,
    [TrackingUrlTemplate] nvarchar(500) NULL,
    [Website] nvarchar(500) NULL,
    [Phone] nvarchar(20) NULL,
    [IsActive] bit NOT NULL,
    [SupportsApi] bit NOT NULL,
    [ApiEndpoint] nvarchar(500) NULL,
    [ApiKey] nvarchar(200) NULL,
    [DefaultRate] decimal(18,2) NULL,
    [FreeShippingThreshold] decimal(18,2) NULL,
    [DisplayOrder] int NOT NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedDate] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ShippingCarriers] PRIMARY KEY ([Id])
);
GO


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
GO


CREATE TABLE [SiteSettings] (
    [Id] int NOT NULL IDENTITY,
    [Key] nvarchar(100) NOT NULL,
    [Value] nvarchar(2000) NOT NULL,
    [Description] nvarchar(500) NULL,
    [Category] nvarchar(30) NOT NULL,
    [IsPublic] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedDate] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_SiteSettings] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [StaticPages] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(200) NOT NULL,
    [Slug] nvarchar(200) NOT NULL,
    [Content] nvarchar(max) NOT NULL,
    [MetaTitle] nvarchar(200) NULL,
    [MetaDescription] nvarchar(500) NULL,
    [MetaKeywords] nvarchar(500) NULL,
    [IsPublished] bit NOT NULL,
    [ShowInFooter] bit NOT NULL,
    [ShowInHeader] bit NOT NULL,
    [DisplayOrder] int NOT NULL,
    [PageType] int NOT NULL,
    [PublishedAt] datetime2 NULL,
    [LastEditedByUserId] int NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedDate] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_StaticPages] PRIMARY KEY ([Id])
);
GO


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
GO


CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Email] nvarchar(max) NOT NULL,
    [Password] nvarchar(max) NOT NULL,
    [Username] nvarchar(max) NOT NULL,
    [Role] int NOT NULL,
    [Phone] nvarchar(20) NULL,
    [AvatarUrl] nvarchar(500) NULL,
    [LastLoginAt] datetime2 NULL,
    [EmailVerified] bit NOT NULL,
    [EmailVerificationToken] nvarchar(max) NULL,
    [EmailVerificationTokenExpiresAt] datetime2 NULL,
    [PasswordResetToken] nvarchar(max) NULL,
    [PasswordResetTokenExpiresAt] datetime2 NULL,
    [FailedLoginAttempts] int NOT NULL,
    [LockoutEndTime] datetime2 NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [TwoFactorSecretKey] nvarchar(max) NULL,
    [TwoFactorBackupCodes] nvarchar(max) NULL,
    [TwoFactorEnabledAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedDate] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Products] (
    [Id] int NOT NULL IDENTITY,
    [ProductName] nvarchar(200) NOT NULL,
    [ProductDescription] nvarchar(2000) NOT NULL,
    [Brand] nvarchar(100) NULL,
    [Manufacturer] nvarchar(100) NULL,
    [CategoryId] int NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedDate] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Products_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id])
);
GO


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
    CONSTRAINT [FK_ProductAttributeValues_ProductAttributes_AttributeId] FOREIGN KEY ([AttributeId]) REFERENCES [ProductAttributes] ([Id]) ON DELETE CASCADE
);
GO


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
    CONSTRAINT [FK_Addresses_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO


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
    CONSTRAINT [FK_Affiliates_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AuditLogs] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NULL,
    [Action] nvarchar(100) NOT NULL,
    [EntityType] nvarchar(100) NULL,
    [EntityId] int NULL,
    [OldValues] nvarchar(500) NULL,
    [NewValues] nvarchar(500) NULL,
    [IpAddress] nvarchar(45) NULL,
    [UserAgent] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AuditLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
);
GO


CREATE TABLE [Campaigns] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(200) NOT NULL,
    [Description] nvarchar(1000) NULL,
    [Type] nvarchar(30) NOT NULL,
    [SellerId] int NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [IsActive] bit NOT NULL,
    [MaxUsageCount] int NULL,
    [MaxUsagePerUser] int NULL,
    [CurrentUsageCount] int NOT NULL,
    [MinimumOrderAmount] decimal(18,2) NULL,
    [MinimumQuantity] int NULL,
    [DiscountPercentage] decimal(5,2) NULL,
    [DiscountAmount] decimal(18,2) NULL,
    [MaxDiscountAmount] decimal(18,2) NULL,
    [BuyQuantity] int NULL,
    [GetQuantity] int NULL,
    [CouponCode] nvarchar(50) NULL,
    [RequiresCouponCode] bit NOT NULL,
    [Scope] nvarchar(30) NOT NULL,
    [Priority] int NOT NULL,
    [IsStackable] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedDate] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Campaigns] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Campaigns_Users_SellerId] FOREIGN KEY ([SellerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Carts] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedDate] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Carts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Carts_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO


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
    CONSTRAINT [FK_LoyaltyPoints_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO


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
    CONSTRAINT [FK_NotificationPreferences_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO


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
    CONSTRAINT [FK_Notifications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Orders] (
    [Id] int NOT NULL IDENTITY,
    [OrderNumber] nvarchar(30) NOT NULL,
    [BuyerId] int NOT NULL,
    [Status] nvarchar(30) NOT NULL,
    [SubTotal] decimal(18,2) NOT NULL,
    [ShippingCost] decimal(18,2) NOT NULL,
    [TaxAmount] decimal(18,2) NOT NULL,
    [DiscountAmount] decimal(18,2) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [Currency] nvarchar(3) NOT NULL,
    [ShippingAddressId] int NULL,
    [ShippingAddressSnapshot] nvarchar(500) NULL,
    [BillingAddressId] int NULL,
    [BillingAddressSnapshot] nvarchar(500) NULL,
    [CustomerNote] nvarchar(1000) NULL,
    [AdminNote] nvarchar(1000) NULL,
    [CancellationReason] int NULL,
    [CancellationNote] nvarchar(500) NULL,
    [CancelledAt] datetime2 NULL,
    [PaidAt] datetime2 NULL,
    [ShippedAt] datetime2 NULL,
    [DeliveredAt] datetime2 NULL,
    [CompletedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedDate] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Orders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Orders_Users_BuyerId] FOREIGN KEY ([BuyerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ProductComparisons] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [Name] nvarchar(200) NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedDate] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ProductComparisons] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProductComparisons_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [RefreshTokens] (
    [Id] int NOT NULL IDENTITY,
    [Token] nvarchar(max) NOT NULL,
    [ExpiresAt] datetime2 NOT NULL,
    [IsRevoked] bit NOT NULL,
    [RevokedAt] datetime2 NULL,
    [UserId] int NOT NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedDate] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO


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
GO


CREATE TABLE [SellerProfiles] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [BusinessName] nvarchar(200) NULL,
    [TaxNumber] nvarchar(100) NULL,
    [BusinessAddress] nvarchar(500) NULL,
    [BusinessPhone] nvarchar(20) NULL,
    [VerificationStatus] nvarchar(20) NOT NULL,
    [VerifiedAt] datetime2 NULL,
    [VerifiedByAdminId] int NULL,
    [RejectionReason] nvarchar(500) NULL,
    [TaxDocumentUrl] nvarchar(500) NULL,
    [IdentityDocumentUrl] nvarchar(500) NULL,
    [SignatureCircularUrl] nvarchar(500) NULL,
    [CustomCommissionRate] decimal(5,2) NULL,
    [BankName] nvarchar(100) NULL,
    [IBAN] nvarchar(34) NULL,
    [AccountHolderName] nvarchar(100) NULL,
    [Rating] decimal(3,2) NOT NULL,
    [RatingCount] int NOT NULL,
    [TotalSales] int NOT NULL,
    [TotalRevenue] decimal(18,2) NOT NULL,
    [WarningCount] int NOT NULL,
    [SuspendedUntil] datetime2 NULL,
    [SuspensionReason] nvarchar(500) NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedDate] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_SellerProfiles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SellerProfiles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [SellerShippingSettings] (
    [Id] int NOT NULL IDENTITY,
    [SellerId] int NOT NULL,
    [FreeShippingThreshold] decimal(18,2) NULL,
    [DefaultShippingCost] decimal(18,2) NOT NULL,
    [PreferredCarrierId] int NULL,
    [UsesMultipleCarriers] bit NOT NULL,
    [OffersStorePickup] bit NOT NULL,
    [PickupAddress] nvarchar(500) NULL,
    [OffersSameDayShipping] bit NOT NULL,
    [SameDayShippingCost] decimal(18,2) NULL,
    [EstimatedShippingDays] int NOT NULL,
    [AcceptsReturns] bit NOT NULL,
    [ReturnDaysLimit] int NOT NULL,
    [OffersFreeReturns] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedDate] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_SellerShippingSettings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SellerShippingSettings_ShippingCarriers_PreferredCarrierId] FOREIGN KEY ([PreferredCarrierId]) REFERENCES [ShippingCarriers] ([Id]),
    CONSTRAINT [FK_SellerShippingSettings_Users_SellerId] FOREIGN KEY ([SellerId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO


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
    CONSTRAINT [FK_Wishlists_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Listings] (
    [Id] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [SellerId] int NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [StockQuantity] int NOT NULL,
    [LowStockThreshold] int NOT NULL,
    [TrackInventory] bit NOT NULL,
    [StockStatus] nvarchar(30) NOT NULL,
    [SKU] nvarchar(50) NULL,
    [Condition] nvarchar(30) NOT NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedDate] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Listings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Listings_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]),
    CONSTRAINT [FK_Listings_Users_SellerId] FOREIGN KEY ([SellerId]) REFERENCES [Users] ([Id])
);
GO


CREATE TABLE [ProductBarcodes] (
    [Id] int NOT NULL IDENTITY,
    [Barcode] nvarchar(100) NOT NULL,
    [ProductId] int NOT NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedDate] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ProductBarcodes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProductBarcodes_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
);
GO


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
    CONSTRAINT [FK_ProductImages_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
);
GO


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
    CONSTRAINT [FK_ProductQuestions_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ProductQuestions_Users_AskedByUserId] FOREIGN KEY ([AskedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


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
    CONSTRAINT [FK_RecentlyViewed_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RecentlyViewed_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO


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
    CONSTRAINT [FK_ProductAttributeMappings_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
);
GO


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
    CONSTRAINT [FK_AffiliatePayouts_Affiliates_AffiliateId] FOREIGN KEY ([AffiliateId]) REFERENCES [Affiliates] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [CampaignCategories] (
    [Id] int NOT NULL IDENTITY,
    [CampaignId] int NOT NULL,
    [CategoryId] int NOT NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedDate] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_CampaignCategories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CampaignCategories_Campaigns_CampaignId] FOREIGN KEY ([CampaignId]) REFERENCES [Campaigns] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CampaignCategories_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION
);
GO


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
    CONSTRAINT [FK_AffiliateReferrals_Affiliates_AffiliateId] FOREIGN KEY ([AffiliateId]) REFERENCES [Affiliates] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AffiliateReferrals_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]),
    CONSTRAINT [FK_AffiliateReferrals_Users_ReferredUserId] FOREIGN KEY ([ReferredUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [CampaignUsages] (
    [Id] int NOT NULL IDENTITY,
    [CampaignId] int NOT NULL,
    [UserId] int NOT NULL,
    [OrderId] int NULL,
    [DiscountApplied] decimal(18,2) NOT NULL,
    [UsedAt] datetime2 NOT NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedDate] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_CampaignUsages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CampaignUsages_Campaigns_CampaignId] FOREIGN KEY ([CampaignId]) REFERENCES [Campaigns] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CampaignUsages_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]),
    CONSTRAINT [FK_CampaignUsages_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


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
    CONSTRAINT [FK_GiftCards_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]),
    CONSTRAINT [FK_GiftCards_Users_PurchasedByUserId] FOREIGN KEY ([PurchasedByUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_GiftCards_Users_RecipientUserId] FOREIGN KEY ([RecipientUserId]) REFERENCES [Users] ([Id])
);
GO


CREATE TABLE [LoyaltyTransactions] (
    [Id] int NOT NULL IDENTITY,
    [LoyaltyPointId] int NOT NULL,
    [Points] int NOT NULL,
    [Type] nvarchar(20) NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [OrderId] int NULL,
    [ReferenceCode] nvarchar(50) NULL,
    [ExpiresAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedDate] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_LoyaltyTransactions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LoyaltyTransactions_LoyaltyPoints_LoyaltyPointId] FOREIGN KEY ([LoyaltyPointId]) REFERENCES [LoyaltyPoints] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LoyaltyTransactions_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id])
);
GO


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
    CONSTRAINT [FK_OrderEvents_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [PaymentIntents] (
    [Id] int NOT NULL IDENTITY,
    [OrderId] int NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [RefundedAmount] decimal(18,2) NOT NULL,
    [Currency] nvarchar(3) NOT NULL,
    [Method] int NOT NULL,
    [Status] int NOT NULL,
    [Provider] nvarchar(50) NOT NULL,
    [ExternalReference] nvarchar(100) NULL,
    [FailureReason] nvarchar(500) NULL,
    [CardLast4] nvarchar(20) NULL,
    [CardBrand] nvarchar(20) NULL,
    [AuthorizedAt] datetime2 NULL,
    [CapturedAt] datetime2 NULL,
    [FailedAt] datetime2 NULL,
    [RefundedAt] datetime2 NULL,
    [ExpiresAt] datetime2 NULL,
    [Requires3DSecure] bit NOT NULL,
    [ThreeDSecureUrl] nvarchar(500) NULL,
    [InstallmentCount] int NULL,
    [InstallmentAmount] decimal(18,2) NULL,
    [Metadata] nvarchar(2000) NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedDate] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_PaymentIntents] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PaymentIntents_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [SellerOrders] (
    [Id] int NOT NULL IDENTITY,
    [OrderId] int NOT NULL,
    [SellerId] int NOT NULL,
    [Status] nvarchar(30) NOT NULL,
    [SubTotal] decimal(18,2) NOT NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedDate] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_SellerOrders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SellerOrders_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_SellerOrders_Users_SellerId] FOREIGN KEY ([SellerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


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
    CONSTRAINT [FK_SellerReviews_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]),
    CONSTRAINT [FK_SellerReviews_Users_SellerId] FOREIGN KEY ([SellerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SellerReviews_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


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
    CONSTRAINT [FK_ProductComparisonItems_ProductComparisons_ComparisonId] FOREIGN KEY ([ComparisonId]) REFERENCES [ProductComparisons] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ProductComparisonItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [CampaignProducts] (
    [Id] int NOT NULL IDENTITY,
    [CampaignId] int NOT NULL,
    [ProductId] int NULL,
    [ListingId] int NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedDate] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_CampaignProducts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CampaignProducts_Campaigns_CampaignId] FOREIGN KEY ([CampaignId]) REFERENCES [Campaigns] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CampaignProducts_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]),
    CONSTRAINT [FK_CampaignProducts_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id])
);
GO


CREATE TABLE [CartItems] (
    [Id] int NOT NULL IDENTITY,
    [CartId] int NOT NULL,
    [Quantity] int NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [ListingId] int NOT NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedDate] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_CartItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CartItems_Carts_CartId] FOREIGN KEY ([CartId]) REFERENCES [Carts] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CartItems_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE NO ACTION
);
GO


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
    CONSTRAINT [FK_WishlistItems_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]),
    CONSTRAINT [FK_WishlistItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_WishlistItems_Wishlists_WishlistId] FOREIGN KEY ([WishlistId]) REFERENCES [Wishlists] ([Id]) ON DELETE CASCADE
);
GO


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
    CONSTRAINT [FK_ProductAnswers_ProductQuestions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [ProductQuestions] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ProductAnswers_Users_AnsweredByUserId] FOREIGN KEY ([AnsweredByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


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
    CONSTRAINT [FK_GiftCardUsages_GiftCards_GiftCardId] FOREIGN KEY ([GiftCardId]) REFERENCES [GiftCards] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_GiftCardUsages_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]),
    CONSTRAINT [FK_GiftCardUsages_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
);
GO


CREATE TABLE [PaymentEvents] (
    [Id] int NOT NULL IDENTITY,
    [PaymentIntentId] int NOT NULL,
    [Status] int NOT NULL,
    [EventType] nvarchar(100) NOT NULL,
    [PayloadJson] nvarchar(4000) NULL,
    [Source] nvarchar(50) NULL,
    [IpAddress] nvarchar(50) NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedDate] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_PaymentEvents] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PaymentEvents_PaymentIntents_PaymentIntentId] FOREIGN KEY ([PaymentIntentId]) REFERENCES [PaymentIntents] ([Id]) ON DELETE CASCADE
);
GO


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
GO


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
GO


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
    CONSTRAINT [FK_Reviews_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]),
    CONSTRAINT [FK_Reviews_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Reviews_SellerOrders_SellerOrderId] FOREIGN KEY ([SellerOrderId]) REFERENCES [SellerOrders] ([Id]),
    CONSTRAINT [FK_Reviews_Users_SellerId] FOREIGN KEY ([SellerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Reviews_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [SellerOrderItems] (
    [Id] int NOT NULL IDENTITY,
    [SellerOrderId] int NOT NULL,
    [ListingId] int NOT NULL,
    [ProductId] int NOT NULL,
    [ProductName] nvarchar(max) NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [Quantity] int NOT NULL,
    [LineTotal] decimal(18,2) NOT NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedDate] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_SellerOrderItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SellerOrderItems_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SellerOrderItems_SellerOrders_SellerOrderId] FOREIGN KEY ([SellerOrderId]) REFERENCES [SellerOrders] ([Id]) ON DELETE CASCADE
);
GO


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
    CONSTRAINT [FK_SellerPayoutItems_SellerPayouts_PayoutId] FOREIGN KEY ([PayoutId]) REFERENCES [SellerPayouts] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Shipments] (
    [Id] int NOT NULL IDENTITY,
    [SellerOrderId] int NOT NULL,
    [Carrier] nvarchar(50) NOT NULL,
    [TrackingNumber] nvarchar(80) NULL,
    [Status] int NOT NULL,
    [ShippedAt] datetime2 NULL,
    [DeliveredAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedDate] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Shipments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Shipments_SellerOrders_SellerOrderId] FOREIGN KEY ([SellerOrderId]) REFERENCES [SellerOrders] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [SupportTickets] (
    [Id] int NOT NULL IDENTITY,
    [TicketNumber] nvarchar(20) NOT NULL,
    [UserId] int NOT NULL,
    [OrderId] int NULL,
    [SellerOrderId] int NULL,
    [Subject] nvarchar(200) NOT NULL,
    [Category] nvarchar(30) NOT NULL,
    [Priority] nvarchar(20) NOT NULL,
    [Status] nvarchar(30) NOT NULL,
    [AssignedToId] int NULL,
    [FirstResponseAt] datetime2 NULL,
    [ResolvedAt] datetime2 NULL,
    [ClosedAt] datetime2 NULL,
    [SatisfactionRating] int NULL,
    [SatisfactionComment] nvarchar(500) NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedDate] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_SupportTickets] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SupportTickets_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]),
    CONSTRAINT [FK_SupportTickets_SellerOrders_SellerOrderId] FOREIGN KEY ([SellerOrderId]) REFERENCES [SellerOrders] ([Id]),
    CONSTRAINT [FK_SupportTickets_Users_AssignedToId] FOREIGN KEY ([AssignedToId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_SupportTickets_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


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
    CONSTRAINT [FK_ReviewReports_Reviews_ReviewId] FOREIGN KEY ([ReviewId]) REFERENCES [Reviews] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ReviewReports_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


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
    CONSTRAINT [FK_ReviewVotes_Reviews_ReviewId] FOREIGN KEY ([ReviewId]) REFERENCES [Reviews] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ReviewVotes_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ShipmentEvents] (
    [ShipmentEventId] int NOT NULL IDENTITY,
    [ShipmentId] int NOT NULL,
    [Status] int NOT NULL,
    [PayloadJson] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ShipmentEvents] PRIMARY KEY ([ShipmentEventId]),
    CONSTRAINT [FK_ShipmentEvents_Shipments_ShipmentId] FOREIGN KEY ([ShipmentId]) REFERENCES [Shipments] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [ReturnShipments] (
    [Id] int NOT NULL IDENTITY,
    [ReturnCode] nvarchar(30) NOT NULL,
    [TicketId] int NULL,
    [RefundId] int NULL,
    [SellerOrderId] int NOT NULL,
    [CarrierId] int NULL,
    [CarrierName] nvarchar(50) NULL,
    [TrackingNumber] nvarchar(100) NULL,
    [Reason] nvarchar(30) NOT NULL,
    [ReasonDescription] nvarchar(500) NULL,
    [ShippingCost] decimal(18,2) NOT NULL,
    [PaidBy] nvarchar(20) NOT NULL,
    [Status] nvarchar(30) NOT NULL,
    [CodeGeneratedAt] datetime2 NULL,
    [ShippedAt] datetime2 NULL,
    [ReceivedAt] datetime2 NULL,
    [ExpiresAt] datetime2 NULL,
    [IsPickupRequested] bit NOT NULL,
    [PickupAddress] nvarchar(500) NULL,
    [AdminNotes] nvarchar(1000) NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedDate] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ReturnShipments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ReturnShipments_Refunds_RefundId] FOREIGN KEY ([RefundId]) REFERENCES [Refunds] ([Id]),
    CONSTRAINT [FK_ReturnShipments_SellerOrders_SellerOrderId] FOREIGN KEY ([SellerOrderId]) REFERENCES [SellerOrders] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ReturnShipments_ShippingCarriers_CarrierId] FOREIGN KEY ([CarrierId]) REFERENCES [ShippingCarriers] ([Id]),
    CONSTRAINT [FK_ReturnShipments_SupportTickets_TicketId] FOREIGN KEY ([TicketId]) REFERENCES [SupportTickets] ([Id])
);
GO


CREATE TABLE [SupportTicketMessages] (
    [Id] int NOT NULL IDENTITY,
    [TicketId] int NOT NULL,
    [SenderId] int NOT NULL,
    [Message] nvarchar(max) NOT NULL,
    [Attachments] nvarchar(2000) NULL,
    [IsInternal] bit NOT NULL,
    [IsSystemMessage] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedDate] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_SupportTicketMessages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SupportTicketMessages_SupportTickets_TicketId] FOREIGN KEY ([TicketId]) REFERENCES [SupportTickets] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_SupportTicketMessages_Users_SenderId] FOREIGN KEY ([SenderId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE INDEX [IX_Addresses_UserId] ON [Addresses] ([UserId]);
GO


CREATE INDEX [IX_AffiliatePayouts_AffiliateId] ON [AffiliatePayouts] ([AffiliateId]);
GO


CREATE UNIQUE INDEX [IX_AffiliatePayouts_PayoutNumber] ON [AffiliatePayouts] ([PayoutNumber]) WHERE [IsDeleted] = 0;
GO


CREATE INDEX [IX_AffiliateReferrals_AffiliateId] ON [AffiliateReferrals] ([AffiliateId]);
GO


CREATE INDEX [IX_AffiliateReferrals_OrderId] ON [AffiliateReferrals] ([OrderId]);
GO


CREATE INDEX [IX_AffiliateReferrals_ReferredUserId] ON [AffiliateReferrals] ([ReferredUserId]);
GO


CREATE UNIQUE INDEX [IX_Affiliates_ReferralCode] ON [Affiliates] ([ReferralCode]) WHERE [IsDeleted] = 0;
GO


CREATE UNIQUE INDEX [IX_Affiliates_UserId] ON [Affiliates] ([UserId]) WHERE [IsDeleted] = 0;
GO


CREATE INDEX [IX_AuditLogs_UserId] ON [AuditLogs] ([UserId]);
GO


CREATE INDEX [IX_Banners_Position_DisplayOrder] ON [Banners] ([Position], [DisplayOrder]);
GO


CREATE UNIQUE INDEX [IX_Brands_Slug] ON [Brands] ([Slug]) WHERE [IsDeleted] = 0;
GO


CREATE INDEX [IX_CampaignCategories_CampaignId] ON [CampaignCategories] ([CampaignId]);
GO


CREATE INDEX [IX_CampaignCategories_CategoryId] ON [CampaignCategories] ([CategoryId]);
GO


CREATE INDEX [IX_CampaignProducts_CampaignId] ON [CampaignProducts] ([CampaignId]);
GO


CREATE INDEX [IX_CampaignProducts_ListingId] ON [CampaignProducts] ([ListingId]);
GO


CREATE INDEX [IX_CampaignProducts_ProductId] ON [CampaignProducts] ([ProductId]);
GO


CREATE UNIQUE INDEX [IX_Campaigns_CouponCode] ON [Campaigns] ([CouponCode]) WHERE [CouponCode] IS NOT NULL AND [IsDeleted] = 0;
GO


CREATE INDEX [IX_Campaigns_SellerId] ON [Campaigns] ([SellerId]);
GO


CREATE INDEX [IX_CampaignUsages_CampaignId] ON [CampaignUsages] ([CampaignId]);
GO


CREATE INDEX [IX_CampaignUsages_OrderId] ON [CampaignUsages] ([OrderId]);
GO


CREATE INDEX [IX_CampaignUsages_UserId] ON [CampaignUsages] ([UserId]);
GO


CREATE UNIQUE INDEX [IX_CartItems_CartId_ListingId] ON [CartItems] ([CartId], [ListingId]) WHERE [IsDeleted] = 0;
GO


CREATE INDEX [IX_CartItems_ListingId] ON [CartItems] ([ListingId]);
GO


CREATE INDEX [IX_Carts_UserId] ON [Carts] ([UserId]);
GO


CREATE INDEX [IX_Categories_ParentCategoryId] ON [Categories] ([ParentCategoryId]);
GO


CREATE UNIQUE INDEX [IX_Categories_Slug] ON [Categories] ([Slug]) WHERE [IsDeleted] = 0;
GO


CREATE UNIQUE INDEX [IX_EmailTemplates_Code] ON [EmailTemplates] ([Code]) WHERE [IsDeleted] = 0;
GO


CREATE UNIQUE INDEX [IX_GiftCards_Code] ON [GiftCards] ([Code]) WHERE [IsDeleted] = 0;
GO


CREATE INDEX [IX_GiftCards_OrderId] ON [GiftCards] ([OrderId]);
GO


CREATE INDEX [IX_GiftCards_PurchasedByUserId] ON [GiftCards] ([PurchasedByUserId]);
GO


CREATE INDEX [IX_GiftCards_RecipientUserId] ON [GiftCards] ([RecipientUserId]);
GO


CREATE INDEX [IX_GiftCardUsages_GiftCardId] ON [GiftCardUsages] ([GiftCardId]);
GO


CREATE INDEX [IX_GiftCardUsages_OrderId] ON [GiftCardUsages] ([OrderId]);
GO


CREATE INDEX [IX_GiftCardUsages_UserId] ON [GiftCardUsages] ([UserId]);
GO


CREATE UNIQUE INDEX [IX_HomeWidgets_Code] ON [HomeWidgets] ([Code]) WHERE [Code] IS NOT NULL AND [IsDeleted] = 0;
GO


CREATE INDEX [IX_HomeWidgets_Position_DisplayOrder] ON [HomeWidgets] ([Position], [DisplayOrder]);
GO


CREATE UNIQUE INDEX [IX_Invoices_InvoiceNumber] ON [Invoices] ([InvoiceNumber]) WHERE [IsDeleted] = 0;
GO


CREATE INDEX [IX_Invoices_OrderId] ON [Invoices] ([OrderId]);
GO


CREATE INDEX [IX_Invoices_SellerOrderId] ON [Invoices] ([SellerOrderId]);
GO


CREATE INDEX [IX_Listings_ProductId] ON [Listings] ([ProductId]);
GO


CREATE INDEX [IX_Listings_SellerId] ON [Listings] ([SellerId]);
GO


CREATE UNIQUE INDEX [IX_LoyaltyPoints_UserId] ON [LoyaltyPoints] ([UserId]) WHERE [IsDeleted] = 0;
GO


CREATE INDEX [IX_LoyaltyTransactions_LoyaltyPointId_CreatedAt] ON [LoyaltyTransactions] ([LoyaltyPointId], [CreatedAt]);
GO


CREATE INDEX [IX_LoyaltyTransactions_OrderId] ON [LoyaltyTransactions] ([OrderId]);
GO


CREATE UNIQUE INDEX [IX_NotificationPreferences_UserId] ON [NotificationPreferences] ([UserId]);
GO


CREATE INDEX [IX_Notifications_UserId] ON [Notifications] ([UserId]);
GO


CREATE INDEX [IX_OrderEvents_OrderId] ON [OrderEvents] ([OrderId]);
GO


CREATE INDEX [IX_Orders_BuyerId] ON [Orders] ([BuyerId]);
GO


CREATE INDEX [IX_PaymentEvents_PaymentIntentId] ON [PaymentEvents] ([PaymentIntentId]);
GO


CREATE UNIQUE INDEX [IX_PaymentIntents_OrderId] ON [PaymentIntents] ([OrderId]);
GO


CREATE INDEX [IX_ProductAnswers_AnsweredByUserId] ON [ProductAnswers] ([AnsweredByUserId]);
GO


CREATE INDEX [IX_ProductAnswers_QuestionId] ON [ProductAnswers] ([QuestionId]);
GO


CREATE INDEX [IX_ProductAttributeMappings_AttributeId] ON [ProductAttributeMappings] ([AttributeId]);
GO


CREATE INDEX [IX_ProductAttributeMappings_AttributeValueId] ON [ProductAttributeMappings] ([AttributeValueId]);
GO


CREATE UNIQUE INDEX [IX_ProductAttributeMappings_ProductId_AttributeId_AttributeValueId] ON [ProductAttributeMappings] ([ProductId], [AttributeId], [AttributeValueId]) WHERE [IsDeleted] = 0;
GO


CREATE UNIQUE INDEX [IX_ProductAttributes_Code] ON [ProductAttributes] ([Code]) WHERE [Code] IS NOT NULL AND [IsDeleted] = 0;
GO


CREATE INDEX [IX_ProductAttributeValues_AttributeId] ON [ProductAttributeValues] ([AttributeId]);
GO


CREATE UNIQUE INDEX [IX_ProductBarcodes_Barcode] ON [ProductBarcodes] ([Barcode]) WHERE [IsDeleted] = 0;
GO


CREATE INDEX [IX_ProductBarcodes_ProductId] ON [ProductBarcodes] ([ProductId]);
GO


CREATE UNIQUE INDEX [IX_ProductComparisonItems_ComparisonId_ProductId] ON [ProductComparisonItems] ([ComparisonId], [ProductId]) WHERE [IsDeleted] = 0;
GO


CREATE INDEX [IX_ProductComparisonItems_ProductId] ON [ProductComparisonItems] ([ProductId]);
GO


CREATE INDEX [IX_ProductComparisons_UserId] ON [ProductComparisons] ([UserId]);
GO


CREATE INDEX [IX_ProductImages_ProductId] ON [ProductImages] ([ProductId]);
GO


CREATE INDEX [IX_ProductQuestions_AskedByUserId] ON [ProductQuestions] ([AskedByUserId]);
GO


CREATE INDEX [IX_ProductQuestions_ProductId_Status] ON [ProductQuestions] ([ProductId], [Status]);
GO


CREATE INDEX [IX_Products_CategoryId] ON [Products] ([CategoryId]);
GO


CREATE INDEX [IX_RecentlyViewed_ProductId] ON [RecentlyViewed] ([ProductId]);
GO


CREATE UNIQUE INDEX [IX_RecentlyViewed_UserId_ProductId] ON [RecentlyViewed] ([UserId], [ProductId]) WHERE [IsDeleted] = 0;
GO


CREATE INDEX [IX_RecentlyViewed_UserId_ViewedAt] ON [RecentlyViewed] ([UserId], [ViewedAt]);
GO


CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);
GO


CREATE INDEX [IX_Refunds_OrderId] ON [Refunds] ([OrderId]);
GO


CREATE UNIQUE INDEX [IX_Refunds_RefundNumber] ON [Refunds] ([RefundNumber]) WHERE [IsDeleted] = 0;
GO


CREATE INDEX [IX_Refunds_SellerOrderId] ON [Refunds] ([SellerOrderId]);
GO


CREATE INDEX [IX_Regions_ParentId] ON [Regions] ([ParentId]);
GO


CREATE INDEX [IX_Regions_Type_ParentId] ON [Regions] ([Type], [ParentId]);
GO


CREATE INDEX [IX_ReturnShipments_CarrierId] ON [ReturnShipments] ([CarrierId]);
GO


CREATE INDEX [IX_ReturnShipments_RefundId] ON [ReturnShipments] ([RefundId]);
GO


CREATE UNIQUE INDEX [IX_ReturnShipments_ReturnCode] ON [ReturnShipments] ([ReturnCode]) WHERE [IsDeleted] = 0;
GO


CREATE INDEX [IX_ReturnShipments_SellerOrderId] ON [ReturnShipments] ([SellerOrderId]);
GO


CREATE INDEX [IX_ReturnShipments_Status_CreatedAt] ON [ReturnShipments] ([Status], [CreatedAt]);
GO


CREATE INDEX [IX_ReturnShipments_TicketId] ON [ReturnShipments] ([TicketId]);
GO


CREATE INDEX [IX_ReviewReports_ReviewId] ON [ReviewReports] ([ReviewId]);
GO


CREATE INDEX [IX_ReviewReports_UserId] ON [ReviewReports] ([UserId]);
GO


CREATE INDEX [IX_Reviews_OrderId] ON [Reviews] ([OrderId]);
GO


CREATE INDEX [IX_Reviews_ProductId_Status] ON [Reviews] ([ProductId], [Status]);
GO


CREATE INDEX [IX_Reviews_SellerId_Status] ON [Reviews] ([SellerId], [Status]);
GO


CREATE INDEX [IX_Reviews_SellerOrderId] ON [Reviews] ([SellerOrderId]);
GO


CREATE INDEX [IX_Reviews_UserId] ON [Reviews] ([UserId]);
GO


CREATE UNIQUE INDEX [IX_ReviewVotes_ReviewId_UserId] ON [ReviewVotes] ([ReviewId], [UserId]) WHERE [IsDeleted] = 0;
GO


CREATE INDEX [IX_ReviewVotes_UserId] ON [ReviewVotes] ([UserId]);
GO


CREATE INDEX [IX_SellerOrderItems_ListingId] ON [SellerOrderItems] ([ListingId]);
GO


CREATE INDEX [IX_SellerOrderItems_SellerOrderId] ON [SellerOrderItems] ([SellerOrderId]);
GO


CREATE INDEX [IX_SellerOrders_OrderId] ON [SellerOrders] ([OrderId]);
GO


CREATE INDEX [IX_SellerOrders_SellerId] ON [SellerOrders] ([SellerId]);
GO


CREATE INDEX [IX_SellerPayoutItems_PayoutId] ON [SellerPayoutItems] ([PayoutId]);
GO


CREATE INDEX [IX_SellerPayoutItems_SellerOrderId] ON [SellerPayoutItems] ([SellerOrderId]);
GO


CREATE UNIQUE INDEX [IX_SellerPayouts_PayoutNumber] ON [SellerPayouts] ([PayoutNumber]) WHERE [IsDeleted] = 0;
GO


CREATE INDEX [IX_SellerPayouts_SellerId_Status] ON [SellerPayouts] ([SellerId], [Status]);
GO


CREATE UNIQUE INDEX [IX_SellerProfiles_UserId] ON [SellerProfiles] ([UserId]) WHERE [IsDeleted] = 0;
GO


CREATE INDEX [IX_SellerProfiles_VerificationStatus] ON [SellerProfiles] ([VerificationStatus]);
GO


CREATE INDEX [IX_SellerReviews_OrderId] ON [SellerReviews] ([OrderId]);
GO


CREATE INDEX [IX_SellerReviews_SellerId_Status] ON [SellerReviews] ([SellerId], [Status]);
GO


CREATE UNIQUE INDEX [IX_SellerReviews_UserId_SellerId_OrderId] ON [SellerReviews] ([UserId], [SellerId], [OrderId]) WHERE [IsDeleted] = 0;
GO


CREATE INDEX [IX_SellerShippingSettings_PreferredCarrierId] ON [SellerShippingSettings] ([PreferredCarrierId]);
GO


CREATE UNIQUE INDEX [IX_SellerShippingSettings_SellerId] ON [SellerShippingSettings] ([SellerId]) WHERE [IsDeleted] = 0;
GO


CREATE INDEX [IX_ShipmentEvents_ShipmentId] ON [ShipmentEvents] ([ShipmentId]);
GO


CREATE UNIQUE INDEX [IX_Shipments_SellerOrderId] ON [Shipments] ([SellerOrderId]);
GO


CREATE INDEX [IX_Shipments_TrackingNumber] ON [Shipments] ([TrackingNumber]);
GO


CREATE UNIQUE INDEX [IX_SiteSettings_Key] ON [SiteSettings] ([Key]) WHERE [IsDeleted] = 0;
GO


CREATE INDEX [IX_SupportTicketMessages_SenderId] ON [SupportTicketMessages] ([SenderId]);
GO


CREATE INDEX [IX_SupportTicketMessages_TicketId_CreatedAt] ON [SupportTicketMessages] ([TicketId], [CreatedAt]);
GO


CREATE INDEX [IX_SupportTickets_AssignedToId] ON [SupportTickets] ([AssignedToId]);
GO


CREATE INDEX [IX_SupportTickets_OrderId] ON [SupportTickets] ([OrderId]);
GO


CREATE INDEX [IX_SupportTickets_SellerOrderId] ON [SupportTickets] ([SellerOrderId]);
GO


CREATE INDEX [IX_SupportTickets_Status_Priority_CreatedAt] ON [SupportTickets] ([Status], [Priority], [CreatedAt]);
GO


CREATE UNIQUE INDEX [IX_SupportTickets_TicketNumber] ON [SupportTickets] ([TicketNumber]) WHERE [IsDeleted] = 0;
GO


CREATE INDEX [IX_SupportTickets_UserId] ON [SupportTickets] ([UserId]);
GO


CREATE UNIQUE INDEX [IX_TaxRates_Code] ON [TaxRates] ([Code]) WHERE [Code] IS NOT NULL AND [IsDeleted] = 0;
GO


CREATE INDEX [IX_WishlistItems_ListingId] ON [WishlistItems] ([ListingId]);
GO


CREATE INDEX [IX_WishlistItems_ProductId] ON [WishlistItems] ([ProductId]);
GO


CREATE UNIQUE INDEX [IX_WishlistItems_WishlistId_ProductId] ON [WishlistItems] ([WishlistId], [ProductId]) WHERE [IsDeleted] = 0;
GO


CREATE INDEX [IX_Wishlists_UserId] ON [Wishlists] ([UserId]);
GO


