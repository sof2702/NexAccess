# ==========================================
# NEXACCESS - BUILD
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY . .

RUN dotnet restore src/NexAccess.API/NexAccess.API.csproj

RUN dotnet publish src/NexAccess.API/NexAccess.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ==========================================
# NEXACCESS - RUNTIME
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

USER $APP_UID

ENTRYPOINT ["dotnet", "NexAccess.API.dll"]
