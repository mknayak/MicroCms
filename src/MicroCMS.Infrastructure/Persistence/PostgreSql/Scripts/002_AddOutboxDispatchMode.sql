-- =============================================================================
-- MicroCMS – Migration 002: Outbox Dispatch Mode
--
-- Adds per-instance broadcast delivery support to the transactional outbox.
--
-- Changes:
--   1. OutboxMessages  – new "DispatchMode" column (0 = Exclusive, 1 = Broadcast)
--   2. OutboxMessages  – replaces old dispatcher indexes with DispatchMode-aware ones
--   3. OutboxDeliveryRecords – new table tracking per-instance delivery of Broadcast messages
--
-- Safe to run against an existing database; all changes are additive.
-- Idempotent: wrapped in a DO block that checks before each step.
-- =============================================================================

BEGIN;

-- =============================================================================
-- 1. Add DispatchMode to OutboxMessages
-- =============================================================================
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'OutboxMessages' AND column_name = 'DispatchMode'
    ) THEN
        ALTER TABLE "OutboxMessages"
            ADD COLUMN "DispatchMode" integer NOT NULL DEFAULT 0;

        COMMENT ON COLUMN "OutboxMessages"."DispatchMode" IS
            '0 = Exclusive (any one instance processes); 1 = Broadcast (every instance processes independently)';
    END IF;
END $$;

-- =============================================================================
-- 2. Replace old dispatcher indexes with DispatchMode-aware equivalents
-- =============================================================================

-- Drop old indexes if they still exist
DROP INDEX IF EXISTS "IX_OutboxMessages_ProcessedOnUtc_OccurredOnUtc";
DROP INDEX IF EXISTS "IX_OutboxMessages_TenantId_ProcessedOnUtc";

-- Exclusive dispatcher: fetch oldest unprocessed Exclusive rows
CREATE INDEX IF NOT EXISTS "IX_OutboxMessages_DispatchMode_ProcessedOnUtc_OccurredOnUtc"
    ON "OutboxMessages" ("DispatchMode", "ProcessedOnUtc", "OccurredOnUtc");

-- Per-tenant dispatch
CREATE INDEX IF NOT EXISTS "IX_OutboxMessages_TenantId_DispatchMode_ProcessedOnUtc"
    ON "OutboxMessages" ("TenantId", "DispatchMode", "ProcessedOnUtc");

-- =============================================================================
-- 3. Create OutboxDeliveryRecords table
--    Tracks which host instance has processed each Broadcast outbox message.
--    PK (MessageId, InstanceId) prevents duplicate processing per instance.
-- =============================================================================
CREATE TABLE IF NOT EXISTS "OutboxDeliveryRecords" (
    "MessageId"      uuid                     NOT NULL,
    "InstanceId"     character varying(128)   NOT NULL,
    "ProcessedOnUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_OutboxDeliveryRecords"
        PRIMARY KEY ("MessageId", "InstanceId"),
    CONSTRAINT "FK_OutboxDeliveryRecords_OutboxMessages_MessageId"
        FOREIGN KEY ("MessageId") REFERENCES "OutboxMessages" ("Id") ON DELETE CASCADE
);

COMMENT ON TABLE "OutboxDeliveryRecords" IS
    'Per-instance delivery tracking for Broadcast outbox messages. One row per (message, instance) pair.';

COMMENT ON COLUMN "OutboxDeliveryRecords"."InstanceId" IS
    'Stable host identifier configured via MicroCMS:Outbox:InstanceId (e.g. CM, CD-01, Preview). Falls back to machine name.';

-- Supports reverse look-up: all instances that processed a given message
CREATE INDEX IF NOT EXISTS "IX_OutboxDeliveryRecords_MessageId"
    ON "OutboxDeliveryRecords" ("MessageId");

-- =============================================================================
-- 4. Record migration in EF migrations history
--    Keeps EF Core and manual scripts in sync so "dotnet ef database update" does
--    not try to re-run this migration.
-- =============================================================================
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260509000000_AddOutboxDispatchMode', '8.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;

COMMIT;
