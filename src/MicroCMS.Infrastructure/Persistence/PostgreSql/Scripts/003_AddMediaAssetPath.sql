
-- =============================================================================
-- MicroCMS – Migration 003: Media Asset Virtual Path
--
-- Enables human-readable, cacheable URLs for uploaded media assets.
--
-- Changes:
--   1. MediaAssets  – new nullable "AssetPath" column (varchar 500)
--   2. MediaAssets  – unique partial index on "AssetPath" (NULL rows excluded)
--
-- Once set, an asset is reachable at  GET /static/assets/{AssetPath}
-- with Cache-Control: public, max-age=31536000 instead of the GUID-based
-- /api/v1/media/{id}/download endpoint.
--
-- Safe to run against an existing database; all changes are additive.
-- Existing rows receive NULL for "AssetPath" and keep their GUID URLs.
-- Idempotent: wrapped in DO blocks that check before each step.
-- =============================================================================

BEGIN;

-- =============================================================================
-- 1. Add AssetPath column to MediaAssets
-- =============================================================================
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'MediaAssets' AND column_name = 'AssetPath'
    ) THEN
        ALTER TABLE "MediaAssets"
            ADD COLUMN "AssetPath" varchar(500) NULL;

        COMMENT ON COLUMN "MediaAssets"."AssetPath" IS
            'Optional virtual path (e.g. css/main.css). When set the asset is served at '
            '/static/assets/{AssetPath} with long-lived browser caching. Globally unique across all tenants.';
    END IF;
END $$;

-- =============================================================================
-- 2. Unique partial index — paths are unique per site.
--    Two sites (even in the same tenant) may both use "css/main.css"; NULL rows
--    are excluded so assets without a public path are unconstrained.
-- =============================================================================
CREATE UNIQUE INDEX IF NOT EXISTS "IX_MediaAssets_TenantId_SiteId_AssetPath"
    ON "MediaAssets" ("TenantId", "SiteId", "AssetPath")
    WHERE "AssetPath" IS NOT NULL;

-- =============================================================================
-- 3. Record migration in EF migrations history
--    Keeps EF Core and manual scripts in sync so "dotnet ef database update"
--    does not try to re-run this migration.
-- =============================================================================
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260510000000_AddMediaAssetPath', '8.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;

COMMIT;
