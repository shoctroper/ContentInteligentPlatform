# syntax=docker/dockerfile:1

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ContentIntelligencePlatform.sln .
COPY src/Domain/ContentIntelligencePlatform.Domain.csproj src/Domain/
COPY src/Application/ContentIntelligencePlatform.Application.csproj src/Application/
COPY src/Infrastructure/ContentIntelligencePlatform.Infrastructure.csproj src/Infrastructure/
COPY src/Api/ContentIntelligencePlatform.Api.csproj src/Api/
RUN dotnet restore src/Api/ContentIntelligencePlatform.Api.csproj

COPY src/ src/
RUN dotnet publish src/Api/ContentIntelligencePlatform.Api.csproj -c Release -o /app --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN addgroup --gid 1000 cip && adduser --uid 1000 --gid 1000 --disabled-password --gecos "" cip

COPY --from=build /app .
COPY knowledge/ /knowledge/

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    Knowledge__RootPath=/knowledge \
    ConnectionStrings__Default="Data Source=/data/cip.db"

RUN mkdir -p /data && chown -R cip:cip /data /knowledge /app
USER cip

EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD wget -qO- http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "ContentIntelligencePlatform.Api.dll"]
