# ── Build stage ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files for layer-cached restore
COPY backend/Domain/PigeonRacing.Domain.csproj             Domain/
COPY backend/Application/PigeonRacing.Application.csproj   Application/
COPY backend/Infrastructure/PigeonRacing.Infrastructure.csproj Infrastructure/
COPY backend/API/PigeonRacing.API.csproj                    API/

RUN dotnet restore API/PigeonRacing.API.csproj

# Copy backend source only
COPY backend/ .
RUN dotnet publish API/PigeonRacing.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Debug stage (VS Code remote attach via vsdbg) ─────────────────────────────
FROM build AS debug
# Install vsdbg so VS Code can attach via "docker exec" pipe transport
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && curl -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -v latest -l /vsdbg \
    && rm -rf /var/lib/apt/lists/*
# Source is mounted by docker-compose.override.yml at /src
# ENTRYPOINT is overridden by docker-compose.debug.yml command

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install curl for health-check probe
RUN apt-get update && apt-get install -y curl --no-install-recommends && rm -rf /var/lib/apt/lists/*

# Non-root user for security
RUN addgroup --system --gid 1001 appgroup && \
    adduser  --system --uid 1001 --ingroup appgroup appuser

COPY --from=build --chown=appuser:appgroup /app/publish .

# Create uploads directory
RUN mkdir -p /app/uploads && chown -R appuser:appgroup /app/uploads

USER appuser

EXPOSE 8080
EXPOSE 8081

ENV ASPNETCORE_URLS="http://+:8080"
ENV ASPNETCORE_ENVIRONMENT="Production"

HEALTHCHECK --interval=15s --timeout=5s --start-period=30s --retries=5 \
  CMD curl -f http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "PigeonRacing.API.dll"]
