# KPIAPI

KPIAPI is a full-stack application built with:

- ASP.NET Core / .NET 10 for the backend API
- PostgreSQL 16 for persistence
- React + Vite + TypeScript for the frontend
- Docker Compose for local orchestration

The repository contains three runtime services:

- `postgres` on port `5432`
- `api` on port `8080`
- `frontend` on port `3000`

The frontend is built with Vite and served from Nginx. The backend is an ASP.NET Core app using Entity Framework Core with PostgreSQL.

## Repository structure

- `Controllers/`
- `DTOs/`
- `Data/`
- `Domain/`
- `Migrations/`
- `Services/`
- `frontend/`
- `Dockerfile`
- `docker-compose.yml`
- `entrypoint.sh`
- `KPIAPI.csproj`
- `KPIAPI.sln`
- `Program.cs`
- `appsettings.json`
- `appsettings.Development.json`

## Prerequisites

On a fresh Linux machine, install:

- Git
- Docker Engine
- Docker Compose plugin

Example for Debian/Ubuntu:

```bash
sudo apt update
sudo apt install -y git docker.io docker-compose-plugin
sudo systemctl enable docker
sudo systemctl start docker
sudo usermod -aG docker "$USER"
```

After adding yourself to the `docker` group, log out and back in before continuing.

## Clone the repository

```bash
git clone https://github.com/HHHoegsted/KPIAPI.git
cd KPIAPI
```

## Start the services

From the repository root:

```bash
docker compose up --build
```

To run in the background:

```bash
docker compose up --build -d
```

## Service URLs

Once the containers are up:

- Frontend: `http://localhost:3000`
- API: `http://localhost:8080`
- PostgreSQL: `localhost:5432`

## Fresh machine quick start

For a brand new Linux machine, the shortest path is:

```bash
sudo apt update
sudo apt install -y git docker.io docker-compose-plugin
sudo systemctl enable docker
sudo systemctl start docker
sudo usermod -aG docker "$USER"
```

Log out and back in, then run:

```bash
git clone https://github.com/HHHoegsted/KPIAPI.git
cd KPIAPI
docker compose up --build -d
docker compose ps
```

## Stop the services

```bash
docker compose down
```

To also remove the Postgres data volume:

```bash
docker compose down -v
```

## Configuration

The Docker Compose file supplies the database connection string to the API container through environment variables.

Current default development values:

- database name: `kpiapi`
- database user: `kpiapi`
- database password: `kpiapi_pw`

These are fine for local development, but they should be changed for any real deployment.

## Development notes

### Backend

The backend is an ASP.NET Core app targeting `.NET 10`.

### Frontend

The frontend lives in `./frontend` and uses:

- React
- TypeScript
- Vite
- Nginx for containerized serving

### Database

The project includes a `Migrations/` folder and uses Entity Framework Core with PostgreSQL.

## Swagger

Swagger is only enabled when the API runs in the `Development` environment.

If your `docker-compose.yml` sets:

```text
ASPNETCORE_ENVIRONMENT=Production
```

then Swagger will not be available.

If you want Swagger locally, change the API environment to `Development` before starting the stack.

## Useful commands

Rebuild everything:

```bash
docker compose up --build
```

View running containers:

```bash
docker compose ps
```

View all logs:

```bash
docker compose logs -f
```

View logs for a single service:

```bash
docker compose logs -f api
docker compose logs -f frontend
docker compose logs -f postgres
```

Stop and remove containers:

```bash
docker compose down
```

Stop and remove containers plus database volume:

```bash
docker compose down -v
```

## Troubleshooting

### Port already in use

If one of the ports is already occupied, Docker Compose will fail to start that service.

Check what is using a port:

```bash
sudo ss -tulpn | grep 3000
sudo ss -tulpn | grep 8080
sudo ss -tulpn | grep 5432
```

Then either stop the conflicting service or change the port mapping in `docker-compose.yml`.

### Docker permission denied

If `docker compose` still requires `sudo`, you likely need to log out and back in after adding your user to the `docker` group.

### Database container starts but app cannot connect

Check logs:

```bash
docker compose logs -f postgres
docker compose logs -f api
```

If needed, restart the stack cleanly:

```bash
docker compose down
docker compose up --build
```

If you want to reset the database completely:

```bash
docker compose down -v
docker compose up --build
```

## License

This project is licensed under the Apache License 2.0.

See the `LICENSE` file for the full license text.
