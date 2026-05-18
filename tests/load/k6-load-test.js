/**
 * MicroCMS k6 Load Test
 * Target: 1 000 concurrent virtual users, P95 latency < 500 ms
 *
 * Usage:
 *   k6 run tests/load/k6-load-test.js
 *   k6 run --env BASE_URL=https://cms.example.com tests/load/k6-load-test.js
 *
 * Environment variables:
 *   BASE_URL   - Base URL of the MicroCMS WebHost  (default: http://localhost:8080)
 *   JWT_TOKEN  - Bearer token for authenticated requests (required for content routes)
 */

import http from "k6/http";
import { check, group, sleep } from "k6";
import { Rate, Trend } from "k6/metrics";

// ── Configuration ────────────────────────────────────────────────────────────

const BASE_URL = __ENV.BASE_URL || "http://localhost:8080";
const JWT_TOKEN = __ENV.JWT_TOKEN || "";

const authHeaders = {
  Authorization: JWT_TOKEN ? `Bearer ${JWT_TOKEN}` : "",
  "Content-Type": "application/json",
};

// ── Custom metrics ───────────────────────────────────────────────────────────

const errorRate = new Rate("errors");
const healthLatency = new Trend("health_latency", true);
const metricsLatency = new Trend("metrics_latency", true);
const entriesLatency = new Trend("entries_latency", true);
const searchLatency = new Trend("search_latency", true);

// ── Load stages ───────────────────────────────────────────────────────────────
// Ramp up → steady state at 1 000 VUs → ramp down

export const options = {
  stages: [
    { duration: "1m",  target: 100  },  // warm-up
    { duration: "2m",  target: 500  },  // ramp up
    { duration: "5m",  target: 1000 },  // peak load — 1 000 concurrent users
    { duration: "2m",  target: 500  },  // ramp down
    { duration: "1m",  target: 0    },  // cool-down
  ],

  thresholds: {
    // P95 latency targets (§2.3 of development plan)
    "http_req_duration{group:::Health}":   ["p(95)<200"],   // health/metrics must be < 200 ms
    "http_req_duration{group:::Entries}":  ["p(95)<500"],   // list/get entries < 500 ms
    "http_req_duration{group:::Search}":   ["p(95)<800"],   // search < 800 ms
    "http_req_duration":                   ["p(95)<1000"],  // overall < 1 s
    errors:                                ["rate<0.01"],   // < 1% error rate
  },
};

// ── Scenario helpers ─────────────────────────────────────────────────────────

function checkHealth() {
  group("Health", () => {
    const res = http.get(`${BASE_URL}/health/live`);
    healthLatency.add(res.timings.duration);
    check(res, {
      "health/live 200": (r) => r.status === 200,
      "health body has status": (r) => r.body.includes("Healthy"),
    }) || errorRate.add(1);
  });
}

function checkMetrics() {
  group("Metrics", () => {
    const res = http.get(`${BASE_URL}/metrics`);
    metricsLatency.add(res.timings.duration);
    check(res, {
      "metrics 200": (r) => r.status === 200,
    }) || errorRate.add(1);
  });
}

function listEntries() {
  group("Entries", () => {
    const res = http.get(`${BASE_URL}/api/v1/entries`, { headers: authHeaders });
    entriesLatency.add(res.timings.duration);
    check(res, {
      "entries not 5xx": (r) => r.status < 500,
    }) || errorRate.add(1);
  });
}

function searchEntries() {
  group("Search", () => {
    const res = http.get(`${BASE_URL}/api/v1/search?q=content`, {
      headers: authHeaders,
    });
    searchLatency.add(res.timings.duration);
    check(res, {
      "search not 5xx": (r) => r.status < 500,
    }) || errorRate.add(1);
  });
}

// ── Default function (executed per VU per iteration) ─────────────────────────

export default function () {
  // Distribute load across scenario types
  const scenario = Math.random();

  if (scenario < 0.30) {
    checkHealth();
  } else if (scenario < 0.40) {
    checkMetrics();
  } else if (scenario < 0.80) {
    listEntries();
  } else {
    searchEntries();
  }

  // ~1–2 s think time simulates realistic user pacing
  sleep(1 + Math.random());
}

// ── Smoke test (quick sanity check, no load) ─────────────────────────────────

export function smokeTest() {
  checkHealth();
  checkMetrics();
}
