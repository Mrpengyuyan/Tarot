# Tarot Game (Full Stack)

This repository contains a complete Tarot game project:

- `code/` -> FastAPI backend (auth, records, AI interpretation integration, tests)
- `tarot-frontend/` -> React + TypeScript frontend (draw cards, 3D flip, interpretation UI)

## Project Structure

```text
.
├─ code/
│  ├─ app/
│  ├─ tests/
│  └─ requirements.txt
└─ tarot-frontend/
   ├─ src/
   └─ package.json
```

## Quick Start

### 1) Backend

```bash
cd code
python -m pip install -r requirements.txt
uvicorn app.main:app --reload
```

Backend default URL:

- API: `http://127.0.0.1:8000/api/v1`
- Docs (when DEBUG enabled): `http://127.0.0.1:8000/docs`

### 2) Frontend

```bash
cd tarot-frontend
npm install
npm start
```

Frontend default URL:

- App: `http://localhost:3000`

## Tests

Run backend focused tests:

```bash
cd code
python -m pytest -q tests
```

Run all backend tests:

```bash
cd code
python -m pytest -q
```

## AI Interpretation Notes

- AI interpretation endpoint: `POST /api/v1/records/{id}/interpret`
- Coze config is controlled by backend env vars in `code/.env`:
  - `COZE_API_KEY`
  - `COZE_BOT_ID`
  - `COZE_BASE_URL`
- For local non-Coze verification, enable:
  - `ALLOW_MOCK_AI_FALLBACK=true`

## CI

GitHub Actions workflow is included:

- `code/.github/workflows/backend-tests.yml`

It runs backend tests automatically on push / pull_request.
