# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

COPY ["Common/Common.csproj", "Common/"]
COPY ["AuditService/AuditService.csproj", "AuditService/"]
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore "AuditService/AuditService.csproj"

COPY Common/ Common/
COPY AuditService/ AuditService/
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish "AuditService/AuditService.csproj" \
        -c Release \
        -o /app/publish \
        --no-restore \
        /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app
EXPOSE 9511
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PRC.AuditService.dll"]
