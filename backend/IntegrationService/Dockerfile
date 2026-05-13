# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

COPY ["Common/Common.csproj", "Common/"]
COPY ["IntegrationService/IntegrationService.csproj", "IntegrationService/"]
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore "IntegrationService/IntegrationService.csproj"

COPY Common/ Common/
COPY IntegrationService/ IntegrationService/
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish "IntegrationService/IntegrationService.csproj" \
        -c Release \
        -o /app/publish \
        --no-restore \
        /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app
EXPOSE 9506
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PRC.IntegrationService.dll"]
