-- Migration 004: Add AiDailyUsage table for DB-backed AI budget tracking (Sprint 15)
-- Replaces the in-memory BudgetService with persistent per-tenant/user/day counters.

CREATE TABLE IF NOT EXISTS "AiDailyUsage" (
    "TenantId"     UUID           NOT NULL,
    "UserId"       UUID           NOT NULL,
    "Date"         DATE           NOT NULL,
    "TotalTokens"  BIGINT         NOT NULL DEFAULT 0,
    "TotalCostUsd" DECIMAL(18,6)  NOT NULL DEFAULT 0,
    CONSTRAINT "PK_AiDailyUsage" PRIMARY KEY ("TenantId", "UserId", "Date")
);
