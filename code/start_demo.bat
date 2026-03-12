@echo off
chcp 65001 >nul
setlocal

echo Starting tarot game demo stack...

docker compose version >nul 2>&1
if %errorlevel%==0 (
  set "COMPOSE_CMD=docker compose"
) else (
  where docker-compose >nul 2>&1
  if %errorlevel%==0 (
    set "COMPOSE_CMD=docker-compose"
  ) else (
    echo Error: neither "docker compose" nor "docker-compose" is available
    exit /b 1
  )
)

if not exist ".env" (
  echo Error: .env file is missing.
  exit /b 1
)

%COMPOSE_CMD% up --build -d

echo Waiting for services...
timeout /t 10 /nobreak >nul

%COMPOSE_CMD% ps

echo Initializing demo data...
docker exec tarot_fastapi_app python -m app.scripts.init_demo_data

echo.
echo Demo system is ready
echo API docs: http://localhost:8000/docs
echo API base: http://localhost:8000/api/v1/
echo Demo usernames: admin, demo_user, alice, bob
echo Stop services: docker compose down (or docker-compose down)

endlocal
