FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY KPIAPI.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish KPIAPI.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS migrate
WORKDIR /src

RUN apt-get update \
    && apt-get install -y --no-install-recommends postgresql-client \
    && rm -rf /var/lib/apt/lists/*

RUN dotnet tool install --global dotnet-ef
ENV PATH="${PATH}:/root/.dotnet/tools"

COPY . ./
COPY --from=build /src/bin /src/bin
COPY --from=build /src/obj /src/obj

COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "KPIAPI.dll"]