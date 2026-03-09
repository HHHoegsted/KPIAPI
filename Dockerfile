# Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /out

# Runtime + EF tools for migrations (simple + reliable)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS runtime
WORKDIR /app

# pg_isready for readiness checks
RUN apt-get update \
  && apt-get install -y --no-install-recommends postgresql-client \
  && rm -rf /var/lib/apt/lists/*

# EF tools
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"

# Copy NuGet package cache from build stage so EF can run without downloading
COPY --from=build /root/.nuget /root/.nuget

# 1) Published output for running the API
COPY --from=build /out /app/published

# 2) Project source so dotnet-ef can find csproj/migrations
COPY . /app/src

# IMPORTANT: remove Windows build artifacts that break Linux builds
RUN find /app/src -type d \( -name bin -o -name obj -o -name .vs \) -prune -exec rm -rf {} +

# Entrypoint
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

EXPOSE 8080
ENTRYPOINT ["/entrypoint.sh"]
