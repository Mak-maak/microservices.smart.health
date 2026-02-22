# =============================================================================
# Multi-stage Dockerfile for SmartHealth Appointments API
# Target: .NET 10 (mcr.microsoft.com/dotnet/aspnet:10.0)
# Non-root user, port 8080
# =============================================================================

# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Copy solution and project files first (layer caching)
COPY SmartHealth.Appointments.slnx ./
COPY src/SmartHealth.Appointments.API/SmartHealth.Appointments.API.csproj ./src/SmartHealth.Appointments.API/

# Restore dependencies
RUN dotnet restore src/SmartHealth.Appointments.API/SmartHealth.Appointments.API.csproj

# Copy source
COPY src/SmartHealth.Appointments.API/. ./src/SmartHealth.Appointments.API/

# Publish (ReadyToRun enabled in csproj)
RUN dotnet publish src/SmartHealth.Appointments.API/SmartHealth.Appointments.API.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN addgroup --system --gid 1001 appgroup \
 && adduser  --system --uid 1001 --ingroup appgroup --no-create-home appuser

# Copy published artefacts
COPY --from=build /app/publish ./

# Health check endpoint
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD wget -qO- http://localhost:8080/liveness || exit 1

# Drop privileges
USER appuser

# Expose HTTP port (HTTPS terminated at ingress/APIM)
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true

ENTRYPOINT ["dotnet", "SmartHealth.Appointments.API.dll"]
