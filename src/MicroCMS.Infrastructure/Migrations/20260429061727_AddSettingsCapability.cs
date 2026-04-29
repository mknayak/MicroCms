using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicroCMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSettingsCapability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiProviderSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ActiveProvider = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Budget_MonthlyCostCapUsd = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Budget_PerUserDailyTokenCap = table.Column<int>(type: "INTEGER", nullable: false),
                    Budget_HardStop = table.Column<bool>(type: "INTEGER", nullable: false),
                    Budget_CurrentMonthSpendUsd = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Safety_PiiRedactionEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Safety_PromptInjectionDetectionEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Safety_SafetyPostFilterEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Safety_GroundedOnlyModeDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    Safety_DataResidencyRegion = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    Safety_AuditLogRetentionDays = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiProviderSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApiClients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    KeyType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    HashedSecret = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExpiresAt = table.Column<long>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    Scopes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiClients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ParentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComponentItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ComponentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    FieldsJson = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    UsedOnPages = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComponentItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Components",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ZonesJson = table.Column<string>(type: "TEXT", nullable: false),
                    UsageCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemCount = table.Column<int>(type: "INTEGER", nullable: false),
                    TemplateType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    TemplateContent = table.Column<string>(type: "TEXT", nullable: true),
                    BackingContentTypeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Components", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContentTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Handle = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    LocalizationMode = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false, defaultValue: "PerLocale"),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "Content"),
                    LayoutId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CopilotConversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GroundedOnlyMode = table.Column<bool>(type: "INTEGER", nullable: false),
                    TotalPromptTokens = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalCompletionTokens = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalCostUsd = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    LastMessageAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CopilotConversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EditLocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    LockedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LockedByDisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    LockedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    ExpiresAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EditLocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContentTypeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Locale = table.Column<string>(type: "TEXT", maxLength: 35, nullable: false),
                    AuthorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    CurrentVersionNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    FieldsJson = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    PublishedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    ScheduledPublishAt = table.Column<long>(type: "INTEGER", nullable: true),
                    ScheduledUnpublishAt = table.Column<long>(type: "INTEGER", nullable: true),
                    FolderId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Folders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ParentFolderId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Folders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Layouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TemplateType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ZonesJson = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "[]"),
                    DefaultPlacementsJson = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "[]"),
                    ShellTemplate = table.Column<string>(type: "TEXT", nullable: true),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Layouts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoginAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    IsSuccessful = table.Column<bool>(type: "INTEGER", nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    AttemptedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginAttempts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MediaAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Meta_FileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Meta_MimeType = table.Column<string>(type: "TEXT", maxLength: 127, nullable: false),
                    Meta_SizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    Meta_WidthPx = table.Column<int>(type: "INTEGER", nullable: true),
                    Meta_HeightPx = table.Column<int>(type: "INTEGER", nullable: true),
                    Meta_Duration = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    Meta_ExifJson = table.Column<string>(type: "TEXT", nullable: false),
                    StorageKey = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    FolderId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UploadedBy = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    AltText = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    AiAltText = table.Column<string>(type: "TEXT", nullable: true),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaAssets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MediaFolders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ParentFolderId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaFolders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    OccurredOnUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    ProcessedOnUtc = table.Column<long>(type: "INTEGER", nullable: true),
                    Error = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    RetryCount = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PageType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ParentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    LinkedEntryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CollectionContentTypeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RoutePattern = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Depth = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    LayoutId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SeoMetaTitle = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    SeoMetaDescription = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    SeoCanonicalUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    SeoOgImage = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    SiteTemplateId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PageTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PageId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    PlacementsJson = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "[]")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Plugins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Version = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Author = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Signature = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    InstalledAt = table.Column<long>(type: "INTEGER", nullable: false),
                    Capabilities = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plugins", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TokenHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    FamilyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<long>(type: "INTEGER", nullable: false),
                    IsRevoked = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    ReplacedByTokenHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SiteSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PreviewUrlTemplate = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    VersioningEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    WorkflowEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    SchedulingEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    PreviewEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AiEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    CorsOrigins = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false, defaultValue: ""),
                    Locales = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SiteTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LayoutId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PlacementsJson = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 63, nullable: false),
                    Settings_DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Settings_DefaultLocale = table.Column<string>(type: "TEXT", maxLength: 35, nullable: false),
                    Settings_EnabledLocales = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    Settings_TimeZoneId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Settings_AiEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Settings_LogoUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    Quota_MaxStorageBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    Quota_MaxApiCallsPerMinute = table.Column<int>(type: "INTEGER", nullable: false),
                    Quota_MaxUsers = table.Column<int>(type: "INTEGER", nullable: false),
                    Quota_MaxSites = table.Column<int>(type: "INTEGER", nullable: false),
                    Quota_MaxContentTypes = table.Column<int>(type: "INTEGER", nullable: false),
                    Quota_MaxAiTokensPerMonth = table.Column<long>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantSecuritySettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RequireMfaForAdmins = table.Column<bool>(type: "INTEGER", nullable: false),
                    SessionIdleTimeout = table.Column<long>(type: "INTEGER", nullable: false),
                    AbsoluteSessionTimeout = table.Column<long>(type: "INTEGER", nullable: false),
                    SsoEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    OidcIssuer = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    IpAllowlist = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantSecuritySettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    LastLoginAt = table.Column<long>(type: "INTEGER", nullable: true),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 72, nullable: true),
                    PasswordChangedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    FailedLoginAttempts = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    LockoutEnd = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebhookSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TargetUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    HashedSecret = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    MaxRetries = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    Events = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiModelTierOverrides",
                columns: table => new
                {
                    FeatureKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    AiProviderSettingsId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Model = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiModelTierOverrides", x => new { x.AiProviderSettingsId, x.FeatureKey });
                    table.ForeignKey(
                        name: "FK_AiModelTierOverrides_AiProviderSettings_AiProviderSettingsId",
                        column: x => x.AiProviderSettingsId,
                        principalTable: "AiProviderSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComponentFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContentTypeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Handle = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    FieldType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    IsRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsLocalized = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsUnique = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsIndexed = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsList = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ValidationJson = table.Column<string>(type: "TEXT", nullable: true),
                    ComponentId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComponentFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComponentFields_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContentTypeFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContentTypeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Handle = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    FieldType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    IsRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsLocalized = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsUnique = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsIndexed = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsList = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ValidationJson = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentTypeFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentTypeFields_ContentTypes_ContentTypeId",
                        column: x => x.ContentTypeId,
                        principalTable: "ContentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CopilotMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    ConversationId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CopilotMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CopilotMessages_CopilotConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "CopilotConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EntryVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VersionNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    FieldsJson = table.Column<string>(type: "TEXT", nullable: false),
                    AuthorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChangeNote = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntryVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntryVersions_Entries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "Entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PageTemplatePlacements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ComponentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Zone = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    BoundItemId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsLayoutDefault = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    PageTemplateId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageTemplatePlacements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PageTemplatePlacements_PageTemplates_PageTemplateId",
                        column: x => x.PageTemplateId,
                        principalTable: "PageTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SiteConfigEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, defaultValue: "general"),
                    IsSecret = table.Column<bool>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    SiteSettingsId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteConfigEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SiteConfigEntries_SiteSettings_SiteSettingsId",
                        column: x => x.SiteSettingsId,
                        principalTable: "SiteSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantConfigEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, defaultValue: "general"),
                    IsSecret = table.Column<bool>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    TenantConfigId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantConfigEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantConfigEntries_TenantConfigs_TenantConfigId",
                        column: x => x.TenantConfigId,
                        principalTable: "TenantConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Handle = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DefaultLocale = table.Column<string>(type: "TEXT", maxLength: 35, nullable: false),
                    CustomDomain = table.Column<string>(type: "TEXT", maxLength: 253, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sites_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorkflowRole = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WebhookDeliveryLogs",
                columns: table => new
                {
                    DeliveredAt = table.Column<long>(type: "INTEGER", nullable: false),
                    WebhookSubscriptionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    StatusCode = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookDeliveryLogs", x => new { x.WebhookSubscriptionId, x.DeliveredAt });
                    table.ForeignKey(
                        name: "FK_WebhookDeliveryLogs_WebhookSubscriptions_WebhookSubscriptionId",
                        column: x => x.WebhookSubscriptionId,
                        principalTable: "WebhookSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CopilotMessageCitations",
                columns: table => new
                {
                    EntryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MessageId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    SimilarityScore = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CopilotMessageCitations", x => new { x.MessageId, x.EntryId });
                    table.ForeignKey(
                        name: "FK_CopilotMessageCitations_CopilotMessages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "CopilotMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SiteEnvironments",
                columns: table => new
                {
                    Type = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    SiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    SslStatus = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    IsLive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteEnvironments", x => new { x.SiteId, x.Type });
                    table.ForeignKey(
                        name: "FK_SiteEnvironments_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiProviderSettings_TenantId",
                table: "AiProviderSettings",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_SiteId_Slug",
                table: "Categories",
                columns: new[] { "SiteId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComponentFields_ComponentId",
                table: "ComponentFields",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_ComponentItems_ComponentId_Status",
                table: "ComponentItems",
                columns: new[] { "ComponentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypeFields_ContentTypeId_Handle",
                table: "ContentTypeFields",
                columns: new[] { "ContentTypeId", "Handle" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypes_SiteId_Handle",
                table: "ContentTypes",
                columns: new[] { "SiteId", "Handle" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CopilotMessages_ConversationId",
                table: "CopilotMessages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_EditLocks_EntityId",
                table: "EditLocks",
                column: "EntityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Entries_SiteId_Locale_Slug",
                table: "Entries",
                columns: new[] { "SiteId", "Locale", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EntryVersions_EntryId",
                table: "EntryVersions",
                column: "EntryId");

            migrationBuilder.CreateIndex(
                name: "IX_Folders_TenantId_SiteId_ParentFolderId",
                table: "Folders",
                columns: new[] { "TenantId", "SiteId", "ParentFolderId" });

            migrationBuilder.CreateIndex(
                name: "IX_Layouts_SiteId_Key",
                table: "Layouts",
                columns: new[] { "SiteId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_AttemptedAt",
                table: "LoginAttempts",
                column: "AttemptedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_TenantId_Email_AttemptedAt",
                table: "LoginAttempts",
                columns: new[] { "TenantId", "Email", "AttemptedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaFolders_TenantId_SiteId_ParentFolderId",
                table: "MediaFolders",
                columns: new[] { "TenantId", "SiteId", "ParentFolderId" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc_OccurredOnUtc",
                table: "OutboxMessages",
                columns: new[] { "ProcessedOnUtc", "OccurredOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_TenantId_ProcessedOnUtc",
                table: "OutboxMessages",
                columns: new[] { "TenantId", "ProcessedOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Pages_SiteId_ParentId",
                table: "Pages",
                columns: new[] { "SiteId", "ParentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Pages_SiteId_Slug",
                table: "Pages",
                columns: new[] { "SiteId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PageTemplatePlacements_PageTemplateId",
                table: "PageTemplatePlacements",
                column: "PageTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_FamilyId",
                table: "RefreshTokens",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_IsRevoked",
                table: "RefreshTokens",
                columns: new[] { "UserId", "IsRevoked" });

            migrationBuilder.CreateIndex(
                name: "IX_SiteConfigEntries_SiteSettingsId_Key",
                table: "SiteConfigEntries",
                columns: new[] { "SiteSettingsId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sites_TenantId_Handle",
                table: "Sites",
                columns: new[] { "TenantId", "Handle" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SiteTemplates_TenantId_SiteId",
                table: "SiteTemplates",
                columns: new[] { "TenantId", "SiteId" });

            migrationBuilder.CreateIndex(
                name: "IX_Tags_SiteId_Slug",
                table: "Tags",
                columns: new[] { "SiteId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantConfigEntries_TenantConfigId_Key",
                table: "TenantConfigEntries",
                columns: new[] { "TenantConfigId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Slug",
                table: "Tenants",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantSecuritySettings_TenantId",
                table: "TenantSecuritySettings",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId",
                table: "UserRoles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId_Email",
                table: "Users",
                columns: new[] { "TenantId", "Email" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiModelTierOverrides");

            migrationBuilder.DropTable(
                name: "ApiClients");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "ComponentFields");

            migrationBuilder.DropTable(
                name: "ComponentItems");

            migrationBuilder.DropTable(
                name: "ContentTypeFields");

            migrationBuilder.DropTable(
                name: "CopilotMessageCitations");

            migrationBuilder.DropTable(
                name: "EditLocks");

            migrationBuilder.DropTable(
                name: "EntryVersions");

            migrationBuilder.DropTable(
                name: "Folders");

            migrationBuilder.DropTable(
                name: "Layouts");

            migrationBuilder.DropTable(
                name: "LoginAttempts");

            migrationBuilder.DropTable(
                name: "MediaAssets");

            migrationBuilder.DropTable(
                name: "MediaFolders");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "Pages");

            migrationBuilder.DropTable(
                name: "PageTemplatePlacements");

            migrationBuilder.DropTable(
                name: "Plugins");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "SiteConfigEntries");

            migrationBuilder.DropTable(
                name: "SiteEnvironments");

            migrationBuilder.DropTable(
                name: "SiteTemplates");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "TenantConfigEntries");

            migrationBuilder.DropTable(
                name: "TenantSecuritySettings");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "WebhookDeliveryLogs");

            migrationBuilder.DropTable(
                name: "AiProviderSettings");

            migrationBuilder.DropTable(
                name: "Components");

            migrationBuilder.DropTable(
                name: "ContentTypes");

            migrationBuilder.DropTable(
                name: "CopilotMessages");

            migrationBuilder.DropTable(
                name: "Entries");

            migrationBuilder.DropTable(
                name: "PageTemplates");

            migrationBuilder.DropTable(
                name: "SiteSettings");

            migrationBuilder.DropTable(
                name: "Sites");

            migrationBuilder.DropTable(
                name: "TenantConfigs");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "WebhookSubscriptions");

            migrationBuilder.DropTable(
                name: "CopilotConversations");

            migrationBuilder.DropTable(
                name: "Tenants");
        }
    }
}
