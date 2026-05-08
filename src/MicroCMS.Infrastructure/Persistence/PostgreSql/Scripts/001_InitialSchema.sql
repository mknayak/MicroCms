-- =============================================================================
-- MicroCMS – PostgreSQL Full Schema Script
-- Source of truth: EF Core entity configurations in
--   src/MicroCMS.Infrastructure/Persistence/Common/Configurations/
-- Run this script once against a clean database to initialise the schema.
-- =============================================================================

-- Enable uuid generation (needed if you ever use gen_random_uuid() in defaults)
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- =============================================================================
-- TENANTS
-- =============================================================================
CREATE TABLE "Tenants" (
    "Id"                          uuid         NOT NULL,
    "Slug"                        varchar(63)  NOT NULL,
    "Status"                      varchar(32)  NOT NULL,
    "CreatedAt"                   timestamptz  NOT NULL,
    "UpdatedAt"                   timestamptz  NOT NULL,
    -- Owned: TenantSettings (flattened)
    "Settings_DisplayName"        varchar(200) NOT NULL,
    "Settings_DefaultLocale"      varchar(35)  NOT NULL,
    "Settings_EnabledLocales"     varchar(1024) NOT NULL,
    "Settings_TimeZoneId"         varchar(64)  NOT NULL,
    "Settings_AiEnabled"          boolean      NOT NULL,
    "Settings_LogoUrl"            varchar(2048),
    -- Owned: TenantQuota (flattened)
    "Quota_MaxStorageBytes"       bigint       NOT NULL,
    "Quota_MaxApiCallsPerMinute"  integer      NOT NULL,
    "Quota_MaxUsers"              integer      NOT NULL,
    "Quota_MaxSites"              integer      NOT NULL,
    "Quota_MaxContentTypes"       integer      NOT NULL,
    "Quota_MaxAiTokensPerMonth"   bigint       NOT NULL,
    CONSTRAINT "PK_Tenants" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "IX_Tenants_Slug" ON "Tenants" ("Slug");

-- =============================================================================
-- SITES  (owned by Tenant)
-- =============================================================================
CREATE TABLE "Sites" (
    "Id"            uuid         NOT NULL,
    "TenantId"      uuid         NOT NULL,
    "Name"          varchar(100) NOT NULL,
    "Handle"        varchar(200) NOT NULL,
    "DefaultLocale" varchar(35)  NOT NULL,
    "CustomDomain"  varchar(253),
    "IsActive"      boolean      NOT NULL,
    "CreatedAt"     timestamptz  NOT NULL,
    CONSTRAINT "PK_Sites" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Sites_Tenants_TenantId"
        FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_Sites_TenantId_Handle" ON "Sites" ("TenantId", "Handle");

-- =============================================================================
-- SITE ENVIRONMENTS  (owned by Site)
-- =============================================================================
CREATE TABLE "SiteEnvironments" (
    "SiteId"    uuid        NOT NULL,
    "Type"      varchar(32) NOT NULL,
    "Url"       varchar(500) NOT NULL,
    "SslStatus" varchar(32) NOT NULL,
    "IsLive"    boolean     NOT NULL,
    CONSTRAINT "PK_SiteEnvironments" PRIMARY KEY ("SiteId", "Type"),
    CONSTRAINT "FK_SiteEnvironments_Sites_SiteId"
        FOREIGN KEY ("SiteId") REFERENCES "Sites" ("Id") ON DELETE CASCADE
);

-- =============================================================================
-- TENANT CONFIGS  (1-to-1 with Tenant; PK = TenantId)
-- =============================================================================
CREATE TABLE "TenantConfigs" (
    "Id"        uuid        NOT NULL,   -- equals TenantId
    "UpdatedAt" timestamptz NOT NULL,
    CONSTRAINT "PK_TenantConfigs" PRIMARY KEY ("Id")
);

-- =============================================================================
-- TENANT CONFIG ENTRIES  (owned by TenantConfig)
-- =============================================================================
CREATE TABLE "TenantConfigEntries" (
    "Id"              uuid         NOT NULL,
    "TenantConfigId"  uuid         NOT NULL,
    "Key"             varchar(200) NOT NULL,
    "Value"           varchar(4000) NOT NULL,
    "Category"        varchar(100) NOT NULL DEFAULT 'general',
    "IsSecret"        boolean      NOT NULL,
    "UpdatedAt"       timestamptz  NOT NULL,
    CONSTRAINT "PK_TenantConfigEntries" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_TenantConfigEntries_TenantConfigs_TenantConfigId"
        FOREIGN KEY ("TenantConfigId") REFERENCES "TenantConfigs" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_TenantConfigEntries_TenantConfigId_Key"
    ON "TenantConfigEntries" ("TenantConfigId", "Key");

-- =============================================================================
-- TENANT SECURITY SETTINGS
-- =============================================================================
CREATE TABLE "TenantSecuritySettings" (
    "Id"                      uuid         NOT NULL,
    "TenantId"                uuid         NOT NULL,
    "RequireMfaForAdmins"     boolean      NOT NULL,
    -- TimeSpan stored as total seconds (bigint) — portable across all DB providers
    "SessionIdleTimeout"      bigint       NOT NULL,
    "AbsoluteSessionTimeout"  bigint       NOT NULL,
    "SsoEnabled"              boolean      NOT NULL,
    "OidcIssuer"              varchar(500),
    "UpdatedAt"               timestamptz  NOT NULL,
    -- IP allowlist stored as pipe-separated string via private backing field
    "IpAllowlist"             varchar(4000) NOT NULL DEFAULT '',
    CONSTRAINT "PK_TenantSecuritySettings" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "IX_TenantSecuritySettings_TenantId"
    ON "TenantSecuritySettings" ("TenantId");

-- =============================================================================
-- SITE SETTINGS
-- =============================================================================
CREATE TABLE "SiteSettings" (
    "Id"                 uuid          NOT NULL,   -- equals SiteId
    "TenantId"           uuid          NOT NULL,
    "PreviewUrlTemplate" varchar(500),
    "VersioningEnabled"  boolean       NOT NULL,
    "WorkflowEnabled"    boolean       NOT NULL,
    "SchedulingEnabled"  boolean       NOT NULL,
    "PreviewEnabled"     boolean       NOT NULL,
    "AiEnabled"          boolean       NOT NULL,
    "UpdatedAt"          timestamptz   NOT NULL,
    -- Pipe-separated lists stored as strings
    "CorsOrigins"        varchar(4000) NOT NULL DEFAULT '',
    "Locales"            varchar(1024) NOT NULL DEFAULT '',
    CONSTRAINT "PK_SiteSettings" PRIMARY KEY ("Id")
);

-- =============================================================================
-- SITE CONFIG ENTRIES  (owned by SiteSettings)
-- =============================================================================
CREATE TABLE "SiteConfigEntries" (
    "Id"             uuid         NOT NULL,
    "SiteSettingsId" uuid         NOT NULL,
    "Key"            varchar(200) NOT NULL,
    "Value"          varchar(4000) NOT NULL,
    "Category"       varchar(100) NOT NULL DEFAULT 'general',
    "IsSecret"       boolean      NOT NULL,
    "UpdatedAt"      timestamptz  NOT NULL,
    CONSTRAINT "PK_SiteConfigEntries" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SiteConfigEntries_SiteSettings_SiteSettingsId"
        FOREIGN KEY ("SiteSettingsId") REFERENCES "SiteSettings" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_SiteConfigEntries_SiteSettingsId_Key"
    ON "SiteConfigEntries" ("SiteSettingsId", "Key");

-- =============================================================================
-- API CLIENTS
-- =============================================================================
CREATE TABLE "ApiClients" (
    "Id"           uuid         NOT NULL,
    "TenantId"     uuid         NOT NULL,
    "SiteId"       uuid         NOT NULL,
    "Name"         varchar(200) NOT NULL,
    "KeyType"      varchar(32)  NOT NULL,
    "HashedSecret" varchar(256) NOT NULL,
    "IsActive"     boolean      NOT NULL,
    "ExpiresAt"    timestamptz,
    "CreatedAt"    timestamptz  NOT NULL,
    -- Scopes stored as pipe-separated string via private backing field
    "Scopes"       varchar(2000) NOT NULL DEFAULT '',
    CONSTRAINT "PK_ApiClients" PRIMARY KEY ("Id")
);

-- =============================================================================
-- CONTENT TYPES
-- Note: self-referencing FK for ParentContentTypeId added after table creation
-- =============================================================================
CREATE TABLE "ContentTypes" (
    "Id"                   uuid         NOT NULL,
    "TenantId"             uuid         NOT NULL,
    "SiteId"               uuid         NOT NULL,
    "Handle"               varchar(64)  NOT NULL,
    "DisplayName"          varchar(200) NOT NULL,
    "Description"          varchar(500),
    "LocalizationMode"     varchar(32)  NOT NULL DEFAULT 'PerLocale',
    "Status"               varchar(32)  NOT NULL,
    "Kind"                 varchar(20)  NOT NULL DEFAULT 'Content',
    "SiteTemplateId"       uuid,
    "ParentContentTypeId"  uuid,
    "CreatedAt"            timestamptz  NOT NULL,
    "UpdatedAt"            timestamptz  NOT NULL,
    CONSTRAINT "PK_ContentTypes" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "IX_ContentTypes_SiteId_Handle"
    ON "ContentTypes" ("SiteId", "Handle");

CREATE INDEX "IX_ContentTypes_ParentContentTypeId"
    ON "ContentTypes" ("ParentContentTypeId");

ALTER TABLE "ContentTypes"
    ADD CONSTRAINT "FK_ContentTypes_ContentTypes_ParentContentTypeId"
        FOREIGN KEY ("ParentContentTypeId") REFERENCES "ContentTypes" ("Id")
        ON DELETE SET NULL;

-- =============================================================================
-- CONTENT TYPE FIELDS  (owned by ContentType)
-- =============================================================================
CREATE TABLE "ContentTypeFields" (
    "Id"            uuid         NOT NULL,
    "ContentTypeId" uuid         NOT NULL,
    "Handle"        varchar(64)  NOT NULL,
    "Label"         varchar(200) NOT NULL,
    "FieldType"     varchar(32)  NOT NULL,
    "IsRequired"    boolean      NOT NULL,
    "IsLocalized"   boolean      NOT NULL,
    "IsUnique"      boolean      NOT NULL,
    "IsIndexed"     boolean      NOT NULL,
    "IsList"        boolean      NOT NULL DEFAULT false,
    "SortOrder"     integer      NOT NULL,
    "Description"   varchar(500),
    "GroupName"     varchar(100) NOT NULL DEFAULT 'Default',
    "ValidationJson" text,
    CONSTRAINT "PK_ContentTypeFields" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ContentTypeFields_ContentTypes_ContentTypeId"
        FOREIGN KEY ("ContentTypeId") REFERENCES "ContentTypes" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_ContentTypeFields_ContentTypeId_Handle"
    ON "ContentTypeFields" ("ContentTypeId", "Handle");

-- =============================================================================
-- FOLDERS  (content entry folders)
-- =============================================================================
CREATE TABLE "Folders" (
    "Id"             uuid         NOT NULL,
    "TenantId"       uuid         NOT NULL,
    "SiteId"         uuid         NOT NULL,
    "Name"           varchar(200) NOT NULL,
    "ParentFolderId" uuid,
    "CreatedAt"      timestamptz  NOT NULL,
    "UpdatedAt"      timestamptz  NOT NULL,
    CONSTRAINT "PK_Folders" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Folders_Folders_ParentFolderId"
        FOREIGN KEY ("ParentFolderId") REFERENCES "Folders" ("Id") ON DELETE SET NULL
);

CREATE INDEX "IX_Folders_TenantId_SiteId_ParentFolderId"
    ON "Folders" ("TenantId", "SiteId", "ParentFolderId");

-- =============================================================================
-- ENTRIES
-- =============================================================================
CREATE TABLE "Entries" (
    "Id"                     uuid         NOT NULL,
    "TenantId"               uuid         NOT NULL,
    "SiteId"                 uuid         NOT NULL,
    "ContentTypeId"          uuid         NOT NULL,
    "Slug"                   varchar(200) NOT NULL,
    "Locale"                 varchar(35)  NOT NULL,
    "AuthorId"               uuid         NOT NULL,
    "Status"                 varchar(32)  NOT NULL,
    "CurrentVersionNumber"   integer      NOT NULL,
    "FieldsJson"             text         NOT NULL,
    "CreatedAt"              timestamptz  NOT NULL,
    "UpdatedAt"              timestamptz  NOT NULL,
    "PublishedAt"            timestamptz,
    "ScheduledPublishAt"     timestamptz,
    "ScheduledUnpublishAt"   timestamptz,
    "FolderId"               uuid,
    CONSTRAINT "PK_Entries" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "IX_Entries_SiteId_Locale_Slug"
    ON "Entries" ("SiteId", "Locale", "Slug");

-- =============================================================================
-- ENTRY VERSIONS  (owned by Entry — append-only)
-- =============================================================================
CREATE TABLE "EntryVersions" (
    "Id"            uuid    NOT NULL,
    "EntryId"       uuid    NOT NULL,
    "VersionNumber" integer NOT NULL,
    "FieldsJson"    text    NOT NULL,
    "AuthorId"      uuid    NOT NULL,
    "ChangeNote"    text,
    "CreatedAt"     timestamptz NOT NULL,
    CONSTRAINT "PK_EntryVersions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_EntryVersions_Entries_EntryId"
        FOREIGN KEY ("EntryId") REFERENCES "Entries" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_EntryVersions_EntryId" ON "EntryVersions" ("EntryId");

-- =============================================================================
-- ENTRY GROUPS
-- =============================================================================
CREATE TABLE "EntryGroups" (
    "Id"            uuid          NOT NULL,
    "TenantId"      uuid          NOT NULL,
    "SiteId"        uuid          NOT NULL,
    "ContentTypeId" uuid          NOT NULL,
    "Handle"        varchar(64)   NOT NULL,
    "Title"         varchar(200)  NOT NULL,
    "Description"   varchar(1000),
    "ImageAssetId"  uuid,
    "CreatedAt"     timestamptz   NOT NULL,
    "UpdatedAt"     timestamptz   NOT NULL,
    CONSTRAINT "PK_EntryGroups" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "IX_EntryGroups_SiteId_ContentTypeId_Handle"
    ON "EntryGroups" ("SiteId", "ContentTypeId", "Handle");

-- =============================================================================
-- ENTRY GROUP MEMBERS  (owned by EntryGroup)
-- =============================================================================
CREATE TABLE "EntryGroupMembers" (
    "GroupId" uuid NOT NULL,
    "EntryId" uuid NOT NULL,
    CONSTRAINT "PK_EntryGroupMembers" PRIMARY KEY ("GroupId", "EntryId"),
    CONSTRAINT "FK_EntryGroupMembers_EntryGroups_GroupId"
        FOREIGN KEY ("GroupId") REFERENCES "EntryGroups" ("Id") ON DELETE CASCADE
);

-- =============================================================================
-- MEDIA FOLDERS
-- =============================================================================
CREATE TABLE "MediaFolders" (
    "Id"             uuid         NOT NULL,
    "TenantId"       uuid         NOT NULL,
    "SiteId"         uuid         NOT NULL,
    "Name"           varchar(200) NOT NULL,
    "ParentFolderId" uuid,
    "CreatedAt"      timestamptz  NOT NULL,
    CONSTRAINT "PK_MediaFolders" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_MediaFolders_TenantId_SiteId_ParentFolderId"
    ON "MediaFolders" ("TenantId", "SiteId", "ParentFolderId");

-- =============================================================================
-- MEDIA ASSETS
-- =============================================================================
CREATE TABLE "MediaAssets" (
    "Id"             uuid          NOT NULL,
    "TenantId"       uuid          NOT NULL,
    "SiteId"         uuid          NOT NULL,
    "StorageKey"     varchar(1024) NOT NULL,
    "FolderId"       uuid,
    "UploadedBy"     uuid          NOT NULL,
    "Status"         varchar(32)   NOT NULL,
    "AltText"        varchar(500),
    "AiAltText"      varchar(500),
    "Visibility"     varchar(32)   NOT NULL DEFAULT 'Public',
    "CreatedAt"      timestamptz   NOT NULL,
    "UpdatedAt"      timestamptz   NOT NULL,
    -- Owned: AssetMetadata (flattened)
    "Meta_FileName"  varchar(255)  NOT NULL,
    "Meta_MimeType"  varchar(127)  NOT NULL,
    "Meta_SizeBytes" bigint        NOT NULL,
    "Meta_WidthPx"   integer,
    "Meta_HeightPx"  integer,
    "Meta_Duration"  interval,
    "Meta_ExifJson"  text          NOT NULL,
    -- Tags stored as pipe-separated string via private backing field
    "Tags"           varchar(2000) NOT NULL DEFAULT '',
    CONSTRAINT "PK_MediaAssets" PRIMARY KEY ("Id")
);

-- =============================================================================
-- MEDIA BLOBS
-- =============================================================================
CREATE TABLE "MediaBlobs" (
    "StorageKey" varchar(1024) NOT NULL,
    "MimeType"   varchar(256)  NOT NULL,
    "Data"       bytea         NOT NULL,
    "CreatedAt"  timestamptz   NOT NULL,
    CONSTRAINT "PK_MediaBlobs" PRIMARY KEY ("StorageKey")
);

-- =============================================================================
-- CATEGORIES
-- =============================================================================
CREATE TABLE "Categories" (
    "Id"          uuid         NOT NULL,
    "TenantId"    uuid         NOT NULL,
    "SiteId"      uuid         NOT NULL,
    "Name"        varchar(200) NOT NULL,
    "Slug"        varchar(200) NOT NULL,
    "ParentId"    uuid,
    "Description" varchar(500),
    "CreatedAt"   timestamptz  NOT NULL,
    "UpdatedAt"   timestamptz  NOT NULL,
    CONSTRAINT "PK_Categories" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "IX_Categories_SiteId_Slug" ON "Categories" ("SiteId", "Slug");

-- =============================================================================
-- TAGS
-- =============================================================================
CREATE TABLE "Tags" (
    "Id"        uuid         NOT NULL,
    "TenantId"  uuid         NOT NULL,
    "SiteId"    uuid         NOT NULL,
    "Name"      varchar(100) NOT NULL,
    "Slug"      varchar(200) NOT NULL,
    "CreatedAt" timestamptz  NOT NULL,
    CONSTRAINT "PK_Tags" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "IX_Tags_SiteId_Slug" ON "Tags" ("SiteId", "Slug");

-- =============================================================================
-- USERS
-- =============================================================================
CREATE TABLE "Users" (
    "Id"                   uuid        NOT NULL,
    "TenantId"             uuid        NOT NULL,
    "Email"                varchar(254) NOT NULL,
    "DisplayName"          varchar(200) NOT NULL,
    "IsActive"             boolean     NOT NULL,
    "CreatedAt"            timestamptz NOT NULL,
    "UpdatedAt"            timestamptz NOT NULL,
    "LastLoginAt"          timestamptz,
    "PasswordHash"         varchar(72),
    "PasswordChangedAt"    timestamptz,
    "FailedLoginAttempts"  integer     NOT NULL DEFAULT 0,
    "LockoutEnd"           timestamptz,
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "IX_Users_TenantId_Email" ON "Users" ("TenantId", "Email");

-- =============================================================================
-- USER ROLES  (owned by User)
-- =============================================================================
CREATE TABLE "UserRoles" (
    "Id"           uuid        NOT NULL,
    "UserId"       uuid        NOT NULL,
    "TenantId"     uuid        NOT NULL,
    "WorkflowRole" varchar(32) NOT NULL,
    "Name"         varchar(100) NOT NULL,
    "SiteId"       uuid,
    "CreatedAt"    timestamptz NOT NULL,
    CONSTRAINT "PK_UserRoles" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_UserRoles_Users_UserId"
        FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_UserRoles_UserId" ON "UserRoles" ("UserId");

-- =============================================================================
-- REFRESH TOKENS
-- =============================================================================
CREATE TABLE "RefreshTokens" (
    "Id"                   uuid        NOT NULL,
    "UserId"               uuid        NOT NULL,
    "TenantId"             uuid        NOT NULL,
    "TokenHash"            varchar(64) NOT NULL,
    "FamilyId"             uuid        NOT NULL,
    "ExpiresAt"            timestamptz NOT NULL,
    "IsRevoked"            boolean     NOT NULL DEFAULT false,
    "CreatedAt"            timestamptz NOT NULL,
    "ReplacedByTokenHash"  varchar(64),
    CONSTRAINT "PK_RefreshTokens" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "IX_RefreshTokens_TokenHash"     ON "RefreshTokens" ("TokenHash");
CREATE        INDEX "IX_RefreshTokens_FamilyId"      ON "RefreshTokens" ("FamilyId");
CREATE        INDEX "IX_RefreshTokens_UserId_IsRevoked" ON "RefreshTokens" ("UserId", "IsRevoked");

-- =============================================================================
-- LOGIN ATTEMPTS
-- =============================================================================
CREATE TABLE "LoginAttempts" (
    "Id"           uuid         NOT NULL,
    "TenantId"     uuid         NOT NULL,
    "Email"        varchar(256) NOT NULL,
    "IsSuccessful" boolean      NOT NULL,
    "IpAddress"    varchar(45),
    "UserAgent"    varchar(512),
    "AttemptedAt"  timestamptz  NOT NULL,
    CONSTRAINT "PK_LoginAttempts" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_LoginAttempts_TenantId_Email_AttemptedAt"
    ON "LoginAttempts" ("TenantId", "Email", "AttemptedAt");

CREATE INDEX "IX_LoginAttempts_AttemptedAt" ON "LoginAttempts" ("AttemptedAt");

-- =============================================================================
-- WEBHOOK SUBSCRIPTIONS
-- =============================================================================
CREATE TABLE "WebhookSubscriptions" (
    "Id"           uuid         NOT NULL,
    "TenantId"     uuid         NOT NULL,
    "SiteId"       uuid,
    "TargetUrl"    varchar(500) NOT NULL,
    "HashedSecret" varchar(256) NOT NULL,
    "IsActive"     boolean      NOT NULL,
    "MaxRetries"   integer      NOT NULL,
    "CreatedAt"    timestamptz  NOT NULL,
    -- Events stored as pipe-separated string via private backing field
    "Events"       varchar(2000) NOT NULL DEFAULT '',
    CONSTRAINT "PK_WebhookSubscriptions" PRIMARY KEY ("Id")
);

-- =============================================================================
-- WEBHOOK DELIVERY LOGS  (owned by WebhookSubscription)
-- =============================================================================
CREATE TABLE "WebhookDeliveryLogs" (
    "WebhookSubscriptionId" uuid         NOT NULL,
    "DeliveredAt"           timestamptz  NOT NULL,
    "EventType"             varchar(200) NOT NULL,
    "StatusCode"            integer      NOT NULL,
    "ErrorMessage"          varchar(1000),
    CONSTRAINT "PK_WebhookDeliveryLogs"
        PRIMARY KEY ("WebhookSubscriptionId", "DeliveredAt"),
    CONSTRAINT "FK_WebhookDeliveryLogs_WebhookSubscriptions_SubId"
        FOREIGN KEY ("WebhookSubscriptionId") REFERENCES "WebhookSubscriptions" ("Id") ON DELETE CASCADE
);

-- =============================================================================
-- OUTBOX MESSAGES
-- =============================================================================
CREATE TABLE "OutboxMessages" (
    "Id"             uuid          NOT NULL,
    "Type"           varchar(512)  NOT NULL,
    "Content"        text          NOT NULL,
    "TenantId"       uuid,
    "OccurredOnUtc"  timestamptz   NOT NULL,
    "ProcessedOnUtc" timestamptz,
    "Error"          varchar(2000),
    "RetryCount"     integer       NOT NULL DEFAULT 0,
    CONSTRAINT "PK_OutboxMessages" PRIMARY KEY ("Id")
);

-- Dispatcher reads oldest unprocessed messages first
CREATE INDEX "IX_OutboxMessages_ProcessedOnUtc_OccurredOnUtc"
    ON "OutboxMessages" ("ProcessedOnUtc", "OccurredOnUtc");

-- Per-tenant dispatch
CREATE INDEX "IX_OutboxMessages_TenantId_ProcessedOnUtc"
    ON "OutboxMessages" ("TenantId", "ProcessedOnUtc");

-- =============================================================================
-- AI PROVIDER SETTINGS
-- =============================================================================
CREATE TABLE "AiProviderSettings" (
    "Id"                                      uuid           NOT NULL,
    "TenantId"                                uuid           NOT NULL,
    "ActiveProvider"                          varchar(100)   NOT NULL,
    "UpdatedAt"                               timestamptz    NOT NULL,
    -- Owned: AiBudget (flattened)
    "Budget_MonthlyCostCapUsd"                numeric(18,4)  NOT NULL,
    "Budget_PerUserDailyTokenCap"             integer        NOT NULL,
    "Budget_HardStop"                         boolean        NOT NULL,
    "Budget_CurrentMonthSpendUsd"             numeric(18,4)  NOT NULL,
    -- Owned: AiSafetyConfig (flattened)
    "Safety_PiiRedactionEnabled"              boolean        NOT NULL,
    "Safety_PromptInjectionDetectionEnabled"  boolean        NOT NULL,
    "Safety_SafetyPostFilterEnabled"          boolean        NOT NULL,
    "Safety_GroundedOnlyModeDefault"          boolean        NOT NULL,
    "Safety_DataResidencyRegion"              varchar(10),
    "Safety_AuditLogRetentionDays"            integer        NOT NULL,
    CONSTRAINT "PK_AiProviderSettings" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "IX_AiProviderSettings_TenantId"
    ON "AiProviderSettings" ("TenantId");

-- =============================================================================
-- AI MODEL TIER OVERRIDES  (owned by AiProviderSettings)
-- =============================================================================
CREATE TABLE "AiModelTierOverrides" (
    "AiProviderSettingsId" uuid         NOT NULL,
    "FeatureKey"           varchar(100) NOT NULL,
    "Model"                varchar(200) NOT NULL,
    CONSTRAINT "PK_AiModelTierOverrides"
        PRIMARY KEY ("AiProviderSettingsId", "FeatureKey"),
    CONSTRAINT "FK_AiModelTierOverrides_AiProviderSettings_AiProviderSettingsId"
        FOREIGN KEY ("AiProviderSettingsId") REFERENCES "AiProviderSettings" ("Id") ON DELETE CASCADE
);

-- =============================================================================
-- COPILOT CONVERSATIONS
-- =============================================================================
CREATE TABLE "CopilotConversations" (
    "Id"                    uuid           NOT NULL,
    "TenantId"              uuid           NOT NULL,
    "UserId"                uuid           NOT NULL,
    "GroundedOnlyMode"      boolean        NOT NULL,
    "TotalPromptTokens"     integer        NOT NULL,
    "TotalCompletionTokens" integer        NOT NULL,
    "TotalCostUsd"          numeric(18,6)  NOT NULL,
    "CreatedAt"             timestamptz    NOT NULL,
    "LastMessageAt"         timestamptz,
    CONSTRAINT "PK_CopilotConversations" PRIMARY KEY ("Id")
);

-- =============================================================================
-- COPILOT MESSAGES  (owned by CopilotConversation)
-- =============================================================================
CREATE TABLE "CopilotMessages" (
    "Id"             uuid        NOT NULL,
    "ConversationId" uuid        NOT NULL,
    "Role"           varchar(32) NOT NULL,
    "Content"        text        NOT NULL,
    "CreatedAt"      timestamptz NOT NULL,
    CONSTRAINT "PK_CopilotMessages" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CopilotMessages_CopilotConversations_ConversationId"
        FOREIGN KEY ("ConversationId") REFERENCES "CopilotConversations" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_CopilotMessages_ConversationId" ON "CopilotMessages" ("ConversationId");

-- =============================================================================
-- COPILOT MESSAGE CITATIONS  (owned by CopilotMessage)
-- =============================================================================
CREATE TABLE "CopilotMessageCitations" (
    "MessageId"       uuid          NOT NULL,
    "EntryId"         uuid          NOT NULL,
    "Slug"            varchar(500)  NOT NULL,
    "Title"           varchar(500)  NOT NULL,
    "SimilarityScore" double precision NOT NULL,
    CONSTRAINT "PK_CopilotMessageCitations" PRIMARY KEY ("MessageId", "EntryId"),
    CONSTRAINT "FK_CopilotMessageCitations_CopilotMessages_MessageId"
        FOREIGN KEY ("MessageId") REFERENCES "CopilotMessages" ("Id") ON DELETE CASCADE
);

-- =============================================================================
-- COMPONENTS
-- =============================================================================
CREATE TABLE "Components" (
    "Id"                   uuid         NOT NULL,
    "TenantId"             uuid         NOT NULL,
    "SiteId"               uuid         NOT NULL,
    "Name"                 varchar(200) NOT NULL,
    "Key"                  varchar(100) NOT NULL,
    "Description"          varchar(500),
    "Category"             varchar(50)  NOT NULL,
    "ZonesJson"            text         NOT NULL DEFAULT '[]',
    "UsageCount"           integer      NOT NULL,
    "ItemCount"            integer      NOT NULL,
    "TemplateType"         varchar(30)  NOT NULL,
    "TemplateContent"      text,
    "ThumbnailDataUri"     text,
    "BackingContentTypeId" uuid,
    "CreatedAt"            timestamptz  NOT NULL,
    "UpdatedAt"            timestamptz  NOT NULL,
    CONSTRAINT "PK_Components" PRIMARY KEY ("Id")
);

-- =============================================================================
-- LAYOUTS
-- =============================================================================
CREATE TABLE "Layouts" (
    "Id"                   uuid         NOT NULL,
    "TenantId"             uuid         NOT NULL,
    "SiteId"               uuid         NOT NULL,
    "Name"                 varchar(200) NOT NULL,
    "Key"                  varchar(100) NOT NULL,
    "TemplateType"         varchar(50)  NOT NULL,
    "ZonesJson"            text         NOT NULL DEFAULT '[]',
    "DefaultPlacementsJson" text        NOT NULL DEFAULT '[]',
    "ShellTemplate"        text,
    "IsShellCustomized"    boolean      NOT NULL DEFAULT false,
    "LayoutConfigJson"     text         NOT NULL DEFAULT '{}',
    "IsDefault"            boolean      NOT NULL,
    "CreatedAt"            timestamptz  NOT NULL,
    "UpdatedAt"            timestamptz  NOT NULL,
    CONSTRAINT "PK_Layouts" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "IX_Layouts_SiteId_Key" ON "Layouts" ("SiteId", "Key");

-- =============================================================================
-- SITE TEMPLATES
-- =============================================================================
CREATE TABLE "SiteTemplates" (
    "Id"             uuid         NOT NULL,
    "TenantId"       uuid         NOT NULL,
    "SiteId"         uuid         NOT NULL,
    "LayoutId"       uuid         NOT NULL,
    "Name"           varchar(200) NOT NULL,
    "Description"    varchar(500),
    "PlacementsJson" text         NOT NULL,
    "CreatedAt"      timestamptz  NOT NULL,
    "UpdatedAt"      timestamptz  NOT NULL,
    CONSTRAINT "PK_SiteTemplates" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_SiteTemplates_TenantId_SiteId" ON "SiteTemplates" ("TenantId", "SiteId");

-- =============================================================================
-- PAGES
-- =============================================================================
CREATE TABLE "Pages" (
    "Id"                      uuid         NOT NULL,
    "TenantId"                uuid         NOT NULL,
    "SiteId"                  uuid         NOT NULL,
    "Title"                   varchar(300) NOT NULL,
    "Slug"                    varchar(200) NOT NULL,
    "PageType"                varchar(32)  NOT NULL,
    "ParentId"                uuid,
    "LinkedEntryId"           uuid,
    "CollectionContentTypeId" uuid,
    "RoutePattern"            varchar(500),
    "Depth"                   integer      NOT NULL,
    "CreatedAt"               timestamptz  NOT NULL,
    "UpdatedAt"               timestamptz  NOT NULL,
    "LayoutId"                uuid,
    -- Owned: SeoMetadata (flattened)
    "SeoMetaTitle"            varchar(60),
    "SeoMetaDescription"      varchar(160),
    "SeoCanonicalUrl"         varchar(500),
    "SeoOgImage"              varchar(500),
    "SiteTemplateId"          uuid,
    CONSTRAINT "PK_Pages" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "IX_Pages_SiteId_Slug"     ON "Pages" ("SiteId", "Slug");
CREATE        INDEX "IX_Pages_SiteId_ParentId" ON "Pages" ("SiteId", "ParentId");

-- =============================================================================
-- PAGE TEMPLATES
-- =============================================================================
CREATE TABLE "PageTemplates" (
    "Id"             uuid        NOT NULL,
    "TenantId"       uuid        NOT NULL,
    "PageId"         uuid        NOT NULL,
    "PlacementsJson" text        NOT NULL DEFAULT '[]',
    "UpdatedAt"      timestamptz NOT NULL,
    CONSTRAINT "PK_PageTemplates" PRIMARY KEY ("Id")
);

-- =============================================================================
-- PAGE TEMPLATE PLACEMENTS  (owned by PageTemplate)
-- =============================================================================
CREATE TABLE "PageTemplatePlacements" (
    "Id"              uuid         NOT NULL,
    "PageTemplateId"  uuid         NOT NULL,
    "ComponentId"     uuid         NOT NULL,
    "Zone"            varchar(200) NOT NULL,
    "SortOrder"       integer      NOT NULL,
    "BoundItemId"     uuid,
    "IsLayoutDefault" boolean      NOT NULL DEFAULT false,
    CONSTRAINT "PK_PageTemplatePlacements" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PageTemplatePlacements_PageTemplates_PageTemplateId"
        FOREIGN KEY ("PageTemplateId") REFERENCES "PageTemplates" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_PageTemplatePlacements_PageTemplateId"
    ON "PageTemplatePlacements" ("PageTemplateId");

-- =============================================================================
-- EDIT LOCKS
-- =============================================================================
CREATE TABLE "EditLocks" (
    "Id"                  uuid         NOT NULL,
    "EntityId"            varchar(200) NOT NULL,
    "EntityType"          varchar(50)  NOT NULL,
    "LockedByUserId"      uuid         NOT NULL,
    "LockedByDisplayName" varchar(200) NOT NULL,
    "LockedAt"            timestamptz  NOT NULL,
    "ExpiresAt"           timestamptz  NOT NULL,
    CONSTRAINT "PK_EditLocks" PRIMARY KEY ("Id")
);

-- Only one lock per entity at a time
CREATE UNIQUE INDEX "IX_EditLocks_EntityId" ON "EditLocks" ("EntityId");

-- =============================================================================
-- PLUGINS
-- =============================================================================
CREATE TABLE "Plugins" (
    "Id"           uuid         NOT NULL,
    "TenantId"     uuid         NOT NULL,
    "Name"         varchar(200) NOT NULL,
    "Version"      varchar(50)  NOT NULL,
    "Author"       varchar(200) NOT NULL,
    "Signature"    varchar(500),
    "IsActive"     boolean      NOT NULL,
    "InstalledAt"  timestamptz  NOT NULL,
    -- Capabilities stored as pipe-separated string via private backing field
    "Capabilities" varchar(4000) NOT NULL DEFAULT '',
    CONSTRAINT "PK_Plugins" PRIMARY KEY ("Id")
);

-- =============================================================================
-- EF CORE MIGRATIONS HISTORY  (required if EF Core is ever used for migrations)
-- =============================================================================
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId"    varchar(150) NOT NULL,
    "ProductVersion" varchar(32)  NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);
