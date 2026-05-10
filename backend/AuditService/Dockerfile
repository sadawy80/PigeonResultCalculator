FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 9511

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["AuditService/AuditService.csproj", "AuditService/"]
COPY ["Common/Common.csproj", "Common/"]
RUN dotnet restore "AuditService/AuditService.csproj"
COPY . .
RUN dotnet publish "AuditService/AuditService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PRC.AuditService.dll"]
