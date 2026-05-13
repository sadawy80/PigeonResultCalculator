# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

COPY ["Common/Common.csproj", "Common/"]
COPY ["ClubService/ClubService.csproj", "ClubService/"]
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore "ClubService/ClubService.csproj"

COPY Common/ Common/
COPY ClubService/ ClubService/
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish "ClubService/ClubService.csproj" \
        -c Release \
        -o /app/publish \
        --no-restore \
        /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app
EXPOSE 9502
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PRC.ClubService.dll"]
