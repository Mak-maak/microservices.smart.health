# microservices.smart.health

[![CI/CD – SmartHealth Appointments](https://github.com/Mak-maak/microservices.smart.health/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/Mak-maak/microservices.smart.health/actions/workflows/ci-cd.yml)

> This repository contains the microservices that power the **Smart Health** application — a cloud-native, event-driven platform for managing patient appointments, doctors, and related healthcare workflows.

---

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Services & Components](#services--components)
4. [Local Development](#local-development)
   - [Prerequisites](#prerequisites)
   - [Run with Docker Compose](#run-with-docker-compose)
   - [Run without Docker](#run-without-docker)
   - [Run Tests](#run-tests)
   - [Configuration Reference](#configuration-reference)
5. [Infrastructure & Deployment](#infrastructure--deployment)
   - [Bicep Infrastructure](#bicep-infrastructure)
   - [Azure Resources Provisioned](#azure-resources-provisioned)
   - [Docker Image Build](#docker-image-build)
6. [CI/CD Pipeline](#cicd-pipeline)
7. [Observability & Troubleshooting](#observability--troubleshooting)
8. [Repo Conventions](#repo-conventions)
   - [Folder Structure](#folder-structure)
   - [Adding a New Microservice](#adding-a-new-microservice)

---

## Overview

Smart Health is a microservices-based healthcare platform. This repository currently hosts the **Appointments** bounded context and the shared Azure infrastructure (Bicep) that supports it. Additional microservices (e.g., Notifications, Medical Records) follow the same patterns and can be added under `src/`.

**Primary languages:** C# (.NET 10) · Bicep · Dockerfile

---

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                   Clients / Mobile Apps                 │
└───────────────────────┬─────────────────────────────────┘
                        │ HTTPS
          ┌─────────────▼─────────────┐
          │  Azure API Management     │  JWT validation, rate-limiting,
          │  (infra/apim.bicep)       │  quota, correlation-ID forwarding
          └─────────────┬─────────────┘
                        │
          ┌─────────────▼─────────────────────────────┐
          │  SmartHealth.Appointments.API  (port 8080) │
          │  ─ Minimal API (.NET 10)                   │
          │  ─ CQRS via MediatR                        │
          │  ─ FluentValidation pipeline               │
          │  ─ MassTransit saga orchestration          │
          └──────┬──────────┬─────────────┬────────────┘
                 │          │             │
          ┌──────▼──┐ ┌─────▼──┐ ┌───────▼──────┐
          │ Azure   │ │ Azure  │ │ Azure Service │
          │  SQL    │ │ Cache  │ │    Bus        │
          │(EF Core)│ │(Redis) │ │ (MassTransit) │
          └─────────┘ └────────┘ └───────────────┘
                 │
          ┌──────▼─────────────────────────┐
          │ Azure Monitor / App Insights   │
          │ OpenTelemetry (OTLP exporter)  │
          └────────────────────────────────┘
```

---

## Services & Components

| Name | Path | Purpose | Exposed Port |
|------|------|---------|-------------|
| **SmartHealth.Appointments.API** | `src/SmartHealth.Appointments.API/` | Manages appointment booking, cancellation, patient & doctor records. Includes saga orchestration and outbox pattern. | `8080` (HTTP) |
| **SmartHealth.Appointments.Tests** | `tests/SmartHealth.Appointments.Tests/` | Unit & integration tests (xUnit, FluentAssertions, NSubstitute, MassTransit test harness). | — |

### Infrastructure modules (`infra/`)

| Module | File | Provisions |
|--------|------|-----------|
| Core orchestrator | `main.bicep` | Wires all modules; outputs API URL |
| Azure SQL | `modules/sql.bicep` | Azure SQL Server + `SmartHealthAppointments` database |
| Azure Service Bus | `modules/servicebus.bicep` | Namespace + `appointments` queue |
| Azure Cache for Redis | `modules/redis.bicep` | Redis cache instance |
| Monitoring | `modules/monitoring.bicep` | Log Analytics workspace + Application Insights |
| Container Registry | `modules/acr.bicep` | Azure Container Registry |
| Container Apps | `modules/containerapps.bicep` | Container Apps Environment + Container App (auto-scaling 1–10 replicas) |
| Key Vault | `modules/keyvault.bicep` | Azure Key Vault for secrets |
| API Management | `apim.bicep` | APIM with JWT auth, rate-limiting, quota, backend routing |

---

## Local Development

### Prerequisites

| Tool | Version |
|------|---------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0 or later |
| [Docker Desktop](https://www.docker.com/products/docker-desktop) | 4.x or later |
| [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) | 2.x (optional – for Azure deployment) |
| [Bicep CLI](https://learn.microsoft.com/azure/azure-resource-manager/bicep/install) | latest (optional – for infra deployment) |

### Run with Docker Compose

The fastest way to start the full stack locally (API + SQL Server + Redis):

```bash
docker compose up --build
```

The API will be available at **http://localhost:8080**.

> **Note:** The compose file uses `Features__UseInMemoryBus=true` so Azure Service Bus is **not** required locally.

Tear down:

```bash
docker compose down -v   # -v removes named volumes (SQL + Redis data)
```

### Run without Docker

1. Start SQL Server and Redis locally (or use the compose dependencies only):

   ```bash
   docker compose up sqlserver redis -d
   ```

2. Restore and run the API:

   ```bash
   cd src/SmartHealth.Appointments.API
   dotnet restore
   dotnet run
   ```

   The API listens on `http://localhost:5000` / `https://localhost:5001` by default (Kestrel).

3. Apply EF Core migrations (automatically applied on startup in `Development` / `Staging`; for manual apply):

   ```bash
   dotnet ef database update
   ```

### Run Tests

From the repository root:

```bash
dotnet test tests/SmartHealth.Appointments.Tests/SmartHealth.Appointments.Tests.csproj \
  --configuration Release \
  --logger "console;verbosity=normal" \
  --collect:"XPlat Code Coverage"
```

Or run the full solution (build + test):

```bash
dotnet build Microservices.SmartHealth.App.slnx --configuration Release
dotnet test Microservices.SmartHealth.App.slnx --configuration Release --no-build
```

### Configuration Reference

Configuration is loaded from `appsettings.json` → `appsettings.{Environment}.json` → environment variables → **Azure Key Vault** (production only).

| Key | Description | Default (dev) |
|-----|-------------|---------------|
| `ConnectionStrings__SqlServer` | Azure SQL / local SQL Server connection string | `Server=localhost,1433;...` |
| `ConnectionStrings__Redis` | Redis connection string | `localhost:6379` |
| `ConnectionStrings__AzureServiceBus` | Azure Service Bus connection string | _(empty – in-memory bus used locally)_ |
| `Azure__KeyVaultUri` | Key Vault URI (production only) | _(empty)_ |
| `ApplicationInsights__ConnectionString` | Application Insights connection string | _(empty)_ |
| `OpenTelemetry__OtlpEndpoint` | OTLP exporter endpoint (e.g. local collector) | _(empty)_ |
| `Features__SagaOrchestration` | Enable MassTransit saga orchestration | `true` |
| `Features__Choreography` | Enable choreography-based consumers (alternative to saga) | `false` |
| `Features__UseInMemoryBus` | Use in-memory transport instead of Azure Service Bus | `true` |
| `Features__EventSourcing` | Enable `EventStoreService` | `false` |

> **Secrets strategy:** In production, connection strings and API keys are stored in **Azure Key Vault** and referenced via `DefaultAzureCredential`. Never commit secrets to source control.

---

## Infrastructure & Deployment

### 🚀 Deployment Stack Scripts (Recommended)

**Modern approach using Azure Deployment Stacks for unified lifecycle management.**

See **[infra/scripts/README.md](infra/scripts/README.md)** for complete documentation.

**Quick Start - PowerShell:**
```powershell
# Deploy development environment
.\infra\scripts\deploy-stack.ps1 `
    -Environment dev `
    -Location eastus `
    -SqlAdminPassword (ConvertTo-SecureString "YourStr0ng!Passw0rd" -AsPlainText -Force)

# Validate deployment
.\infra\scripts\validate-stack.ps1 -Environment dev -DetailedOutput

# Clean up
.\infra\scripts\destroy-stack.ps1 -Environment dev -DeleteResourceGroup
```

**Quick Start - Bash:**
```bash
# Deploy development environment
./infra/scripts/deploy-stack.sh \
    --environment dev \
    --location eastus \
    --password "YourStr0ng!Passw0rd"

# Validate deployment
az stack group show --name smarthealth-dev-stack --resource-group smarthealth-dev-rg
```

### Traditional Bicep Deployment (Alternative)

**Prerequisites:** Azure CLI + Bicep CLI installed, and an active Azure subscription.

1. Log in to Azure:

   ```bash
   az login
   az account set --subscription "<your-subscription-id>"
   ```

2. Create a resource group (first time):

   ```bash
   az group create --name smarthealth-dev-rg --location eastus
   ```

3. Deploy all infrastructure:

   ```bash
   az deployment group create \
     --resource-group smarthealth-dev-rg \
     --template-file infra/main.bicep \
     --parameters environment=dev sqlAdminPassword="<YourStr0ngP@ssword>"
   ```

   Replace `dev` with `staging` or `prod` as needed.

4. *(Optional)* Deploy API Management:

   ```bash
   az deployment group create \
     --resource-group smarthealth-dev-rg \
     --template-file infra/apim.bicep \
     --parameters \
       prefix=smarthealth-dev \
       location=eastus \
       apiUrl="<containerAppUrl>" \
       tenantId="<azure-tenant-id>" \
       jwtAudience="<app-registration-client-id>"
   ```

### Azure Resources Provisioned

| Resource | SKU / Notes |
|----------|------------|
| Azure SQL | Configurable in `modules/sql.bicep` |
| Azure Service Bus | Namespace + `appointments` queue |
| Azure Cache for Redis | Configurable in `modules/redis.bicep` |
| Log Analytics + Application Insights | Workspace + AI component |
| Azure Container Registry | For storing Docker images |
| Azure Container Apps | Environment + App; scales 1–10 replicas based on HTTP concurrency |
| Azure Key Vault | Secrets storage |
| Azure API Management | Developer SKU (use Standard/Premium in production) |

### Docker Image Build

The `Dockerfile` in `src/SmartHealth.Appointments.API/` uses a **multi-stage build**:

- **Build stage** (`mcr.microsoft.com/dotnet/sdk:10.0`): restores, builds, and publishes.
- **Runtime stage** (`mcr.microsoft.com/dotnet/aspnet:10.0`): minimal image, runs as non-root user.

Build and run manually:

```bash
docker build -t smarthealth-appointments:local \
  -f src/SmartHealth.Appointments.API/Dockerfile \
  src/SmartHealth.Appointments.API/

docker run -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ConnectionStrings__SqlServer="<connection-string>" \
  -e ConnectionStrings__Redis="<redis-connection>" \
  -e Features__UseInMemoryBus=true \
  smarthealth-appointments:local
```

---

## CI/CD Pipeline

The GitHub Actions workflow (`.github/workflows/ci-cd.yml`) runs three jobs:

| Job | Trigger | Steps |
|-----|---------|-------|
| **Build & Test** | Push to `main`/`develop`, PR to `main` | Restore → Build → Test → Publish test results |
| **Build & Push Docker Image** | Push to `main` only | Azure OIDC login → ACR login → `docker/build-push-action` |
| **Deploy to AKS** | After image push (push to `main` only) | `az aks get-credentials` → `kubectl set image` → verify rollout |

> **Note:** The CI/CD workflow deploys to **Azure Kubernetes Service (AKS)** via `kubectl`. The Bicep templates under `infra/` provision an alternative **Azure Container Apps** environment. Both are valid hosting targets; use whichever matches your deployment strategy.

### Required GitHub Secrets

| Secret | Description |
|--------|------------|
| `ACR_LOGIN_SERVER` | Azure Container Registry login server (e.g. `myacr.azurecr.io`) |
| `ACR_NAME` | ACR name (short form, e.g. `myacr`) |
| `AZURE_CLIENT_ID` | Service principal / managed identity client ID (OIDC) |
| `AZURE_TENANT_ID` | Azure AD tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID |
| `AKS_RESOURCE_GROUP` | Resource group containing the AKS cluster (CI/CD → AKS deployment) |
| `AKS_CLUSTER_NAME` | AKS cluster name (CI/CD → AKS deployment) |

---

## Observability & Troubleshooting

### Logging

The application uses the built-in `Microsoft.Extensions.Logging` framework. Log levels are configured per-environment in `appsettings.{Environment}.json`. In development, EF Core query logs are emitted at `Information` level.

### Tracing

**OpenTelemetry** is configured with:

- `OpenTelemetry.Instrumentation.AspNetCore` – HTTP request traces.
- `OpenTelemetry.Instrumentation.Http` – Outbound HTTP call traces.
- `MassTransit` activity source – Message bus traces.
- **Azure Monitor** exporter (`Azure.Monitor.OpenTelemetry.AspNetCore`) when `ApplicationInsights__ConnectionString` is set.
- **OTLP exporter** when `OpenTelemetry__OtlpEndpoint` is set (compatible with Jaeger, Tempo, etc.).

### Health Endpoints

| Endpoint | Purpose |
|----------|---------|
| `GET /health` | Overall health (all checks) |
| `GET /readiness` | Readiness checks: SQL Server, Redis, Azure Service Bus |
| `GET /liveness` | Liveness probe (lightweight, returns `{ status: "alive" }`) |

### Common Issues

| Issue | Likely Cause | Resolution |
|-------|-------------|------------|
| Container fails to start | SQL Server not ready | The compose `healthcheck` retries 10 times; wait for `sqlserver` to become healthy |
| `Connection refused` on Redis | Redis not running | `docker compose up redis -d` |
| `401 Unauthorized` via APIM | Invalid/missing JWT | Ensure the bearer token has the `appointments.read` or `appointments.write` scope |
| EF Core migration error | Schema drift | Run `dotnet ef database update` or let the app auto-migrate in `Development` |
| High latency on first request | ReadyToRun warm-up | Expected; subsequent requests are fast |

---

## Repo Conventions

### Folder Structure

```
microservices.smart.health/
├── .github/
│   └── workflows/
│       └── ci-cd.yml              # GitHub Actions CI/CD pipeline
├── infra/
│   ├── main.bicep                 # Root Bicep template (orchestrator)
│   ├── apim.bicep                 # API Management (optional, standalone)
│   └── modules/
│       ├── acr.bicep
│       ├── containerapps.bicep
│       ├── keyvault.bicep
│       ├── monitoring.bicep
│       ├── redis.bicep
│       ├── servicebus.bicep
│       └── sql.bicep
├── src/
│   └── SmartHealth.Appointments.API/
│       ├── Domain/                # Entities, value objects, domain events, exceptions
│       ├── Features/              # CQRS commands/queries (Appointments, Doctors, Patients)
│       ├── Infrastructure/        # EF Core, Redis, MassTransit, saga, outbox, event sourcing
│       ├── Shared/                # Cross-cutting pipeline behaviours (logging, validation)
│       ├── Dockerfile
│       └── Program.cs
├── tests/
│   └── SmartHealth.Appointments.Tests/
│       ├── Domain/
│       ├── Features/
│       ├── Saga/
│       └── Outbox/
├── docker-compose.yml             # Local dev stack (API + SQL + Redis)
└── Microservices.SmartHealth.App.slnx
```

### Adding a New Microservice

1. **Create the project** under `src/SmartHealth.<ServiceName>.API/`:

   ```bash
   dotnet new webapi -n SmartHealth.<ServiceName>.API \
     --output src/SmartHealth.<ServiceName>.API
   ```

2. **Add to solution:**

   ```bash
   dotnet sln Microservices.SmartHealth.App.slnx add \
     src/SmartHealth.<ServiceName>.API/SmartHealth.<ServiceName>.API.csproj
   ```

3. **Add a Dockerfile** following the pattern in `src/SmartHealth.Appointments.API/Dockerfile` (multi-stage, non-root user, port 8080).

4. **Add a service entry** to `docker-compose.yml` with its dependencies.

5. **Add a Bicep module** under `infra/modules/` and wire it into `infra/main.bicep`.

6. **Add tests** under `tests/SmartHealth.<ServiceName>.Tests/` and reference the new project.

7. **Update the CI/CD workflow** (`.github/workflows/ci-cd.yml`) to include the new project in build and test steps.

