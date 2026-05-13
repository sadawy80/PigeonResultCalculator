# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

COPY ["Common/Common.csproj", "Common/"]
COPY ["SubscriptionService/SubscriptionService.csproj", "SubscriptionService/"]
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore "SubscriptionService/SubscriptionService.csproj"

COPY Common/ Common/
COPY SubscriptionService/ SubscriptionService/
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish "SubscriptionService/SubscriptionService.csproj" \
        -c Release \
        -o /app/publish \
        --no-restore \
        /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app
EXPOSE 9509
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PRC.SubscriptionService.dll"]
