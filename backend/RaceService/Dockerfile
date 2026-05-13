# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

COPY ["Common/Common.csproj", "Common/"]
COPY ["RaceService/RaceService.csproj", "RaceService/"]
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore "RaceService/RaceService.csproj"

COPY Common/ Common/
COPY RaceService/ RaceService/
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish "RaceService/RaceService.csproj" \
        -c Release \
        -o /app/publish \
        --no-restore \
        /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app
EXPOSE 9503
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PRC.RaceService.dll"]
