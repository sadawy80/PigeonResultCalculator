# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

COPY ["Common/Common.csproj", "Common/"]
COPY ["AdminService/AdminService.csproj", "AdminService/"]
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore "AdminService/AdminService.csproj"

COPY Common/ Common/
COPY AdminService/ AdminService/
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish "AdminService/AdminService.csproj" \
        -c Release \
        -o /app/publish \
        --no-restore \
        /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app
EXPOSE 9507
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PRC.AdminService.dll"]
