FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 9502

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["ClubService/ClubService.csproj", "ClubService/"]
COPY ["Common/Common.csproj", "Common/"]
RUN dotnet restore "ClubService/ClubService.csproj"
COPY . .
RUN dotnet publish "ClubService/ClubService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PRC.ClubService.dll"]
