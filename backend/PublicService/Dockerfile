# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

COPY ["Common/Common.csproj", "Common/"]
COPY ["PublicService/PublicService.csproj", "PublicService/"]
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore "PublicService/PublicService.csproj"

COPY Common/ Common/
COPY PublicService/ PublicService/
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish "PublicService/PublicService.csproj" \
        -c Release \
        -o /app/publish \
        --no-restore \
        /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app
EXPOSE 9508
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PRC.PublicService.dll"]
