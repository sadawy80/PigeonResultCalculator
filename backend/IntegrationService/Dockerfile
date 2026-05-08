FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 9506

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Common/Common.csproj", "Common/"]
COPY ["IntegrationService/IntegrationService.csproj", "IntegrationService/"]
RUN dotnet restore "IntegrationService/IntegrationService.csproj"
COPY Common/ Common/
COPY IntegrationService/ IntegrationService/
WORKDIR "/src/IntegrationService"
RUN dotnet build "IntegrationService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "IntegrationService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PRC.IntegrationService.dll"]
