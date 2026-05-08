FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 9504

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Common/Common.csproj", "Common/"]
COPY ["FederationService/FederationService.csproj", "FederationService/"]
RUN dotnet restore "FederationService/FederationService.csproj"
COPY Common/ Common/
COPY FederationService/ FederationService/
WORKDIR "/src/FederationService"
RUN dotnet build "FederationService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FederationService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PRC.FederationService.dll"]
