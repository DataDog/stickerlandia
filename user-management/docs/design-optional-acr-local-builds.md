# Design: Optional Azure Container Registry for Local Builds

Generated: 2026-02-02
Status: Approved

## Problem Statement

### Goal
Enable optional deployment from locally-built container images via Azure Container Registry (ACR), while maintaining the existing public GHCR deployment path as the default. This supports development workflows where:
1. First deploy creates infrastructure including ACR
2. Developer builds and pushes images to ACR
3. Second deploy creates Container Apps using those images

### Constraints
- Must maintain backward compatibility with existing GHCR workflow
- Container images must ALWAYS be built for AMD64 architecture regardless of host OS (Apple Silicon, ARM, etc.)
- Must support a two-phase deployment: infrastructure-only (ACR), then full deployment (apps)
- ACR should be created in shared infrastructure when using shared mode, or inline for ephemeral environments
- Managed identity authentication preferred over admin credentials

### Success Criteria
- [ ] Existing deployments continue to work unchanged (default behavior)
- [ ] New `create_acr` variable controls ACR creation
- [ ] New `deploy_container_apps` variable controls Container App creation
- [ ] ACR uses managed identity for image pull (no secrets)
- [ ] README includes clear instructions for local build + push workflow
- [ ] All images built as AMD64 regardless of host architecture

## Context

### Current State
The Terraform configuration under `infra/azure/` currently:
- Uses `container_image_registry` variable defaulting to `ghcr.io/datadog/stickerlandia`
- Creates three Container Apps: API, Worker, and Migration Job
- No ACR exists in either shared or service-specific infrastructure
- Container Apps have no registry block (uses anonymous pull from public registries)

### Related Decisions
- [ADR-001](adr/ADR-001-azure-container-apps-production-architecture.md): Mentions ACR Premium with private endpoint as part of Option B (Secure Production)

## Alternatives Considered

### Option A: ACR in Service Infrastructure (Simple)

**Summary**: Add optional ACR resource directly to the user-management service Terraform, controlled by a single variable.

**Architecture**:
```
user-management/infra/azure/
├── main.tf
├── application.tf
├── registry.tf (NEW - optional ACR)
└── variables.tf (add create_acr, deploy_container_apps)
```

**Pros**:
- Simplest implementation - single directory to modify
- No coordination with shared infrastructure
- Self-contained within the service

**Cons**:
- ACR created per-service (wasteful if multiple services need it)
- Doesn't follow established pattern of shared resources (PostgreSQL, Service Bus, Key Vault)
- Registry naming must include service prefix to avoid conflicts

**Coupling Analysis**:
| Component | Ca | Ce | I |
|-----------|----|----|---|
| registry.tf | 1 | 2 | 0.67 |
| application.tf | 0 | 3 | 1.0 |

New dependencies introduced:
- application.tf -> registry.tf (when ACR enabled)
- app_identity -> ACR (AcrPull role assignment)

Coupling impact: Low (self-contained within service)

**Failure Modes**:
| Mode | Severity | Occurrence | Detection | RPN |
|------|----------|------------|-----------|-----|
| ACR not created before push | Medium | High (first deploy) | Easy (az acr fails) | 6 |
| Apps created before images pushed | High | High (first deploy) | Medium (deploy fails) | 12 |
| Identity lacks AcrPull role | Medium | Low | Easy (pull fails) | 3 |

**Evolvability Assessment**:
- Adding more services: Hard - each service creates own ACR
- Moving to shared ACR later: Medium - requires state migration
- Adding private endpoint: Easy - add to same module

### Option B: ACR in Shared Infrastructure (Recommended)

**Summary**: Add optional ACR to the shared-resources module, following the established pattern for PostgreSQL, Service Bus, and Key Vault. Service-specific Terraform looks up the ACR when enabled.

**Architecture**:
```
shared/infra/azure/modules/shared-resources/
├── registry.tf (NEW - optional ACR)
├── outputs.tf (add ACR outputs)
└── variables.tf (add create_acr)

user-management/infra/azure/
├── main.tf (add ACR lookup)
├── application.tf (add registry block conditionally)
└── variables.tf (add use_acr, deploy_container_apps)
```

**Pros**:
- Follows established pattern for shared resources
- Single ACR serves all services (cost-efficient)
- Consistent with ADR-001 architecture vision
- Clean separation of concerns

**Cons**:
- Requires changes to two Terraform configurations
- More complex conditional logic
- Must coordinate shared and service deployments

**Coupling Analysis**:
| Component | Ca | Ce | I |
|-----------|----|----|---|
| shared/registry.tf | 2+ | 2 | 0.5 |
| service/application.tf | 0 | 4 | 1.0 |
| service/main.tf | 2 | 3 | 0.6 |

New dependencies introduced:
- All services -> shared ACR (when enabled)
- Service identities -> ACR via role assignments

Coupling impact: Medium (adds cross-module dependency, but follows existing pattern)

**Failure Modes**:
| Mode | Severity | Occurrence | Detection | RPN |
|------|----------|------------|-----------|-----|
| Shared ACR not created | Medium | Low (clear workflow) | Easy | 3 |
| Service lookup fails | Medium | Low | Easy | 3 |
| Cross-service image conflicts | Low | Low | Medium | 2 |
| Role assignment missing | Medium | Low | Easy | 3 |

**Evolvability Assessment**:
- Adding more services: Easy - just add ACR lookup
- Adding private endpoint: Easy - add to shared module
- Switching services to ACR: Easy - flip variable per service

### Option C: Service-Only with Deployment Phases (Pragmatic)

**Summary**: Add ACR to service infrastructure with explicit two-phase deployment control. Simpler than Option B, avoids shared infrastructure changes, but supports the two-phase workflow cleanly.

**Architecture**:
```
user-management/infra/azure/
├── main.tf
├── application.tf (conditional on deploy_container_apps)
├── registry.tf (NEW - conditional on create_acr)
└── variables.tf (add create_acr, deploy_container_apps)
```

Workflow:
1. `terraform apply -var="create_acr=true" -var="deploy_container_apps=false"` - Creates ACR only
2. Build and push images to ACR
3. `terraform apply -var="create_acr=true" -var="deploy_container_apps=true"` - Creates apps

**Pros**:
- Self-contained - no shared infrastructure changes required
- Clear two-phase deployment model
- Simple to understand and operate
- Maintains backward compatibility (both variables default to appropriate values)

**Cons**:
- ACR per service (though most deployments may only have one service)
- Diverges from shared resource pattern
- Requires explicit coordination of variable values

**Coupling Analysis**:
| Component | Ca | Ce | I |
|-----------|----|----|---|
| registry.tf | 1 | 2 | 0.67 |
| application.tf | 0 | 3 | 1.0 |

Coupling impact: Low

**Failure Modes**:
| Mode | Severity | Occurrence | Detection | RPN |
|------|----------|------------|-----------|-----|
| Deploy apps before ACR | Medium | Low (clear docs) | Easy | 3 |
| Deploy apps before push | High | Medium | Medium (clear error) | 8 |
| Wrong variable combo | Low | Low | Easy | 2 |

**Evolvability Assessment**:
- Adding private endpoint: Easy
- Multiple services sharing ACR: Would need migration to shared model
- Switching back to GHCR: Easy - just change variables

## Comparison Matrix

| Criterion | Option A | Option B | Option C |
|-----------|----------|----------|----------|
| Complexity | Low | Medium | Low |
| Follows Patterns | No | Yes | Partial |
| Changes Required | 1 directory | 2 directories | 1 directory |
| Multi-Service Ready | No | Yes | No |
| Two-Phase Deploy | Awkward | Clean | Clean |
| Failure Resilience | Medium | High | Medium |
| Backward Compat | Yes | Yes | Yes |

## Recommendation

**Recommended Option**: Option C (Service-Only with Deployment Phases)

**Rationale**:
Given the constraints and current state:
1. The request is specifically for the user-management service, not a platform-wide change
2. Shared infrastructure module is in a separate directory and changing it would expand scope
3. The two-phase deployment workflow (ACR first, then apps) is a core requirement
4. Option C provides the cleanest implementation with the fewest moving parts

If multi-service ACR sharing becomes a requirement later, the ACR can be migrated to shared infrastructure with a straightforward state move operation.

**Tradeoffs Accepted**:
- Single-service ACR: Acceptable because this is the only service currently needing local builds
- Not following shared pattern: Acceptable because adding to shared infrastructure is out of scope for this request

**Risks to Monitor**:
- If more services need local builds, consider migrating to Option B
- Monitor ACR costs if multiple ephemeral environments are created

## Implementation Plan

### Phase 1: Variables and Registry
- [ ] Add `create_acr` variable (default: false)
- [ ] Add `deploy_container_apps` variable (default: true)
- [ ] Create `registry.tf` with conditional ACR resource
- [ ] Add AcrPull role assignment for app identity

### Phase 2: Application Updates
- [ ] Make Container App resources conditional on `deploy_container_apps`
- [ ] Add `registry` block to Container Apps when using ACR
- [ ] Update locals to compute correct image registry path

### Phase 3: Outputs and Documentation
- [ ] Add ACR-related outputs (login server, name)
- [ ] Create/update README with deployment workflow
- [ ] Include AMD64 build instructions for all platforms

## Detailed Implementation

### New Variables (variables.tf)

```hcl
variable "create_acr" {
  description = "Whether to create an Azure Container Registry for local builds"
  type        = bool
  default     = false
}

variable "deploy_container_apps" {
  description = "Whether to deploy Container Apps. Set to false for first deploy to create ACR, then true after pushing images."
  type        = bool
  default     = true
}
```

### New Registry File (registry.tf)

```hcl
resource "azurerm_container_registry" "acr" {
  count               = var.create_acr ? 1 : 0
  name                = "acrstickerlandia${var.env}"
  resource_group_name = local.shared.resource_group_name
  location            = local.shared.resource_group_location
  sku                 = "Basic"
  admin_enabled       = false
  tags                = local.tags
}

resource "azurerm_role_assignment" "acr_pull" {
  count                = var.create_acr ? 1 : 0
  scope                = azurerm_container_registry.acr[0].id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_user_assigned_identity.app_identity.principal_id
}
```

### Container App Registry Block (application.tf)

```hcl
# Add to each Container App when using ACR:
dynamic "registry" {
  for_each = var.create_acr ? [1] : []
  content {
    server   = azurerm_container_registry.acr[0].login_server
    identity = azurerm_user_assigned_identity.app_identity.id
  }
}
```

### Conditional Resource Creation

All Container App resources (`azurerm_container_app.api`, `azurerm_container_app.worker`, `azurerm_container_app_job.migration`) will be wrapped with:

```hcl
count = var.deploy_container_apps ? 1 : 0
```

### Image Registry Logic

```hcl
locals {
  # Compute the effective image registry
  effective_registry = var.create_acr ? azurerm_container_registry.acr[0].login_server : var.container_image_registry
}
```

### README Section

````markdown
## Deploying with Local Container Builds

By default, this Terraform deploys Container Apps using images from the public GHCR registry.
To deploy from locally-built images, follow this two-phase process:

### Phase 1: Create ACR

```bash
cd infra/azure
terraform apply -var-file=dev.tfvars \
  -var="create_acr=true" \
  -var="deploy_container_apps=false"
```

Note the ACR login server from the outputs.

### Phase 2: Build and Push Images (AMD64)

**IMPORTANT**: Images must be built for AMD64 architecture regardless of your host OS.

```bash
# Login to ACR
az acr login --name acrstickerlandiadev

# Build and push AMD64 images (works on any host including Apple Silicon)
docker buildx build --platform linux/amd64 \
  -t acrstickerlandiadev.azurecr.io/user-management-service:latest \
  -f src/Stickerlandia.UserManagement.Api/Dockerfile \
  --push .

docker buildx build --platform linux/amd64 \
  -t acrstickerlandiadev.azurecr.io/user-management-worker:latest \
  -f src/Stickerlandia.UserManagement.Worker/Dockerfile \
  --push .

docker buildx build --platform linux/amd64 \
  -t acrstickerlandiadev.azurecr.io/user-management-migration:latest \
  -f src/Stickerlandia.UserManagement.MigrationService/Dockerfile \
  --push .
```

### Phase 3: Deploy Container Apps

```bash
terraform apply -var-file=dev.tfvars \
  -var="create_acr=true" \
  -var="deploy_container_apps=true" \
  -var="container_image_registry=acrstickerlandiadev.azurecr.io"
```

### Subsequent Deployments

For subsequent deployments with new images:
1. Build and push new images to ACR
2. Run terraform apply (apps will pull latest images on next revision)
````

## Open Questions
- [x] Should ACR use Basic or Standard SKU? **Decision: Basic SKU** - sufficient for development workflows
- [x] Should we add ACR private endpoint for VNet-only access? **Decision: No** - ACR should be publicly accessible for local pushes
