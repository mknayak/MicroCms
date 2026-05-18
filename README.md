# MicroCMS

A multi-tenant, headless content management system built on .NET 8 and ASP.NET Core.  
It exposes a REST API, a GraphQL endpoint, structured observability via OpenTelemetry/Serilog, and ships with a React-based Admin UI.

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [Prerequisites](#2-prerequisites)
3. [Local Development Setup (no Docker)](#3-local-development-setup-no-docker)
4. [Running the Full Stack with Docker Compose](#4-running-the-full-stack-with-docker-compose)
5. [Observability Stack](#5-observability-stack)
6. [Running Tests](#6-running-tests)
7. [Building the Docker Image](#7-building-the-docker-image)
8. [Deploying to Kubernetes (AKS / EKS)](#8-deploying-to-kubernetes-aks--eks)
9. [Environment Variable Reference](#9-environment-variable-reference)
10. [Secrets Rotation](#10-secrets-rotation)

---

## 1. Architecture Overview

```
┌──────────────────────────────────────────────────┐
│                  Clients                         │
│  Admin UI (React)  │  Headless (Next.js / SDK)   │
└────────┬───────────┴──────────────┬──────────────┘
         │                          │
         ▼                          ▼
┌─────────────────────────────────────────────────┐
│            MicroCMS.WebHost  :8080              │
│  REST API (/api/v1)  │  GraphQL (/graphql)      │
│  Health  (/health/live|ready)                   │
│  Metrics (/metrics)                             │
└──┬──────────┬───────────┬────────────┬──────────┘
   │          │           │            │
   ▼          ▼           ▼            ▼
PostgreSQL  Redis     OpenSearch   OTel Collector
                                    /     \
                                Jaeger  Prometheus
                                            │
                                         Grafana
```

### Key projects

| Project | Role |
|---------|------|
| `MicroCMS.WebHost` | ASP.NET Core composition root — wires all layers |
| `MicroCMS.Api` | Controllers, middleware, problem details |
| `MicroCMS.Application` | CQRS commands/queries, MediatR pipeline |
| `MicroCMS.Domain` | Aggregates, domain events, value objects |
| `MicroCMS.Infrastructure` | EF Core, Redis, OpenSearch, background jobs |
| `MicroCMS.GraphQL` | Hot Chocolate schema, resolvers, subscriptions |
| `MicroCMS.Admin.WebHost` | ASP.NET Core SPA host serving the React Admin UI |

---

## 2. Prerequisites

| Tool | Version | Notes |
|------|---------|-------|
| .NET SDK | 8.0+ | `dotnet --version` |
| Node.js | 18+ | Admin UI only |
| Docker Desktop | 4.x+ | For compose stack & container build |
| PostgreSQL | 15+ | Or use Docker Compose service |
| `helm` CLI | 3.x+ | Kubernetes deployments only |
| `kubectl` | 1.28+ | Kubernetes deployments only |
| `k6` | latest | Load testing only |

Install .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0  
Install Docker Desktop: https://www.docker.com/products/docker-desktop/

---

## 3. Local Development Setup (no Docker)

### 3.1 Clone and restore

```powershell
git clone https://github.com/mknayak/MicroCms
cd MicroCms
dotnet restore
```

### 3.2 Configure the database

The default configuration in `src/MicroCMS.WebHost/appsettings.json` points to a local PostgreSQL instance.  
Edit the connection string to match your environment:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=microcms_db;Username=postgres;Password=YOUR_PASSWORD"
}
```

For a quick SQLite setup (no PostgreSQL required), override in `appsettings.Development.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=App_Data/microcms_dev.db"
},
"MicroCMS": {
  "Database": {
    "Provider": "Sqlite"
  }
}
```

### 3.3 Apply database migrations

```powershell
cd src/MicroCMS.WebHost
dotnet ef database update --project ../MicroCMS.Infrastructure
```

### 3.4 Run the API host

```powershell
cd src/MicroCMS.WebHost
dotnet run
```

The host starts on `http://localhost:5000` (HTTP) and `https://localhost:5001` (HTTPS).

| Endpoint | URL |
|----------|-----|
| Swagger UI | http://localhost:5000/swagger |
| Health live | http://localhost:5000/health/live |
| Health ready | http://localhost:5000/health/ready |
| Prometheus metrics | http://localhost:5000/metrics |
| GraphQL | http://localhost:5000/graphql |

### 3.5 Run the Admin UI (optional)

```powershell
cd src/MicroCMS.Admin.WebHost/ClientApp
npm install
npm run dev        # Vite dev server at http://localhost:5173
```

Or run the ASP.NET Core SPA host (serves built `dist/` in Production):

```powershell
cd src/MicroCMS.Admin.WebHost
dotnet run
```

### 3.6 First-time tenant setup

Onboard the first tenant using the API:

```powershell
$body = @{
  name = "My Tenant"
  slug = "my-tenant"
  adminEmail = "admin@example.com"
  adminPassword = "Admin@12345"
} | ConvertTo-Json

Invoke-RestMethod -Method Post `
  -Uri "http://localhost:5000/api/v1/admin/tenants/onboard" `
  -Body $body -ContentType "application/json"
```

---

## 4. Running the Full Stack with Docker Compose

`docker-compose.yml` at the repository root starts all services:

| Service | Port | Description |
|---------|------|-------------|
| `webhost` | 8080 | MicroCMS API |
| `postgres` | 5432 | PostgreSQL 15 |
| `redis` | 6379 | Redis cache |
| `opensearch` | 9200 | Full-text search |
| `otel-collector` | 4317 (gRPC) | OpenTelemetry collector |
| `jaeger` | 16686 | Trace UI |
| `prometheus` | 9090 | Metrics storage |
| `grafana` | 3000 | Dashboards |

### 4.1 Start everything

```powershell
# From repository root
docker compose up -d

# Wait for services to be healthy (~30 seconds)
docker compose ps
```

### 4.2 Verify the stack

```powershell
# API health check
Invoke-RestMethod http://localhost:8080/health/live

# Prometheus metrics
Invoke-RestMethod http://localhost:8080/metrics

# Open Swagger
Start-Process http://localhost:8080/swagger

# Open Jaeger traces
Start-Process http://localhost:16686

# Open Grafana (admin / admin)
Start-Process http://localhost:3000
```

### 4.3 View logs

```powershell
# Follow application logs
docker compose logs webhost -f

# All services
docker compose logs -f
```

### 4.4 Stop everything

```powershell
docker compose down          # keep volumes (DB data persists)
docker compose down -v       # also delete volumes (full reset)
```

---

## 5. Observability Stack

### How data flows

```
MicroCMS (Serilog OTLP + OTel SDK)
        │
        ▼  gRPC :4317
  OTel Collector
        ├──▶  Jaeger      (traces)
        ├──▶  Loki        (logs, if added)
        └──▶  debug       (stdout)

Prometheus  ──── scrapes /metrics every 15s ────▶  MicroCMS
        │
        ▼
  Grafana  (reads Prometheus + Jaeger)
```

### Enable telemetry

Telemetry is **disabled by default** to keep local runs fast.  
To enable, set in `appsettings.json` or as an environment variable:

```json
"Telemetry": {
  "Enabled": true,
  "OtlpEndpoint": "http://localhost:4317"
}
```

Or as an environment variable:

```
Telemetry__Enabled=true
Telemetry__OtlpEndpoint=http://otel-collector:4317
```

### Validate OTel collector config

```powershell
docker run --rm -v ${PWD}/deploy/otel:/cfg `
  otel/opentelemetry-collector-contrib:latest `
  validate --config=/cfg/collector-config.yaml
```

### Validate Prometheus config

```powershell
docker run --rm -v ${PWD}/deploy/prometheus:/etc/prometheus `
  prom/prometheus `
  promtool check config /etc/prometheus/prometheus.yml
```

### Run a load test (k6)

```powershell
# Install k6 (one-time)
winget install k6

# App must be running first, then:
k6 run tests/load/k6-load-test.js

# Override base URL
$env:BASE_URL = "http://localhost:8080"
k6 run tests/load/k6-load-test.js
```

---

## 6. Running Tests

### All tests

```powershell
dotnet test
```

### By project

```powershell
# Domain unit tests (no dependencies)
dotnet test tests/MicroCMS.Domain.UnitTests

# Application unit tests (no dependencies)
dotnet test tests/MicroCMS.Application.UnitTests

# API contract tests (in-process host)
dotnet test tests/MicroCMS.Api.ContractTests

# E2E tests (in-process host, SQLite)
dotnet test tests/MicroCMS.E2E.Tests

# Architecture tests
dotnet test tests/MicroCMS.Architecture.Tests

# Infrastructure integration tests (requires Docker for PostgreSQL / Redis / MinIO)
dotnet test tests/MicroCMS.Infrastructure.IntegrationTests
```

### Coverage report

```powershell
dotnet test --collect:"XPlat Code Coverage"
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html
Start-Process coverage-report/index.html
```

---

## 7. Building the Docker Image

```powershell
# Build the multi-stage image
docker build -f Dockerfile.webhost -t microcms-webhost:latest .

# Run the image standalone (SQLite, no external dependencies)
docker run -p 8080:8080 `
  -e MicroCMS__Database__Provider=Sqlite `
  -e ConnectionStrings__DefaultConnection="Data Source=/app/data/microcms.db" `
  microcms-webhost:latest

# Verify
Invoke-RestMethod http://localhost:8080/health/live
```

### Tagging for a registry

```powershell
# Azure Container Registry
docker tag microcms-webhost:latest <your-acr>.azurecr.io/microcms-webhost:1.0.0
docker push <your-acr>.azurecr.io/microcms-webhost:1.0.0

# AWS ECR
docker tag microcms-webhost:latest <account>.dkr.ecr.<region>.amazonaws.com/microcms-webhost:1.0.0
docker push <account>.dkr.ecr.<region>.amazonaws.com/microcms-webhost:1.0.0
```

---

## 8. Deploying to Kubernetes (AKS / EKS)

### 8.1 Lint and preview the Helm chart

```powershell
# Lint for errors
helm lint deploy/helm/microcms

# Dry-run render all templates (no cluster needed)
helm template microcms deploy/helm/microcms \
  --set image.repository=<your-acr>.azurecr.io/microcms-webhost \
  --set image.tag=1.0.0
```

---

### 8.2 Deploy to AKS

#### Prerequisites

```powershell
# Install Azure CLI (one-time)
winget install Microsoft.AzureCLI

# Log in
az login

# Get AKS credentials
az aks get-credentials --resource-group <rg-name> --name <aks-cluster-name>

# Verify cluster connection
kubectl get nodes
```

#### Create namespace and secrets

```powershell
kubectl create namespace microcms

# Database connection string
kubectl create secret generic microcms-secrets -n microcms `
  --from-literal=db-connection="Host=<host>;Database=microcms;Username=<user>;Password=<pass>" `
  --from-literal=jwt-secret="<your-jwt-secret-32-chars-min>" `
  --from-literal=redis-connection="<redis-host>:6379"
```

#### Install / upgrade with Helm

```powershell
helm upgrade --install microcms deploy/helm/microcms `
  --namespace microcms `
  --set image.repository=<your-acr>.azurecr.io/microcms-webhost `
  --set image.tag=1.0.0 `
  --set existingSecret=microcms-secrets `
  --set ingress.enabled=true `
  --set ingress.hosts[0].host=api.yourdomain.com `
  --set autoscaling.enabled=true `
  --set autoscaling.minReplicas=2 `
  --set autoscaling.maxReplicas=10 `
  --wait
```

#### Verify the deployment

```powershell
kubectl get pods -n microcms
kubectl get svc -n microcms
kubectl logs -n microcms deployment/microcms -f

# Health check via the cluster
kubectl port-forward -n microcms svc/microcms 8080:80
Invoke-RestMethod http://localhost:8080/health/ready
```

---

### 8.3 Deploy to EKS

#### Prerequisites

```powershell
# Install AWS CLI and eksctl (one-time)
winget install Amazon.AWSCLI
winget install eksctl

# Configure AWS credentials
aws configure

# Update kubeconfig for your cluster
aws eks update-kubeconfig --region <region> --name <cluster-name>

# Verify
kubectl get nodes
```

#### Authenticate Docker with ECR

```powershell
$region = "<your-region>"
$account = (aws sts get-caller-identity --query Account --output text)
aws ecr get-login-password --region $region |
  docker login --username AWS --password-stdin "$account.dkr.ecr.$region.amazonaws.com"

# Push the image
docker push "$account.dkr.ecr.$region.amazonaws.com/microcms-webhost:1.0.0"
```

#### Create secrets and deploy

```powershell
kubectl create namespace microcms

kubectl create secret generic microcms-secrets -n microcms `
  --from-literal=db-connection="Host=<rds-endpoint>;Database=microcms;Username=<user>;Password=<pass>" `
  --from-literal=jwt-secret="<your-jwt-secret>" `
  --from-literal=redis-connection="<elasticache-endpoint>:6379"

helm upgrade --install microcms deploy/helm/microcms `
  --namespace microcms `
  --set image.repository="$account.dkr.ecr.$region.amazonaws.com/microcms-webhost" `
  --set image.tag=1.0.0 `
  --set existingSecret=microcms-secrets `
  --set ingress.enabled=true `
  --set ingress.annotations."kubernetes\.io/ingress\.class"=alb `
  --set ingress.hosts[0].host=api.yourdomain.com `
  --set autoscaling.enabled=true `
  --wait
```

---

### 8.4 Helm values reference

Key values in `deploy/helm/microcms/values.yaml`:

| Key | Default | Description |
|-----|---------|-------------|
| `replicaCount` | `2` | Number of pod replicas |
| `image.repository` | *(required)* | Container image repository |
| `image.tag` | `latest` | Image tag |
| `existingSecret` | `""` | K8s secret name containing DB/JWT credentials |
| `autoscaling.enabled` | `false` | Enable HPA |
| `autoscaling.minReplicas` | `2` | HPA minimum pods |
| `autoscaling.maxReplicas` | `10` | HPA maximum pods |
| `autoscaling.targetCPUUtilizationPercentage` | `70` | HPA CPU threshold |
| `ingress.enabled` | `false` | Enable ingress resource |
| `ingress.hosts[0].host` | `chart-example.local` | Ingress hostname |
| `resources.requests.cpu` | `250m` | CPU request |
| `resources.requests.memory` | `256Mi` | Memory request |
| `resources.limits.cpu` | `1000m` | CPU limit |
| `resources.limits.memory` | `512Mi` | Memory limit |

Override any value at deploy time with `--set key=value` or a custom values file:

```powershell
helm upgrade --install microcms deploy/helm/microcms `
  -f my-production-values.yaml
```

---

### 8.5 Rolling update

```powershell
# Update image tag to new version
helm upgrade microcms deploy/helm/microcms `
  --namespace microcms `
  --reuse-values `
  --set image.tag=1.1.0 `
  --wait

# Check rollout status
kubectl rollout status deployment/microcms -n microcms
```

### 8.6 Rollback

```powershell
# View release history
helm history microcms -n microcms

# Roll back to previous revision
helm rollback microcms -n microcms
```

### 8.7 Uninstall

```powershell
helm uninstall microcms -n microcms
kubectl delete namespace microcms
```

---

## 9. Environment Variable Reference

All `appsettings.json` keys can be overridden as environment variables using `__` as the separator.

| Variable | Example Value | Description |
|----------|--------------|-------------|
| `ConnectionStrings__DefaultConnection` | `Host=db;Database=microcms;...` | PostgreSQL connection string |
| `MicroCMS__Database__Provider` | `PostgreSql` \| `Sqlite` | Database provider |
| `MicroCMS__Cache__Provider` | `InMemory` \| `Redis` | Cache provider |
| `MicroCMS__Cache__ConnectionString` | `redis:6379` | Redis connection string |
| `MicroCMS__Search__Provider` | `None` \| `OpenSearch` | Search provider |
| `MicroCMS__Search__Endpoint` | `http://opensearch:9200` | OpenSearch endpoint |
| `MicroCMS__Storage__Provider` | `DATABASE` \| `S3` \| `AzureBlob` | Media storage provider |
| `TrustedClients__Admin__Secret` | *(32+ char random string)* | JWT signing secret |
| `TrustedClients__Admin__Issuer` | `microcms-admin` | JWT issuer |
| `TrustedClients__Admin__Audience` | `microcms-api` | JWT audience |
| `Telemetry__Enabled` | `true` \| `false` | Enable OTel export |
| `Telemetry__OtlpEndpoint` | `http://otel-collector:4317` | OTel collector gRPC address |
| `ASPNETCORE_ENVIRONMENT` | `Development` \| `Production` | ASP.NET environment |
| `ASPNETCORE_URLS` | `http://+:8080` | Listening addresses |

---

## 10. Secrets Rotation

See [`docs/secrets-rotation-runbook.md`](docs/secrets-rotation-runbook.md) for step-by-step procedures covering:

- JWT signing key rotation (zero-downtime rolling restart)
- Database password rotation
- Redis password rotation
- AI provider API key rotation
- Post-rotation verification checklist

---

## Contributing

1. Fork the repository and create a feature branch.
2. Follow the [Definition of Done](docs/DEVELOPMENT_PLAN.md#definition-of-done-every-sprint).
3. Run `dotnet build` (zero warnings) and `dotnet test` (all pass) before opening a PR.
4. Architecture tests (`MicroCMS.Architecture.Tests`) must pass — they block merge on failure.

## License

[MIT](LICENSE)
