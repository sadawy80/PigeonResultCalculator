FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 9505

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
