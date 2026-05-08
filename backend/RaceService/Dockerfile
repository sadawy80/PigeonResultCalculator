FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 9503

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["RaceService/RaceService.csproj", "RaceService/"]
COPY ["Common/Common.csproj", "Common/"]
RUN dotnet restore "RaceService/RaceService.csproj"
COPY . .
RUN dotnet publish "RaceService/RaceService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PRC.RaceService.dll"]
