# syntax=docker/dockerfile:1
# Multi-stage build for the Todo API (.NET 10).
# Build context is the API/ folder (this directory).

# ---- Stage 1: build & publish ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the csproj first and restore, so the restore layer is cached
# and only re-runs when dependencies change (not on every source edit).
COPY Lab-01/Lab-01.csproj Lab-01/
RUN dotnet restore Lab-01/Lab-01.csproj

# Copy the rest of the source and publish a Release build.
COPY Lab-01/ Lab-01/
RUN dotnet publish Lab-01/Lab-01.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---- Stage 2: runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Kestrel listens on plain HTTP 8080 inside the container (map it on the host).
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Run as the non-root user provided by the aspnet base image.
USER $APP_UID

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Lab-01.dll"]
