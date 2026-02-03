# ADR-002: Optional Azure Container Registry for Local Builds

Date: 2026-02-02
Status: Accepted

## Context

The current Azure Terraform configuration deploys Container Apps using images from the public GitHub Container Registry (ghcr.io/datadog/stickerlandia). While this works well for CI/CD pipelines that automatically publish to GHCR, it creates friction for local development and testing scenarios where developers want to:

1. Build container images locally
2. Push them to a private registry
3. Deploy to Azure for testing

The challenge is that ACR must exist before images can be pushed, but Container Apps need images to exist before they can be deployed. This requires a two-phase deployment approach.

Additionally, developers using Apple Silicon Macs or other ARM-based machines must ensure images are built for AMD64 architecture, as Azure Container Apps runs on AMD64 infrastructure.

## Decision

We will add optional ACR support to the service infrastructure with explicit two-phase deployment control.

### Implementation

1. **New Variables**:
   - `create_acr` (bool, default: false) - Controls ACR creation
   - `deploy_container_apps` (bool, default: true) - Controls Container App creation

2. **New Resources** (when `create_acr=true`):
   - Azure Container Registry (Basic SKU)
   - AcrPull role assignment for the service managed identity

3. **Modified Resources** (when `deploy_container_apps=false`):
   - Container Apps (API, Worker, Migration Job) are not created

4. **Registry Authentication**:
   - Managed identity authentication (no admin credentials)
   - Registry block added to Container Apps when using ACR

### Deployment Workflow

```bash
# Phase 1: Create infrastructure including ACR
terraform apply -var="create_acr=true" -var="deploy_container_apps=false"

# Phase 2: Build and push AMD64 images
docker buildx build --platform linux/amd64 -t <acr>.azurecr.io/image:tag --push .

# Phase 3: Deploy Container Apps
terraform apply -var="create_acr=true" -var="deploy_container_apps=true"
```

## Consequences

### Positive
- Enables local build and test workflows without modifying CI/CD
- No changes required to shared infrastructure
- Backward compatible - existing deployments work unchanged
- Managed identity authentication is secure (no secrets)
- Clear two-phase model prevents ordering issues

### Negative
- ACR is per-service rather than shared across services
- Requires explicit variable coordination between deployment phases
- Additional cost (~$5/month for Basic ACR) when enabled

### Neutral
- README documentation required for the workflow
- Developers must remember to use `--platform linux/amd64` on ARM machines

## Alternatives Considered

### ACR in Shared Infrastructure
Adding ACR to the shared-resources module would follow the established pattern for PostgreSQL, Service Bus, and Key Vault. This was rejected because:
- Expands scope beyond the user-management service
- Adds complexity for a feature currently needed by one service
- Can be migrated to shared infrastructure later if needed

### Single Variable Control
Using a single variable like `use_local_registry` was considered but rejected because it doesn't clearly separate the "create ACR" step from the "deploy apps" step, making the two-phase workflow confusing.

## Related Decisions
- [ADR-001](ADR-001-azure-container-apps-production-architecture.md): References ACR Premium with private endpoint as part of the secure production architecture

## Notes
- Basic SKU ACR is sufficient for development; upgrade to Standard/Premium for production features like geo-replication or private endpoints
- The `container_image_registry` variable must be updated to point to ACR login server when using local builds
- Future enhancement: Consider adding ACR private endpoint for VNet-only access
