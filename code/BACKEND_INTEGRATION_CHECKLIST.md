# Backend API Integration Checklist

This checklist is for validating the full tarot reading flow:
`auth -> create record -> draw cards -> request interpretation -> read result`,
with request-log verification from `server.log`.

## 1) Preconditions

- Backend is running and reachable (default: `http://localhost:8000/api/v1`).
- Test account exists and can login.
- Tarot seed data exists (`/spreads/` returns non-empty).
- Coze config is set when testing real AI:
  - `COZE_API_KEY`
  - `COZE_BOT_ID`
- Optional but recommended:
  - `ALLOW_MOCK_AI_FALLBACK=false` when verifying real Coze behavior.

## 2) Run Automated Verification

Use the script below for real end-to-end calls and log checks:

```bash
python app/scripts/verify_records_interpret_flow.py \
  --base-url http://localhost:8000/api/v1 \
  --username testuser \
  --password testpass123 \
  --question-type general \
  --poll-attempts 20 \
  --poll-interval 2.0 \
  --interpret-timeout 90 \
  --log-file server.log
```

Notes:

- If your server is started with stdout redirection (for example to `backend.out.log`), pass that file to `--log-file`.
- The verification script disables system proxy for local calls by default, to avoid localhost false `502`.

Output:

- Console summary for each API step.
- JSON report in `app/scripts/reports/flow_report_<trace_id>.json`.

## 3) Manual Step Expectations

### 3.1 Auth and setup

- `POST /login` => `200`, includes `access_token`.
- `GET /spreads/` => `200`, list is non-empty.

### 3.2 Record flow

- `POST /records/` => `200`, returns `id`.
- `POST /records/{id}/draw` => `200`.
- `GET /records/{id}/cards` => `200`, list size > 0.

### 3.3 Interpretation flow

- `POST /records/{id}/interpret?force_ai=true`:
  - expected `200` with interpretation payload when successful;
  - may be `502/503` on upstream AI issues (still indicates endpoint was hit).
- `GET /records/{id}` => `200`.
- `GET /records/{id}/interpretation`:
  - `200` when interpretation is persisted;
  - temporary `404` is acceptable during processing window.

### 3.4 Response schema checks

The final interpretation response should include:

- `overall_interpretation` (required, non-empty)
- Optional: `card_analysis`, `relationship_analysis`, `advice`, `warning`, `summary`
- Optional: `key_themes`

## 4) Log Verification (critical)

In appended `server.log` lines after the test starts, confirm route hits:

- `POST /api/v1/login`
- `GET /api/v1/spreads/`
- `POST /api/v1/records/`
- `POST /api/v1/records/{id}/draw`
- `GET /api/v1/records/{id}/cards`
- `POST /api/v1/records/{id}/interpret`  <-- most important
- `GET /api/v1/records/{id}`
- `GET /api/v1/records/{id}/interpretation` (if polled)

If `/interpret` is missing in logs, the frontend/backend trigger chain is broken before AI call.

## 5) Quick Triage Guide

- Login fails:
  - check credentials, token injection in frontend interceptor.
- Draw/cards fail:
  - check record ownership validation and spread/card seed data.
- Interpret fails with 502/503:
  - inspect Coze credentials, network, and Coze endpoint mode.
- Interpret succeeds but UI empty:
  - verify frontend mapping and detail-page refresh/poll path.
- `/interpret` not hit:
  - verify frontend trigger condition (all cards flipped / manual retry button).

## 6) Pass Criteria

- API flow steps complete without auth/data errors.
- `/records/{id}/interpret` is present in `server.log`.
- Final interpretation is retrievable from `GET /records/{id}/interpretation`
  or present in `GET /records/{id}` detail payload.
