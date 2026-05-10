FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 9510

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["BackupService/BackupService.csproj", "BackupService/"]
COPY ["Common/Common.csproj", "Common/"]
RUN dotnet restore "BackupService/BackupService.csproj"
COPY . .
RUN dotnet publish "BackupService/BackupService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PRC.BackupService.dll"]
