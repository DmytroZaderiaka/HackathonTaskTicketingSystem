# Backend API image. Build context is the repository root (see docker-compose.yml).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first for better layer caching.
COPY src/HackathonTaskTicketingSystem.Api/HackathonTaskTicketingSystem.Api.csproj src/HackathonTaskTicketingSystem.Api/
RUN dotnet restore src/HackathonTaskTicketingSystem.Api/HackathonTaskTicketingSystem.Api.csproj

COPY src/ src/
RUN dotnet publish src/HackathonTaskTicketingSystem.Api/HackathonTaskTicketingSystem.Api.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "HackathonTaskTicketingSystem.Api.dll"]
