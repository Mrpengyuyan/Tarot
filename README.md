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
