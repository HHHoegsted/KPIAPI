#!/bin/sh
set -e

echo "[kpiapi] Starting container entrypoint..."

CONN="${ConnectionStrings__DefaultConnection:-}"
if [ -z "$CONN" ]; then
  echo "[kpiapi] ERROR: ConnectionStrings__DefaultConnection is not set"
  exit 1
fi

HOST=$(echo "$CONN" | sed -n 's/.*Host=\([^;]*\).*/\1/p')
PORT=$(echo "$CONN" | sed -n 's/.*Port=\([^;]*\).*/\1/p')
DB=$(echo "$CONN" | sed -n 's/.*Database=\([^;]*\).*/\1/p')
USER=$(echo "$CONN" | sed -n 's/.*Username=\([^;]*\).*/\1/p')

if [ -z "$HOST" ]; then HOST="postgres"; fi
if [ -z "$PORT" ]; then PORT="5432"; fi
if [ -z "$DB" ]; then DB="kpiapi"; fi
if [ -z "$USER" ]; then USER="kpiapi"; fi

echo "[kpiapi] Waiting for Postgres at $HOST:$PORT (db=$DB user=$USER)..."

i=0
until pg_isready -h "$HOST" -p "$PORT" -U "$USER" -d "$DB" >/dev/null 2>&1; do
  i=$((i+1))
  if [ "$i" -gt 60 ]; then
    echo "[kpiapi] ERROR: Postgres not ready after 60 attempts"
    echo "[kpiapi] Connection string was: $CONN"
    exit 1
  fi
  echo "[kpiapi] Postgres not ready yet. Sleeping..."
  sleep 2
done

echo "[kpiapi] Postgres ready."

cd /app/src

echo "[kpiapi] Restoring packages for EF (using container cache)..."
dotnet restore KPIAPI.csproj

echo "[kpiapi] Building (needed for EF deps.json)..."
dotnet build KPIAPI.csproj -c Release

echo "[kpiapi] Applying EF migrations..."
dotnet ef database update --project KPIAPI.csproj --configuration Release

echo "[kpiapi] Starting API..."
cd /app/published
exec dotnet KPIAPI.dll
