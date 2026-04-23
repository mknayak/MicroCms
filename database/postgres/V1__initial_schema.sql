-- =============================================================================
-- MicroCMS — PostgreSQL Initial Schema
-- Version : V1
-- Target  : PostgreSQL 15+
-- Apply   : psql -U <user> -d <database> -f V1__initial_schema.sql
--           or use Flyway / Liquibase with this file as-is.
-- Notes   :
--   • All PKs are UUID (application-generated).
--   • Timestamps are TIMESTAMPTZ — always UTC.
--   • JSON columns use TEXT; cast to ::jsonb at query time where needed.
--   • tenant_id is the leading column on every composite index for row-level isolation.
-- =============================================================================

-- ---------------------------------------------------------------------------
-- 1. Tenants
-- ---------------------------------------------------------------------------
CREATE TABLE tenants (
    id      UUID         NOT NULL,
    slug      VARCHAR(63)  NOT NULL,
    status         VARCHAR(32)  NOT NULL,
    settings_display_name VARCHAR(200) NOT NULL,
    settings_default_locale     VARCHAR(35)  NOT NULL,
    settings_enabled_locales    VARCHAR(1024) NOT NULL,
    settings_time_zone_id       VARCHAR(64)  NOT NULL,
 settings_ai_enabled     BOOLEAN      NOT NULL DEFAULT FALSE,
    settings_logo_url  VARCHAR(2048),
    quota_max_storage_bytes     BIGINT       NOT NULL DEFAULT 0,
    quota_max_api_calls_per_min INT          NOT NULL DEFAULT 0,
    quota_max_users             INT       NOT NULL DEFAULT 0,
    quota_max_sites   INT          NOT NULL DEFAULT 0,
    quota_max_content_types     INT          NOT NULL DEFAULT 0,
    quota_max_ai_tokens_month   BIGINT       NOT NULL DEFAULT 0,
    created_at    TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at        TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_tenants        PRIMARY KEY (id),
    CONSTRAINT uq_tenants_slug   UNIQUE (slug)
);

-- ---------------------------------------------------------------------------
-- 2. TenantSecuritySettings
-- ---------------------------------------------------------------------------
CREATE TABLE tenant_security_settings (
    id      UUID        NOT NULL,
    tenant_id       UUID   NOT NULL,
    mfa_required            BOOLEAN     NOT NULL DEFAULT FALSE,
    allowed_ip_ranges       TEXT,    -- JSON array of CIDR strings
    session_timeout_minutes INT      NOT NULL DEFAULT 30,
    max_failed_logins    INT         NOT NULL DEFAULT 5,
    created_at       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_tenant_security_settings PRIMARY KEY (id),
CONSTRAINT fk_tss_tenant  FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON DELETE CASCADE,
    CONSTRAINT uq_tss_tenant UNIQUE (tenant_id)
);

CREATE INDEX ix_tss_tenant_id ON tenant_security_settings (tenant_id);

-- ---------------------------------------------------------------------------
-- 3. Sites
-- ---------------------------------------------------------------------------
CREATE TABLE sites (
    id   UUID         NOT NULL,
    tenant_id      UUID         NOT NULL,
    name      VARCHAR(100) NOT NULL,
    handle         VARCHAR(200) NOT NULL,
    default_locale VARCHAR(35)  NOT NULL,
    custom_domain  VARCHAR(253),
  is_active      BOOLEAN NOT NULL DEFAULT TRUE,
    created_at     TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_sites     PRIMARY KEY (id),
    CONSTRAINT fk_sites_tenant FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON DELETE CASCADE,
    CONSTRAINT uq_sites_tenant_handle UNIQUE (tenant_id, handle)
);

CREATE INDEX ix_sites_tenant_id ON sites (tenant_id);

-- ---------------------------------------------------------------------------
-- 4. SiteSettings
-- ---------------------------------------------------------------------------
CREATE TABLE site_settings (
    id    UUID         NOT NULL,
    tenant_id UUID         NOT NULL,
    site_id          UUID         NOT NULL,
    preview_secret   VARCHAR(128) NOT NULL,
    robots_txt  TEXT,
    custom_head_html TEXT,
    created_at       TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at       TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_site_settings PRIMARY KEY (id),
    CONSTRAINT fk_ss_site FOREIGN KEY (site_id) REFERENCES sites (id) ON DELETE CASCADE,
    CONSTRAINT uq_ss_site       UNIQUE (site_id)
);

CREATE INDEX ix_ss_tenant_id ON site_settings (tenant_id);

-- ---------------------------------------------------------------------------
-- 5. ApiClients
-- ---------------------------------------------------------------------------
CREATE TABLE api_clients (
    id            UUID         NOT NULL,
    tenant_id     UUID    NOT NULL,
    site_id       UUID         NOT NULL,
    name      VARCHAR(200) NOT NULL,
    key_type      VARCHAR(32)  NOT NULL,
    hashed_secret VARCHAR(512) NOT NULL,
    scopes        TEXT         NOT NULL DEFAULT '[]',   -- JSON array of scope strings
    is_active BOOLEAN      NOT NULL DEFAULT TRUE,
    expires_at    TIMESTAMPTZ,
    created_at    TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_api_clients      PRIMARY KEY (id),
    CONSTRAINT fk_api_clients_site FOREIGN KEY (site_id) REFERENCES sites (id) ON DELETE CASCADE
);

CREATE INDEX ix_api_clients_tenant_id ON api_clients (tenant_id);
CREATE INDEX ix_api_clients_site_id   ON api_clients (site_id);

-- ---------------------------------------------------------------------------
-- 6. ContentTypes
-- ---------------------------------------------------------------------------
CREATE TABLE content_types (
    id           UUID         NOT NULL,
    tenant_id    UUID         NOT NULL,
    site_id UUID         NOT NULL,
    handle       VARCHAR(64)  NOT NULL,
    display_name VARCHAR(200) NOT NULL,
    description  VARCHAR(500),
    status       VARCHAR(32)  NOT NULL,
    created_at   TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at   TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_content_types         PRIMARY KEY (id),
    CONSTRAINT uq_content_types_site_handle  UNIQUE (site_id, handle)
);

CREATE INDEX ix_content_types_tenant_id ON content_types (tenant_id);

-- ---------------------------------------------------------------------------
-- 7. ContentTypeFields
-- ---------------------------------------------------------------------------
CREATE TABLE content_type_fields (
    id    UUID   NOT NULL,
    content_type_id UUID         NOT NULL,
    handle VARCHAR(64)  NOT NULL,
    label         VARCHAR(200) NOT NULL,
    field_type      VARCHAR(32)  NOT NULL,
    is_required     BOOLEAN      NOT NULL DEFAULT FALSE,
    is_localized    BOOLEAN      NOT NULL DEFAULT FALSE,
    is_unique     BOOLEAN      NOT NULL DEFAULT FALSE,
  sort_order  INT NOT NULL DEFAULT 0,
    description     VARCHAR(500),
    validation_json TEXT,
 CONSTRAINT pk_content_type_fields   PRIMARY KEY (id),
    CONSTRAINT fk_ctf_content_type             FOREIGN KEY (content_type_id) REFERENCES content_types (id) ON DELETE CASCADE,
    CONSTRAINT uq_ctf_content_type_handle      UNIQUE (content_type_id, handle)
);

CREATE INDEX ix_ctf_content_type_id ON content_type_fields (content_type_id);

-- ---------------------------------------------------------------------------
-- 8. Folders  (content organisation folders)
-- ---------------------------------------------------------------------------
CREATE TABLE folders (
    id    UUID         NOT NULL,
    tenant_id    UUID      NOT NULL,
    site_id          UUID      NOT NULL,
    name          VARCHAR(200) NOT NULL,
    parent_folder_id UUID,
    created_at       TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_folders        PRIMARY KEY (id),
    CONSTRAINT fk_folders_parent FOREIGN KEY (parent_folder_id) REFERENCES folders (id) ON DELETE SET NULL
);

CREATE INDEX ix_folders_tenant_id          ON folders (tenant_id);
CREATE INDEX ix_folders_tenant_site_parent ON folders (tenant_id, site_id, parent_folder_id);

-- ---------------------------------------------------------------------------
-- 9. Entries
-- ---------------------------------------------------------------------------
CREATE TABLE entries (
    id          UUID      NOT NULL,
    tenant_id  UUID         NOT NULL,
    site_id       UUID NOT NULL,
    content_type_id        UUID  NOT NULL,
    slug          VARCHAR(200) NOT NULL,
 locale      VARCHAR(35)  NOT NULL,
    author_id              UUID         NOT NULL,
folder_id              UUID,
 status             VARCHAR(32)  NOT NULL,
    current_version_number INT          NOT NULL DEFAULT 1,
    fields_json            TEXT         NOT NULL DEFAULT '{}',
    created_at   TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at             TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    published_at           TIMESTAMPTZ,
    scheduled_publish_at TIMESTAMPTZ,
    scheduled_unpublish_at TIMESTAMPTZ,
    CONSTRAINT pk_entries        PRIMARY KEY (id),
  CONSTRAINT uq_entries_site_locale_slug UNIQUE (site_id, locale, slug),
    CONSTRAINT fk_entries_content_type     FOREIGN KEY (content_type_id) REFERENCES content_types (id),
    CONSTRAINT fk_entries_folder        FOREIGN KEY (folder_id) REFERENCES folders (id) ON DELETE SET NULL
);

CREATE INDEX ix_entries_tenant_id             ON entries (tenant_id);
CREATE INDEX ix_entries_tenant_site_ct_status ON entries (tenant_id, site_id, content_type_id, status);
CREATE INDEX ix_entries_tenant_folder         ON entries (tenant_id, folder_id);
CREATE INDEX ix_entries_scheduled_publish     ON entries (scheduled_publish_at)
    WHERE scheduled_publish_at IS NOT NULL;

-- ---------------------------------------------------------------------------
-- 10. EntryVersions
-- ---------------------------------------------------------------------------
CREATE TABLE entry_versions (
    id    UUID     NOT NULL,
    entry_id UUID     NOT NULL,
    version_number INT          NOT NULL,
    fields_json TEXT       NOT NULL DEFAULT '{}',
    author_id      UUID   NOT NULL,
 change_note    VARCHAR(500),
    created_at     TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_entry_versions        PRIMARY KEY (id),
    CONSTRAINT uq_entry_versions_entry_version UNIQUE (entry_id, version_number),
    CONSTRAINT fk_ev_entry         FOREIGN KEY (entry_id) REFERENCES entries (id) ON DELETE CASCADE
);

CREATE INDEX ix_entry_versions_entry_id ON entry_versions (entry_id, version_number DESC);

-- ---------------------------------------------------------------------------
-- 11. Pages
-- ---------------------------------------------------------------------------
CREATE TABLE pages (
    id      UUID         NOT NULL,
    tenant_id         UUID NOT NULL,
    site_id          UUID         NOT NULL,
    title   VARCHAR(300) NOT NULL,
    slug          VARCHAR(200) NOT NULL,
    page_type           VARCHAR(32)  NOT NULL,
    parent_id                  UUID,
  linked_entry_id UUID,
  collection_content_type_id UUID,
    route_pattern     VARCHAR(500),
    depth               INT          NOT NULL DEFAULT 0,
    created_at    TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at         TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_pages   PRIMARY KEY (id),
    CONSTRAINT fk_pages_parent FOREIGN KEY (parent_id) REFERENCES pages (id) ON DELETE SET NULL
);

CREATE INDEX ix_pages_tenant_id    ON pages (tenant_id);
CREATE INDEX ix_pages_tenant_site  ON pages (tenant_id, site_id);

-- ---------------------------------------------------------------------------
-- 12. MediaFolders
-- ---------------------------------------------------------------------------
CREATE TABLE media_folders (
    id       UUID         NOT NULL,
    tenant_id        UUID         NOT NULL,
    site_id      UUID NOT NULL,
    name      VARCHAR(200) NOT NULL,
    parent_folder_id UUID,
    created_at       TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_media_folders        PRIMARY KEY (id),
    CONSTRAINT fk_media_folders_parent FOREIGN KEY (parent_folder_id) REFERENCES media_folders (id) ON DELETE SET NULL
);

CREATE INDEX ix_media_folders_tenant_site_parent ON media_folders (tenant_id, site_id, parent_folder_id);

-- ---------------------------------------------------------------------------
-- 13. MediaAssets
-- ---------------------------------------------------------------------------
CREATE TABLE media_assets (
    id   UUID  NOT NULL,
    tenant_id        UUID NOT NULL,
    site_id UUID          NOT NULL,
    storage_key      VARCHAR(1024) NOT NULL,
    folder_id        UUID,
    uploaded_by      UUID  NOT NULL,
    status   VARCHAR(32) NOT NULL,
    alt_text       VARCHAR(500),
    tags     VARCHAR(2000) NOT NULL DEFAULT '',
    meta_file_name   VARCHAR(255)  NOT NULL,
    meta_mime_type   VARCHAR(127)  NOT NULL,
  meta_size_bytes  BIGINT        NOT NULL,
    meta_width_px    INT,
    meta_height_px   INT,
    meta_duration    INTERVAL,
    meta_exif_json   TEXT NOT NULL DEFAULT '{}',
    created_at TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
 updated_at       TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_media_assets        PRIMARY KEY (id),
  CONSTRAINT fk_media_assets_folder FOREIGN KEY (folder_id) REFERENCES media_folders (id) ON DELETE SET NULL
);

CREATE INDEX ix_media_assets_tenant_id     ON media_assets (tenant_id);
CREATE INDEX ix_media_assets_tenant_site   ON media_assets (tenant_id, site_id);
CREATE INDEX ix_media_assets_tenant_folder ON media_assets (tenant_id, folder_id);
CREATE INDEX ix_media_assets_status        ON media_assets (tenant_id, status);

-- ---------------------------------------------------------------------------
-- 14. Categories
-- ---------------------------------------------------------------------------
CREATE TABLE categories (
    id          UUID         NOT NULL,
    tenant_id   UUID   NOT NULL,
    site_id   UUID         NOT NULL,
    name        VARCHAR(200) NOT NULL,
    slug      VARCHAR(200) NOT NULL,
    parent_id   UUID,
    description VARCHAR(500),
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_categories      PRIMARY KEY (id),
    CONSTRAINT uq_categories_site_slug UNIQUE (site_id, slug),
    CONSTRAINT fk_categories_parent  FOREIGN KEY (parent_id) REFERENCES categories (id) ON DELETE SET NULL
);

CREATE INDEX ix_categories_tenant_id ON categories (tenant_id);

-- ---------------------------------------------------------------------------
-- 15. Tags
-- ---------------------------------------------------------------------------
CREATE TABLE tags (
  id    UUID   NOT NULL,
    tenant_id  UUID         NOT NULL,
 site_id    UUID    NOT NULL,
    name    VARCHAR(100) NOT NULL,
 slug       VARCHAR(200) NOT NULL,
    created_at TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_tags      PRIMARY KEY (id),
    CONSTRAINT uq_tags_site_slug  UNIQUE (site_id, slug)
);

CREATE INDEX ix_tags_tenant_id ON tags (tenant_id);

-- ---------------------------------------------------------------------------
-- 16. Users
-- ---------------------------------------------------------------------------
CREATE TABLE users (
    id           UUID         NOT NULL,
    tenant_id    UUID    NOT NULL,
    email        VARCHAR(254) NOT NULL,
    display_name VARCHAR(200) NOT NULL,
    pwd_hash     VARCHAR(256),
    mfa_secret   VARCHAR(128),
    is_active    BOOLEAN    NOT NULL DEFAULT TRUE,
    created_at   TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at   TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_users   PRIMARY KEY (id),
    CONSTRAINT uq_users_tenant_email UNIQUE (tenant_id, email)
);

CREATE INDEX ix_users_tenant_id ON users (tenant_id);

-- ---------------------------------------------------------------------------
-- 17. UserRoles
-- ---------------------------------------------------------------------------
CREATE TABLE user_roles (
    id            UUID NOT NULL,
    user_id       UUID  NOT NULL,
  tenant_id     UUID         NOT NULL,
    workflow_role VARCHAR(32)  NOT NULL,
    name          VARCHAR(100) NOT NULL,
 site_id       UUID,
 created_at    TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_user_roles      PRIMARY KEY (id),
    CONSTRAINT fk_user_roles_user FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
);

CREATE INDEX ix_user_roles_user_id   ON user_roles (user_id);
CREATE INDEX ix_user_roles_tenant_id ON user_roles (tenant_id);

-- ---------------------------------------------------------------------------
-- 18. RefreshTokens
-- ---------------------------------------------------------------------------
CREATE TABLE refresh_tokens (
    id           UUID   NOT NULL,
    user_id          UUID         NOT NULL,
    tenant_id    UUID         NOT NULL,
    token_hash         VARCHAR(128) NOT NULL,
    family_id              UUID    NOT NULL,
    expires_at  TIMESTAMPTZ  NOT NULL,
    is_revoked      BOOLEAN NOT NULL DEFAULT FALSE,
    replaced_by_token_hash VARCHAR(128),
    created_at       TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
 CONSTRAINT pk_refresh_tokens      PRIMARY KEY (id),
    CONSTRAINT uq_refresh_token_hash  UNIQUE (token_hash),
    CONSTRAINT fk_refresh_tokens_user FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
);

CREATE INDEX ix_refresh_tokens_user_id   ON refresh_tokens (user_id);
CREATE INDEX ix_refresh_tokens_family_id ON refresh_tokens (family_id);
CREATE INDEX ix_refresh_tokens_tenant_id ON refresh_tokens (tenant_id);

-- ---------------------------------------------------------------------------
-- 19. LoginAttempts
-- ---------------------------------------------------------------------------
CREATE TABLE login_attempts (
    id     UUID      NOT NULL,
    tenant_id     UUID     NOT NULL,
    email      VARCHAR(254) NOT NULL,
  is_successful BOOLEAN      NOT NULL,
    ip_address    VARCHAR(45),
    user_agent    VARCHAR(512),
    attempted_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_login_attempts PRIMARY KEY (id)
);

-- Partial index: only failed recent attempts matter for lockout evaluation
CREATE INDEX ix_login_attempts_tenant_email_failed
    ON login_attempts (tenant_id, email, attempted_at DESC)
    WHERE is_successful = FALSE;

-- ---------------------------------------------------------------------------
-- 20. WebhookSubscriptions
-- ---------------------------------------------------------------------------
CREATE TABLE webhook_subscriptions (
    id         UUID         NOT NULL,
    tenant_id     UUID         NOT NULL,
    site_id       UUID,
    target_url    VARCHAR(500) NOT NULL,
    hashed_secret VARCHAR(512) NOT NULL,
    events        TEXT         NOT NULL DEFAULT '[]',   -- JSON array
    is_active     BOOLEAN      NOT NULL DEFAULT TRUE,
    max_retries   INT  NOT NULL DEFAULT 3,
    delivery_logs TEXT         NOT NULL DEFAULT '[]',   -- JSON array (capped at 50 entries)
    created_at    TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_webhook_subscriptions PRIMARY KEY (id)
);

CREATE INDEX ix_webhook_subscriptions_tenant_id ON webhook_subscriptions (tenant_id);

-- ---------------------------------------------------------------------------
-- 21. OutboxMessages
-- ---------------------------------------------------------------------------
CREATE TABLE outbox_messages (
    id               UUID          NOT NULL,
    type           VARCHAR(512)  NOT NULL,
 content          TEXT    NOT NULL,
    tenant_id        UUID,
    occurred_on_utc  TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    processed_on_utc TIMESTAMPTZ,
    error        VARCHAR(2000),
    retry_count      INT        NOT NULL DEFAULT 0,
    CONSTRAINT pk_outbox_messages PRIMARY KEY (id)
);

-- Partial index: dispatcher queries only unprocessed messages
CREATE INDEX ix_outbox_unprocessed
    ON outbox_messages (occurred_on_utc)
    WHERE processed_on_utc IS NULL;

CREATE INDEX ix_outbox_tenant_processed ON outbox_messages (tenant_id, processed_on_utc);

-- ---------------------------------------------------------------------------
-- 22. Components
-- ---------------------------------------------------------------------------
CREATE TABLE components (
    id          UUID    NOT NULL,
    tenant_id   UUID         NOT NULL,
    site_id     UUID   NOT NULL,
    name        VARCHAR(200) NOT NULL,
    zone        VARCHAR(100) NOT NULL,
usage_count INT          NOT NULL DEFAULT 0,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_components PRIMARY KEY (id)
);

CREATE INDEX ix_components_tenant_id ON components (tenant_id);

CREATE TABLE component_fields (
    id        UUID         NOT NULL,
    component_id    UUID         NOT NULL,
    handle          VARCHAR(64)  NOT NULL,
    label VARCHAR(200) NOT NULL,
    field_type      VARCHAR(32)  NOT NULL,
    is_required     BOOLEAN      NOT NULL DEFAULT FALSE,
    is_localized    BOOLEAN   NOT NULL DEFAULT FALSE,
    is_unique       BOOLEAN      NOT NULL DEFAULT FALSE,
    sort_order    INT          NOT NULL DEFAULT 0,
    description     VARCHAR(500),
    validation_json TEXT,
    CONSTRAINT pk_component_fields         PRIMARY KEY (id),
    CONSTRAINT fk_cf_component             FOREIGN KEY (component_id) REFERENCES components (id) ON DELETE CASCADE,
    CONSTRAINT uq_cf_component_handle      UNIQUE (component_id, handle)
);

CREATE TABLE component_items (
    id UUID   NOT NULL,
    component_id UUID      NOT NULL,
    tenant_id    UUID        NOT NULL,
    site_id      UUID        NOT NULL,
    fields_json  TEXT     NOT NULL DEFAULT '{}',
    created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_component_items  PRIMARY KEY (id),
    CONSTRAINT fk_ci_component     FOREIGN KEY (component_id) REFERENCES components (id) ON DELETE CASCADE
);

CREATE INDEX ix_component_items_tenant_id ON component_items (tenant_id);

-- ---------------------------------------------------------------------------
-- 23. PageTemplates
-- ---------------------------------------------------------------------------
CREATE TABLE page_templates (
    id    UUID   NOT NULL,
    tenant_id   UUID   NOT NULL,
    site_id     UUID         NOT NULL,
    name        VARCHAR(200) NOT NULL,
    handle      VARCHAR(64)  NOT NULL,
    schema_json TEXT         NOT NULL DEFAULT '{}',
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_page_templates   PRIMARY KEY (id),
    CONSTRAINT uq_page_templates_site_handle UNIQUE (site_id, handle)
);

CREATE INDEX ix_page_templates_tenant_id ON page_templates (tenant_id);

-- ---------------------------------------------------------------------------
-- 24. Plugins
-- ---------------------------------------------------------------------------
CREATE TABLE plugins (
    id           UUID  NOT NULL,
    tenant_id    UUID         NOT NULL,
    name         VARCHAR(200) NOT NULL,
    version      VARCHAR(64)  NOT NULL,
    author    VARCHAR(200) NOT NULL,
    signature    VARCHAR(512),
    capabilities TEXT         NOT NULL DEFAULT '[]',   -- JSON array
    is_active  BOOLEAN      NOT NULL DEFAULT TRUE,
    installed_at TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
  CONSTRAINT pk_plugins PRIMARY KEY (id)
);

CREATE INDEX ix_plugins_tenant_id ON plugins (tenant_id);

-- ---------------------------------------------------------------------------
-- 25. CopilotConversations
-- ---------------------------------------------------------------------------
CREATE TABLE copilot_conversations (
    id   UUID  NOT NULL,
    tenant_id       UUID          NOT NULL,
    user_id      UUID   NOT NULL,
    grounded_only_mode      BOOLEAN    NOT NULL DEFAULT FALSE,
    total_prompt_tokens     INT           NOT NULL DEFAULT 0,
    total_completion_tokens INT     NOT NULL DEFAULT 0,
    total_cost_usd  NUMERIC(10,6) NOT NULL DEFAULT 0,
    messages_json    TEXT          NOT NULL DEFAULT '[]',   -- serialised CopilotMessage[]
    created_at              TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    last_message_at  TIMESTAMPTZ,
    CONSTRAINT pk_copilot_conversations PRIMARY KEY (id)
);

CREATE INDEX ix_copilot_conversations_tenant_user
    ON copilot_conversations (tenant_id, user_id, created_at DESC);

-- ---------------------------------------------------------------------------
-- 26. AiProviderSettings
-- ---------------------------------------------------------------------------
CREATE TABLE ai_provider_settings (
    id            UUID NOT NULL,
    tenant_id     UUID          NOT NULL,
    provider_name VARCHAR(64)   NOT NULL,
    model_tier VARCHAR(32)   NOT NULL,
    api_key_enc   TEXT,            -- application-level encrypted; NULL = use system key
    endpoint_url  VARCHAR(2048),
    is_enabled BOOLEAN       NOT NULL DEFAULT TRUE,
  settings_json TEXT          NOT NULL DEFAULT '{}',
    created_at    TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    updated_at    TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_ai_provider_settings      PRIMARY KEY (id),
    CONSTRAINT uq_ai_provider_tenant_name   UNIQUE (tenant_id, provider_name, model_tier)
);

CREATE INDEX ix_ai_provider_settings_tenant_id ON ai_provider_settings (tenant_id);

-- ---------------------------------------------------------------------------
-- EF Core migration history  (required if you ever switch to EF migrations)
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId"VARCHAR(150) NOT NULL,
    "ProductVersion" VARCHAR(32)  NOT NULL,
    CONSTRAINT pk_ef_migrations_history PRIMARY KEY ("MigrationId")
);
