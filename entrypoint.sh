#!/bin/sh
set -e

echo "[kpiapi-migrate] Starting migration entrypoint..."

CONN="${ConnectionStrings__DefaultConnection:-}"
if [ -z "$CONN" ]; then
    echo "[kpiapi-migrate] ERROR: ConnectionStrings__DefaultConnection is not set"
    exit 1
fi

HOST=$(echo "$CONN" | sed -n 's/.*Host=\([^;]*\).*/\1/p')
PORT=$(echo "$CONN" | sed -n 's/.*Port=\([^;]*\).*/\1/p')
DB=$(echo "$CONN" | sed -n 's/.*Database=\([^;]*\).*/\1/p')
USER=$(echo "$CONN" | sed -n 's/.*Username=\([^;]*\).*/\1/p')

if [ -z "$HOST" ]; then
    HOST="postgres"
fi

if [ -z "$PORT" ]; then
    PORT="5432"
fi

if [ -z "$DB" ]; then
    DB="kpiapi"
fi

if [ -z "$USER" ]; then
    USER="kpiapi"
fi

echo "[kpiapi-migrate] Waiting for Postgres at $HOST:$PORT (db=$DB user=$USER)..."

i=0
until pg_isready -h "$HOST" -p "$PORT" -U "$USER" -d "$DB" >/dev/null 2>&1; do
    i=$((i + 1))
    if [ "$i" -gt 60 ]; then
        echo "[kpiapi-migrate] ERROR: Postgres not ready after 60 attempts"
        exit 1
    fi

    echo "[kpiapi-migrate] Postgres not ready yet. Sleeping..."
    sleep 2
done

echo "[kpiapi-migrate] Postgres ready."
echo "[kpiapi-migrate] Applying EF migrations..."

dotnet ef database update --project KPIAPI.csproj --configuration Release

echo "[kpiapi-migrate] Migrations applied successfully."