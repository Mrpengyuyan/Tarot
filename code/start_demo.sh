#!/bin/bash

set -euo pipefail

echo "Starting tarot game demo stack..."

action_compose() {
  if docker compose version >/dev/null 2>&1; then
    docker compose "$@"
  elif command -v docker-compose >/dev/null 2>&1; then
    docker-compose "$@"
  else
    echo "Error: neither 'docker compose' nor 'docker-compose' is available"
    exit 1
  fi
}

if ! docker info >/dev/null 2>&1; then
  echo "Error: Docker is not running. Start Docker Desktop first."
  exit 1
fi

if [ ! -f ".env" ]; then
  echo "Error: .env file is missing."
  exit 1
fi

action_compose up --build -d

echo "Waiting for services..."
sleep 10

action_compose ps

echo "Initializing demo data..."
docker exec tarot_fastapi_app python -m app.scripts.init_demo_data

echo
echo "Demo system is ready"
echo "API docs: http://localhost:8000/docs"
echo "API base: http://localhost:8000/api/v1/"
echo "Demo usernames: admin, demo_user, alice, bob"
echo "Stop services: docker compose down (or docker-compose down)"
