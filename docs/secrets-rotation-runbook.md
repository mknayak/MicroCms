# MicroCMS — Secrets Rotation Runbook

**Version:** 1.0  
**Last Updated:** 2026-05-16  
**Owner:** Platform / Security Team  
**Review Cadence:** Every 90 days, or immediately after any suspected compromise

---

## 1. Scope

This runbook covers rotation of all secrets used by MicroCMS:

| Secret | Location | Rotation Frequency |
|--------|----------|--------------------|
| JWT signing key (`TrustedClients:Admin:Secret`) | App config / K8s Secret | 90 days |
| Database password (`ConnectionStrings:DefaultConnection`) | App config / K8s Secret | 90 days |
| Redis password (`MicroCMS:Cache:ConnectionString`) | App config / K8s Secret | 90 days |
| API keys (per-tenant, hashed at rest) | Database `ApiClients` table | On request / 365 days |
| HMAC storage signing key (`HmacSigning:Key`) | App config / K8s Secret | 180 days |
| AI provider API key (`ai:api_key` in settings) | `TenantConfigEntries` table | 90 days |
| Vector store API key (`ai:vector_store_api_key`) | `TenantConfigEntries` table | 90 days |
| Webhook HMAC signing keys | `WebhookSubscriptions` table | 180 days |

---

## 2. Pre-Rotation Checklist

Before rotating any secret:

- [ ] Confirm you have write access to the target secret store (K8s Secrets, Azure Key Vault, AWS Secrets Manager).
- [ ] Identify all deployments consuming the secret (`kubectl get deployments -l app.kubernetes.io/name=microcms`).
- [ ] Schedule a maintenance window if rotating the DB password (requires rolling restart).
- [ ] Notify on-call via the incident channel before starting.
- [ ] Confirm you can roll back (keep the old secret value for 15 minutes post-rotation).

---

## 3. JWT Signing Key Rotation

The JWT signing key is symmetric (HS256). Rotation is a two-phase zero-downtime process:

### Phase 1 — Add new key alongside old key

1. Generate a new 256-bit key:
   ```powershell
   [Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }))
   ```

2. Add the new key to configuration as `TrustedClients:AdminNew` with a unique `Issuer` value.

3. Deploy — the dispatch scheme now accepts tokens from **both** issuers.

4. Verify: log in again; confirm the new issuer appears in JWT claims (`iss` field).

### Phase 2 — Remove old key (after all existing tokens expire)

5. Wait for all old tokens to expire (`AccessTokenMinutes` × 2 as buffer, max 30 min).

6. Remove `TrustedClients:Admin` and rename `TrustedClients:AdminNew` → `TrustedClients:Admin`.

7. Deploy. No restart required — configuration reload triggers gracefully.

8. Verify: old tokens are now rejected with 401.

### Rollback

Re-add the old key as a secondary `TrustedClients` entry. All live sessions resume immediately.

---

## 4. Database Password Rotation

1. Create the new password on the PostgreSQL server:
   ```sql
   ALTER USER microcms WITH PASSWORD 'new-strong-password';
   ```

2. Update the K8s Secret:
   ```bash
   kubectl create secret generic microcms-secrets \
     --from-literal=db-connection-string="Host=postgres;Port=5432;Database=microcms_db;Username=microcms;Password=new-strong-password" \
     --dry-run=client -o yaml | kubectl apply -f -
   ```

3. Perform a rolling restart to pick up the new connection string:
   ```bash
   kubectl rollout restart deployment/microcms
   kubectl rollout status deployment/microcms
   ```

4. Verify: `kubectl logs -l app=microcms --tail=50 | grep -i "error\|fail"` — confirm no connection errors.

5. After 15 minutes, revoke the old password on the DB server.

---

## 5. Redis Password Rotation

1. Update the Redis ACL / `requirepass` in the Redis config and reload:
   ```bash
   redis-cli CONFIG SET requirepass "new-redis-password"
   ```

2. Update the K8s Secret (field `redis-connection-string`), then rolling-restart the deployment.

3. Verify with `/health/ready` — the `redis` check must return `Healthy`.

---

## 6. API Key Rotation (Per-Tenant)

API keys are stored as SHA-256 hashes. Rotation requires re-issuing a new key:

1. Call the API key rotation endpoint as `TenantAdmin`:
   ```http
   POST /api/v1/api-keys/{keyId}/rotate
   Authorization: Bearer <admin-jwt>
   ```

2. The response contains the new plaintext key (shown once). Distribute securely to the consumer.

3. The old key is invalidated immediately upon rotation.

4. Confirm the consumer has updated their key before the rotation window closes (24 h).

---

## 7. HMAC Storage Signing Key Rotation

Media signed URLs are HMAC-SHA256 signed. Rotating the key invalidates all existing signed URLs.

> **Warning:** Coordinate with any clients caching signed URLs. Existing URLs become invalid immediately.

1. Generate a new 256-bit HMAC key and update `HmacSigning:Key` in configuration.

2. Deploy (rolling restart).

3. Verify media access still works via the admin UI (a fresh signed URL should resolve).

---

## 8. AI Provider API Key Rotation

AI API keys are stored with `isSecret: true` in `TenantConfigEntries` and are never returned via the API.

1. Rotate the key in the provider's dashboard (OpenAI / Azure / Ollama).

2. Update via the Admin UI → AI Settings → Provider API Key field, or via the config API:
   ```http
   PUT /api/v1/tenants/{tenantId}/config
   { "key": "ai:api_key", "value": "sk-new-key", "isSecret": true }
   ```

3. Test: trigger a test AI completion from the Admin UI drafting panel.

---

## 9. Emergency Rotation (Suspected Compromise)

If a secret is suspected to have been leaked:

1. **Immediately invalidate** the old secret (DB: revoke password; JWT: remove entry from config and restart).
2. **Rotate** following the steps above, skipping the dual-key grace period.
3. **Audit logs**: review the AI audit log (`AiAuditLog` table) and infrastructure logs for anomalous calls.
4. **Notify** affected tenants if their data may have been accessed.
5. **Incident report**: file within 24 hours per the security incident response policy.

---

## 10. Post-Rotation Verification

After any rotation, verify the following:

```bash
# Health check passes
curl -s https://cms.example.com/health/ready | jq .status

# No auth failures in logs
kubectl logs -l app=microcms --since=5m | grep -c "401\|403\|fail"

# Prometheus shows no spike in 5xx errors
# → Grafana dashboard: "API Error Rate" panel
```

---

## 11. Audit Trail

Record every rotation in the team's secrets-rotation log (shared spreadsheet or Confluence):

| Date | Secret | Rotated By | Reason | Verified By |
|------|--------|------------|--------|-------------|
| YYYY-MM-DD | JWT signing key | @engineer | Scheduled 90-day | @reviewer |
