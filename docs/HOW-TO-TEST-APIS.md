# How to Test the MicroCMS APIs

This document walks you through the full lifecycle of authenticating with the MicroCMS API
and calling protected endpoints — using **curl**, **Postman**, or any HTTP client.

---

## Table of Contents

1. [Base URL & API Versioning](#1-base-url--api-versioning)
2. [Authentication Methods](#2-authentication-methods)
3. [Step 1 — First-Run Installation](#3-step-1--first-run-installation)
4. [Step 2 — Obtain a JWT Token (Login)](#4-step-2--obtain-a-jwt-token-login)
5. [Step 3 — Pass the Auth Header](#5-step-3--pass-the-auth-header)
6. [Step 4 — Refresh a Token](#6-step-4--refresh-a-token)
7. [Step 5 — Logout](#7-step-5--logout)
8. [API Key Authentication](#8-api-key-authentication)
9. [Common Workflows](#9-common-workflows)
   - [Create a Site](#91-create-a-site)
   - [List / Get Content Entries](#92-list--get-content-entries)
   - [Create a Content Entry](#93-create-a-content-entry)
   - [Manage API Clients (Delivery Keys)](#94-manage-api-clients-delivery-keys)
10. [Testing with Postman](#10-testing-with-postman)
11. [HTTP Response Codes Reference](#11-http-response-codes-reference)

---

## 1. Base URL & API Versioning

All API routes are prefixed with the API version:

```
http://localhost:5000/api/v1/<controller>
```

Every controller is versioned as `v1.0`. The version is embedded in the URL segment
`v{version:apiVersion}`, so all examples below use `/api/v1/`.

---

## 2. Authentication Methods

MicroCMS supports **two** authentication mechanisms:

| Method | Header | When to Use |
|---|---|---|
| **JWT Bearer Token** | `Authorization: Bearer <access_token>` | Admin / management operations |
| **API Key** | `X-Api-Key: <raw_key>` | Delivery / preview / headless integrations |

JWT Bearer takes priority. If an `Authorization` header is present, the API key middleware
is skipped automatically.

---

## 3. Step 1 — First-Run Installation

Before you can log in, the system must be installed. This is a **one-time, anonymous** call.

### Check installation status

```bash
curl -X GET http://localhost:5000/api/v1/install/status
```

**Response (not yet installed):**
```json
{ "isInstalled": false }
```

### Perform first-run installation

```bash
curl -X POST http://localhost:5000/api/v1/install \
  -H "Content-Type: application/json" \
  -d '{
    "tenantName": "My Company",
    "siteName":   "My Website",
    "adminEmail": "admin@example.com",
    "adminPassword": "P@ssw0rd!"
  }'
```

**Response (201 Created):**
```json
{
  "tenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "siteId":   "yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyy",
  "adminUserId": "zzzzzzzz-zzzz-zzzz-zzzz-zzzzzzzzzzzz"
}
```

> **Note:** Once installed, calling `POST /api/v1/install` again returns `409 Conflict`.

---

## 4. Step 2 — Obtain a JWT Token (Login)

### Endpoint

```
POST /api/v1/auth/login
```

This endpoint is **anonymous** — no token required.

### Request

```bash
curl -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email":    "admin@example.com",
    "password": "P@ssw0rd!"
  }'
```

### Response (200 OK)

```json
{
  "accessToken":  "<JWT>",
  "refreshToken": "<opaque-refresh-token>",
  "expiresIn":    900
}
```

| Field | Description |
|---|---|
| `accessToken` | Short-lived JWT (default: 15 minutes). Pass in `Authorization: Bearer`. |
| `refreshToken` | Long-lived opaque token. Use it to obtain a new access token silently. |
| `expiresIn` | Seconds until the access token expires. |

---

## 5. Step 3 — Pass the Auth Header

Include the `accessToken` as a **Bearer token** in every protected request:

```
Authorization: Bearer <accessToken>
```

### Example — list all entries for a site

```bash
ACCESS_TOKEN="<paste accessToken here>"
SITE_ID="yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyy"

curl -X GET "http://localhost:5000/api/v1/entries?siteId=${SITE_ID}" \
  -H "Authorization: Bearer ${ACCESS_TOKEN}"
```

---

## 6. Step 4 — Refresh a Token

Access tokens expire in 15 minutes (configurable via `TrustedClients:Admin:AccessTokenMinutes`
in `appsettings.json`). Use the refresh token to get a new pair silently.

### Endpoint

```
POST /api/v1/auth/refresh
```

Anonymous — no `Authorization` header needed.

```bash
curl -X POST http://localhost:5000/api/v1/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "<your-refresh-token>"
  }'
```

**Response:** Same shape as `/auth/login` with a fresh `accessToken` and rotated `refreshToken`.

> Each refresh token is **single-use** — a new one is issued on every rotation.

---

## 7. Step 5 — Logout

### Single-device logout (revoke one refresh token)

```bash
curl -X POST http://localhost:5000/api/v1/auth/logout \
  -H "Authorization: Bearer ${ACCESS_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{ "refreshToken": "<your-refresh-token>" }'
```

### All-device logout (revoke all sessions for the current user)

```bash
curl -X POST http://localhost:5000/api/v1/auth/logout-all \
  -H "Authorization: Bearer ${ACCESS_TOKEN}"
```

Both return **204 No Content** on success.

---

## 8. API Key Authentication

API keys are intended for headless delivery integrations. They are managed via the
`/api/v1/api-clients` controller and **require a JWT token to create**.

### Create an API client (management token required)

```bash
curl -X POST http://localhost:5000/api/v1/api-clients \
  -H "Authorization: Bearer ${ACCESS_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "name":    "Production Delivery Key",
    "siteId":  "yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyy",
    "keyType": "Delivery",
    "scopes":  ["content:read"]
  }'
```

**Response (201 Created):**
```json
{
  "id":     "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "rawKey": "mcms_live_xxxxxxxxxxxxxxxxxxxxxxxxxxxx"
}
```

> **Important:** `rawKey` is returned **only once** and never stored in plain text.
> Copy it immediately. If lost, use the `/regenerate` endpoint to issue a new key.

### Use the API key in requests

```bash
curl -X GET "http://localhost:5000/api/v1/entries?siteId=${SITE_ID}" \
  -H "X-Api-Key: mcms_live_xxxxxxxxxxxxxxxxxxxxxxxxxxxx"
```

### Revoke an API key

```bash
curl -X POST http://localhost:5000/api/v1/api-clients/${CLIENT_ID}/revoke \
  -H "Authorization: Bearer ${ACCESS_TOKEN}"
```

### Regenerate an API key

```bash
curl -X POST http://localhost:5000/api/v1/api-clients/${CLIENT_ID}/regenerate \
  -H "Authorization: Bearer ${ACCESS_TOKEN}"
```

### List API clients for a site

```bash
curl -X GET "http://localhost:5000/api/v1/api-clients?siteId=${SITE_ID}" \
  -H "Authorization: Bearer ${ACCESS_TOKEN}"
```

---

## 9. Common Workflows

### 9.1 Create a Site

```bash
curl -X POST http://localhost:5000/api/v1/tenants \
  -H "Authorization: Bearer ${ACCESS_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Acme Corp",
    "handle": "acme"
  }'
```

### 9.2 List / Get Content Entries

**List entries (paginated):**

```bash
curl -X GET "http://localhost:5000/api/v1/entries?siteId=${SITE_ID}&pageNumber=1&pageSize=20" \
  -H "Authorization: Bearer ${ACCESS_TOKEN}"
```

**Filter by content type and locale:**

```bash
curl -X GET "http://localhost:5000/api/v1/entries?siteId=${SITE_ID}&contentTypeId=${CT_ID}&locale=en-US" \
  -H "Authorization: Bearer ${ACCESS_TOKEN}"
```

**Get a single entry:**

```bash
curl -X GET "http://localhost:5000/api/v1/entries/${ENTRY_ID}" \
  -H "Authorization: Bearer ${ACCESS_TOKEN}"
```

**Get entry version history:**

```bash
curl -X GET "http://localhost:5000/api/v1/entries/${ENTRY_ID}/versions" \
  -H "Authorization: Bearer ${ACCESS_TOKEN}"
```

### 9.3 Create a Content Entry

```bash
curl -X POST http://localhost:5000/api/v1/entries \
  -H "Authorization: Bearer ${ACCESS_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "siteId":        "yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyy",
    "contentTypeId": "cccccccc-cccc-cccc-cccc-cccccccccccc",
    "locale":        "en-US",
    "fields": {
      "title": "Hello World",
      "body":  "This is my first entry."
    }
  }'
```

### 9.4 Manage API Clients (Delivery Keys)

See [Section 8](#8-api-key-authentication) above.

---

## 10. Testing with Postman

1. **Import the base URL** — create an environment with variable `BASE_URL = http://localhost:5000`.
2. **Login request** — create a `POST {{BASE_URL}}/api/v1/auth/login` request with a JSON body.
3. **Auto-save the token** — in the *Tests* tab of the login request, add:
   ```javascript
   const json = pm.response.json();
   pm.environment.set("ACCESS_TOKEN",  json.accessToken);
   pm.environment.set("REFRESH_TOKEN", json.refreshToken);
   ```
4. **Set the collection auth** — in Collection → Authorization, choose **Bearer Token** and set
   the token to `{{ACCESS_TOKEN}}`.
5. **All child requests** inherit the Bearer token automatically.
6. **Refresh silently** — add a pre-request script on the collection level that checks
   expiry and calls `POST /auth/refresh` when needed, updating `ACCESS_TOKEN`.

---

## 11. HTTP Response Codes Reference

| Code | Meaning |
|---|---|
| `200 OK` | Request succeeded with a response body. |
| `201 Created` | Resource created; body contains the new resource. |
| `204 No Content` | Request succeeded with no response body (e.g., logout, delete). |
| `400 Bad Request` | Malformed JSON or missing required fields. |
| `401 Unauthorized` | Missing, expired, or invalid token / API key. |
| `403 Forbidden` | Authenticated but lacks the required permission / scope. |
| `404 Not Found` | The requested resource does not exist. |
| `409 Conflict` | Resource already exists (e.g., system already installed). |
| `422 Unprocessable Entity` | Validation failed; `detail` field explains the error. |
| `500 Internal Server Error` | Unexpected server-side error. |

All error responses follow the **RFC 7807 Problem Details** format:

```json
{
  "title":  "Auth.InvalidCredentials",
  "detail": "Email or password is incorrect.",
  "status": 401
}
```

---

## JWT Configuration Reference

The JWT secret, issuer, and audience are configured in
`src/MicroCMS.WebHost/Configuration/appsettings.json` under `TrustedClients:Admin`:

```json
"TrustedClients": {
  "Admin": {
    "Secret":              "5DE7C44A99A04FF4A9241CE81BA3DDF9",
    "Issuer":              "microcms-admin",
    "Audience":            "microcms-api",
    "AccessTokenMinutes":  15
  }
}
```

Override these values (especially `Secret`) in production using environment variables or
a secrets manager. The `Jwt:*` configuration keys are derived automatically from these
values at startup, so there is a single source of truth.
