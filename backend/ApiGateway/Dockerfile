# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

COPY ["Common/Common.csproj", "Common/"]
COPY ["ApiGateway/ApiGateway.csproj", "ApiGateway/"]
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore "ApiGateway/ApiGateway.csproj"

COPY Common/ Common/
COPY ApiGateway/ ApiGateway/
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish "ApiGateway/ApiGateway.csproj" \
        -c Release \
        -o /app/publish \
        --no-restore \
        /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app
EXPOSE 9500
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PRC.ApiGateway.dll"]
