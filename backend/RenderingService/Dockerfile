# syntax=docker/dockerfile:1.7
#
# RenderingService stays on the Debian-based aspnet image (not alpine) because
# Puppeteer/Chromium plus the broad font set we need (CJK, Arabic, emoji) is
# far better tested on debian-slim. The apt-installed chromium below resolves
# the correct arch automatically — works on both linux/amd64 and linux/arm64
# when built with `docker buildx --platform`.

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Common/Common.csproj", "Common/"]
COPY ["RenderingService/RenderingService.csproj", "RenderingService/"]
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore "RenderingService/RenderingService.csproj"

COPY Common/ Common/
COPY RenderingService/ RenderingService/
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish "RenderingService/RenderingService.csproj" \
        -c Release \
        -o /app/publish \
        --no-restore \
        /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 9505

RUN apt-get update && apt-get install -y --no-install-recommends \
        chromium \
        chromium-driver \
        ca-certificates \
        fonts-liberation \
        fonts-noto \
        fonts-noto-cjk \
        fonts-noto-color-emoji \
        fonts-arabeyes \
        fontconfig \
        netcat-openbsd \
    && fc-cache -f \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/* /var/cache/apt/archives/*

ENV PUPPETEER_EXECUTABLE_PATH=/usr/bin/chromium \
    PUPPETEER_SKIP_DOWNLOAD=true

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PRC.RenderingService.dll"]
