# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

COPY ["Common/Common.csproj", "Common/"]
COPY ["IdentityService/IdentityService.csproj", "IdentityService/"]
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore "IdentityService/IdentityService.csproj"

COPY Common/ Common/
COPY IdentityService/ IdentityService/
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish "IdentityService/IdentityService.csproj" \
        -c Release \
        -o /app/publish \
        --no-restore \
        /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app
EXPOSE 9501
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PRC.IdentityService.dll"]
