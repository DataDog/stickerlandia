# Azure Infrastructure for User Management Service

Terraform configuration for deploying the User Management Service to Azure Container Apps.

## Prerequisites

- Azure CLI installed and authenticated (`az login`)
- Terraform >= 1.0
- Shared infrastructure deployed (see `shared/infra/azure/`)
- Docker with buildx support (for local builds)

## Standard Deployment (from GHCR)

Deploy using pre-built images from the public GitHub Container Registry:

```bash
cd infra/azure
terraform init
terraform apply -var-file=dev.tfvars
```

## Deploying with Local Container Builds

To deploy from locally-built images instead of GHCR, follow this two-phase process.

### Phase 1: Create ACR (without Container Apps)

```bash
cd infra/azure
terraform init
terraform apply -var-file=dev.tfvars \
  -var="create_acr=true" \
  -var="deploy_container_apps=false"
```

Note the `acr_login_server` from the outputs (e.g., `acrstickerlandiadev.azurecr.io`).

### Phase 2: Build and Push Images (AMD64)

**IMPORTANT**: Images must be built for AMD64 architecture regardless of your host OS (including Apple Silicon Macs).

```bash
# Login to ACR
az acr login --name acrstickerlandiadev

mise build:docker-dev
docker tag docker.io/stickerlandia/user-management-service:dev acrstickerlandiadev.azurecr.io/user-management-service:latest
docker tag docker.io/stickerlandia/user-management-worker:dev acrstickerlandiadev.azurecr.io/user-management-worker:latest
docker tag docker.io/stickerlandia/user-management-migration:dev acrstickerlandiadev.azurecr.io/user-management-migration:latest
docker push acrstickerlandiadev.azurecr.io/user-management-service:latest
docker push acrstickerlandiadev.azurecr.io/user-management-worker:latest
docker push acrstickerlandiadev.azurecr.io/user-management-migration:latest
```

### Phase 3: Deploy Container Apps

```bash
terraform apply -var-file=dev.tfvars \
  -var="create_acr=true" \
  -var="deploy_container_apps=true"
```

### Subsequent Deployments

For subsequent deployments with new images:

1. Build and push new images to ACR (Phase 2 commands)
2. Run terraform apply to create a new revision:
   ```bash
   terraform apply -var-file=dev.tfvars \
     -var="create_acr=true" \
     -var="deploy_container_apps=true" \
     -var="app_version=v1.2.3"
   ```

## Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `subscription_id` | Azure Subscription ID | (required) |
| `env` | Environment name (dev, staging, prod) | (required) |
| `location` | Azure region | `uksouth` |
| `app_version` | Container image tag | `latest` |
| `container_image_registry` | Registry base path | `ghcr.io/datadog/stickerlandia` |
| `create_acr` | Create Azure Container Registry | `false` |
| `deploy_container_apps` | Deploy Container Apps | `true` |
| `use_shared_infrastructure` | Use existing shared resources | `true` |
| `shared_keyvault_suffix` | Key Vault suffix (when using shared) | (required when shared) |

## Outputs

| Output | Description |
|--------|-------------|
| `api_url` | Public URL for the API |
| `acr_login_server` | ACR login server (when `create_acr=true`) |
| `acr_name` | ACR name (when `create_acr=true`) |
| `container_apps_deployed` | Whether apps were deployed |

## Architecture

See [ADR-001](../../docs/adr/ADR-001-azure-container-apps-production-architecture.md) for the full architecture decision.

The deployment creates:
- User-assigned managed identity
- Azure Container Registry (optional, when `create_acr=true`)
- Container App Job for migrations
- API Container App (2-10 replicas)
- Worker Container App (1-3 replicas, Service Bus scaling)
- Front Door origin group, origin, and routes
- PostgreSQL database
- Service Bus queue and topic
- Key Vault secrets
