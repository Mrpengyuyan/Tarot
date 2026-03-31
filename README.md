# Tarot Project (Flattened)

This folder contains both frontend and backend in a single flat project root.

## Main Paths

- Frontend source: `src/`, `public/`
- Backend source: `app/`
- Frontend deps: `package.json`
- Backend deps: `requirements.txt`
- Backend tests: `tests/`

## Run Frontend

```powershell
npm install
npm start
```

## Run Backend

```powershell
python -m venv .venv
.\.venv\Scripts\activate
pip install -r requirements.txt
uvicorn app.main:app --reload --host 0.0.0.0 --port 8000
```

Startup defaults are conservative:
- `AUTO_CREATE_TABLES_ON_STARTUP=false`
- `AUTO_BOOTSTRAP_REFERENCE_DATA_ON_STARTUP=false`

If you want one-shot local bootstrap on startup, explicitly enable them in `.env`.

## CSRF (Cookie Auth)

This project uses cookie-based auth and enables CSRF protection by default:
- `CSRF_PROTECTION_ENABLED=true`
- `CSRF_COOKIE_NAME=csrf_token`
- `CSRF_HEADER_NAME=X-CSRF-Token`

Frontend requests with unsafe methods (`POST/PUT/PATCH/DELETE`) automatically send `X-CSRF-Token` from cookie.

## Reusable UI Smoke Test

This project includes a browser-driven smoke script that validates the full user path:

1. Login
2. Select a spread
3. Enter a question
4. Draw cards
5. Flip cards
6. Wait for AI interpretation
7. Verify the same-page result and the persisted detail page

Run it with:

```powershell
python scripts/smoke_ui_interpretation.py
```

Or use the npm shortcut:

```powershell
npm run smoke:ui
```

Useful options:

```powershell
python scripts/smoke_ui_interpretation.py --headed
python scripts/smoke_ui_interpretation.py --question "我最近适合换工作吗？"
python scripts/smoke_ui_interpretation.py --spread-index 1
```

Notes:
- The script prefers a local Microsoft Edge install by default.
- You can override the browser path with `SMOKE_BROWSER_PATH`.
- The script expects the frontend and backend to already be running locally.

## LLM Provider (DeepSeek)

Configure these variables in `.env`:

```env
DEEPSEEK_API_KEY=your_deepseek_api_key_here
DEEPSEEK_BASE_URL=https://api.deepseek.com
DEEPSEEK_CHAT_MODEL=deepseek-chat
DEEPSEEK_REASONER_MODEL=deepseek-reasoner
DEEPSEEK_ENABLE_REASONER_FALLBACK=true
AI_REASONER_RATIO_WARMUP_CALLS=20
```

Routing policy in backend:
- Primary route: `deepseek-chat`
- Conditional fallback: `deepseek-reasoner`

## Production AI Parameters

Recommended runtime policy:

```env
DEEPSEEK_TIMEOUT=55
DEEPSEEK_REASONER_TRIGGER_CHARS=180
DEEPSEEK_ENABLE_REASONER_FALLBACK=true
AI_REASONER_RATIO_WARMUP_CALLS=20

AI_MAX_RETRIES=2
AI_RETRY_BACKOFF_MS=800
AI_RETRY_BACKOFF_FACTOR=2.0
AI_RETRY_MAX_BACKOFF_MS=5000
AI_RETRY_ON_TIMEOUT=true
AI_RETRY_ON_429=true
AI_RETRY_ON_5XX=true
AI_MAX_OUTPUT_TOKENS=900

AI_BUDGET_GUARD_ENABLED=true
AI_DAILY_BUDGET_USD=5
AI_MONTHLY_BUDGET_USD=120
AI_REQUEST_SOFT_CAP_USD=0.0035
AI_REASONER_MAX_PERCENT=0.20
AI_DISABLE_REASONER_WHEN_BUDGET_HIGH=true
AI_BUDGET_ALERT_THRESHOLD=0.85
```

Notes:
- `AI_BUDGET_GUARD_ENABLED` uses process-local counters. Keep it `false` for multi-instance deployment unless you provide a centralized budget store.
- In Docker Compose, backend `DATABASE_URL` is overridden to Postgres automatically.
- OpenAPI docs at `/docs` are available only when `DEBUG=true`.
- Production builds disable source map generation via `.env.production` to avoid third-party source map warnings from `@mediapipe/tasks-vision`.

## Scope Boundary

This repository is an application project (frontend + backend service). It does not contain model training/checkpoint pipelines or paper-style offline evaluation workflows.
