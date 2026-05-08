FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 9508

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Common/Common.csproj", "Common/"]
COPY ["PublicService/PublicService.csproj", "PublicService/"]
RUN dotnet restore "PublicService/PublicService.csproj"
COPY Common/ Common/
COPY PublicService/ PublicService/
WORKDIR "/src/PublicService"
RUN dotnet build "PublicService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PublicService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PRC.PublicService.dll"]
