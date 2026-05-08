FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 9507

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["AdminService/AdminService.csproj", "AdminService/"]
COPY ["Common/Common.csproj", "Common/"]
RUN dotnet restore "AdminService/AdminService.csproj"
COPY . .
RUN dotnet publish "AdminService/AdminService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PRC.AdminService.dll"]
