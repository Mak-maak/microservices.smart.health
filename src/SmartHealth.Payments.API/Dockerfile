# =============================================================================
# Multi-stage Dockerfile for SmartHealth Payments API
# Target: .NET 10 (mcr.microsoft.com/dotnet/aspnet:10.0)
# Non-root user, port 8080
# =============================================================================

# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY SmartHealth.Payments.API.csproj ./
RUN dotnet restore SmartHealth.Payments.API.csproj -r linux-x64

COPY . ./
RUN dotnet publish SmartHealth.Payments.API.csproj \
    --configuration Release \
    --runtime linux-x64 \
    --no-restore \
    --output /app/publish

# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

RUN groupadd --system --gid 1001 appgroup \
 && useradd --system --uid 1001 --gid appgroup --no-create-home appuser

COPY --from=build /app/publish ./

HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

USER appuser

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true

ENTRYPOINT ["dotnet", "SmartHealth.Payments.API.dll"]
