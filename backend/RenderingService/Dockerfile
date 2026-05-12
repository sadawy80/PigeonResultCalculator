FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 9505

# Install system Chromium + the font set the cert / result templates need.
# Using apt-installed Chromium (instead of PuppeteerSharp's BrowserFetcher) means
# the same image works on linux/amd64 and linux/arm64 — apt resolves the right
# arch automatically when the image is built with `docker buildx --platform`.
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
    && fc-cache -f -v \
    && rm -rf /var/lib/apt/lists/*

ENV PUPPETEER_EXECUTABLE_PATH=/usr/bin/chromium
ENV PUPPETEER_SKIP_DOWNLOAD=true

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Common/Common.csproj", "Common/"]
COPY ["RenderingService/RenderingService.csproj", "RenderingService/"]
RUN dotnet restore "RenderingService/RenderingService.csproj"
COPY Common/ Common/
COPY RenderingService/ RenderingService/
WORKDIR "/src/RenderingService"
RUN dotnet build "RenderingService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RenderingService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PRC.RenderingService.dll"]
