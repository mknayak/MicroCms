-- Migration: AddContentTypeParent
-- Target: SQLite
-- Generated from: 20260508074915_AddContentTypeParent

-- Up Migration

ALTER TABLE "ContentTypes"
    ADD COLUMN "ParentContentTypeId" TEXT NULL
    REFERENCES "ContentTypes" ("Id") ON DELETE SET NULL;

CREATE INDEX "IX_ContentTypes_ParentContentTypeId"
    ON "ContentTypes" ("ParentContentTypeId");

-- Down Migration (run manually to revert)
-- SQLite does not support DROP COLUMN directly in older versions.
-- For SQLite 3.35.0+ you can use:
--
--   DROP INDEX IF EXISTS "IX_ContentTypes_ParentContentTypeId";
--   ALTER TABLE "ContentTypes" DROP COLUMN "ParentContentTypeId";
--
-- For older SQLite versions, recreate the table without the column:
--
-- BEGIN TRANSACTION;
--
-- CREATE TABLE "ContentTypes_backup" AS
--     SELECT <all columns except ParentContentTypeId>
--     FROM "ContentTypes";
--
-- DROP TABLE "ContentTypes";
-- ALTER TABLE "ContentTypes_backup" RENAME TO "ContentTypes";
--
-- COMMIT;
