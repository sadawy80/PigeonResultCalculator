# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

COPY ["Common/Common.csproj", "Common/"]
COPY ["FileService/FileService.csproj", "FileService/"]
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore "FileService/FileService.csproj"

COPY Common/ Common/
COPY FileService/ FileService/
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish "FileService/FileService.csproj" \
        -c Release \
        -o /app/publish \
        --no-restore \
        /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app
EXPOSE 9512
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PRC.FileService.dll"]
