FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 9509

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["SubscriptionService/SubscriptionService.csproj", "SubscriptionService/"]
COPY ["Common/Common.csproj", "Common/"]
RUN dotnet restore "SubscriptionService/SubscriptionService.csproj"
COPY . .
RUN dotnet publish "SubscriptionService/SubscriptionService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PRC.SubscriptionService.dll"]
