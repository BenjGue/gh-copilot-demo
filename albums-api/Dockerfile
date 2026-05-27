# syntax=docker/dockerfile:1

# --- Build stage --------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj first for better layer caching
COPY albums-api.csproj ./
RUN dotnet restore albums-api.csproj

# Copy the rest of the source
COPY . ./
RUN dotnet publish albums-api.csproj -c Release -o /app/publish /p:UseAppHost=false

# --- Runtime stage ------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Listen on port 3000 (matches docker-compose and the workshop expectations).
# HTTPS is terminated by Traefik in front of the container, so we serve plain HTTP here.
ENV ASPNETCORE_URLS=http://+:3000 \
    ASPNETCORE_ENVIRONMENT=Development \
    DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build /app/publish ./

EXPOSE 3000
ENTRYPOINT ["dotnet", "albums-api.dll"]
