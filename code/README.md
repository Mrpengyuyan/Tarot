# Tarot Game (Frontend + Backend)

This repository contains a full-stack tarot game:
- `tarot-frontend/`: React + TypeScript UI
- `code/`: FastAPI backend (auth, records, cards, spreads, AI interpretation)

## Current architecture
- Frontend calls backend APIs under `/api/v1/*`.
- Backend integrates with Coze for interpretation generation.
- Auth supports bearer token and HttpOnly cookie session.

## Backend quick start
```bash
cd code
python -m pip install -r requirements.txt
python -m uvicorn app.main:app --reload
```

## Frontend quick start
```bash
cd tarot-frontend
npm install
npm start
```

## Demo bootstrap
Use:
- `code/start_demo.sh` (Linux/macOS)
- `code/start_demo.bat` (Windows)

These scripts bring up containers and initialize demo users.

## Notes
- Set `COZE_API_KEY` and `COZE_BOT_ID` in backend `.env` to enable real AI interpretation.
- Do not commit secrets, local virtual environments, or generated logs.
