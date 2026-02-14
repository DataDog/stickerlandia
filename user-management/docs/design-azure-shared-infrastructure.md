# Design: Azure Shared Infrastructure Split

Generated: 2026-02-02
Status: Draft

## Problem Statement

### Goal
Split the Azure Terraform infrastructure into shared (environment-wide) and service-specific components, following the pattern established in the AWS CDK shared infrastructure.

### Constraints
- Must follow the existing AWS shared infrastructure patterns
- Shared resources should be deployed once per environment
- Services should reference shared resources via data sources or outputs
- Each service should be independently deployable after shared infra exists

### Success Criteria
- [ ] Clear separation between shared and service-specific Terraform
- [ ] Services can be added without modifying shared infrastructure
- [ ] Reduced cost through resource sharing (VNet, PostgreSQL cluster, Service Bus)
- [ ] Consistent with AWS shared infrastructure patterns

## Context

### AWS Shared Infrastructure Pattern

The AWS deployment splits infrastructure as follows:

**Shared (`shared/infra/aws/`):**
- VPC with subnets (public, private, isolated)
- API Gateway + VPC Link
- CloudFront distribution
- Aurora PostgreSQL cluster (shared database server)
- EventBridge Event Bus
- Service Discovery namespace
- SSM Parameters for cross-stack references

**Service-Specific (`user-management/infra/aws/`):**
- ECS Cluster
- Service's database (within shared cluster)
- SNS topics / SQS queues
- Lambda functions / ECS services
- API Gateway routes

### Current Azure State

All resources are currently in one service-specific folder (`user-management/infra/azure/`).

## Recommended Split

### Shared Infrastructure (`shared/infra/azure/`)

| Resource | File | Rationale |
|----------|------|-----------|
| Resource Group | `main.tf` | Single RG for all shared resources |
| Virtual Network | `networking.tf` | One VNet per environment, services share subnets |
| Subnets | `networking.tf` | Container Apps, Private Endpoints, PostgreSQL |
| Private DNS Zones | `networking.tf` | Shared DNS for private endpoints |
| Log Analytics Workspace | `monitoring.tf` | Single workspace for all services |
| PostgreSQL Flexible Server | `database.tf` | Shared database server, services create own DBs |
| Service Bus Namespace | `messaging.tf` | Shared namespace, services create own queues/topics |
| Key Vault (shared) | `keyvault.tf` | Shared secrets (Datadog API key, etc.) |
| Azure Front Door Profile | `frontdoor.tf` | Single CDN/load balancer entry point |
| Front Door Endpoint | `frontdoor.tf` | Shared endpoint, services add routes |

### Service-Specific Infrastructure (`user-management/infra/azure/`)

| Resource | File | Rationale |
|----------|------|-----------|
| Container App Environment | `application.tf` | Could be shared, but isolation is safer |
| Container Apps (API, Worker) | `application.tf` | Service-specific workloads |
| Container App Job (Migration) | `application.tf` | Service-specific migrations |
| User Assigned Identity | `application.tf` | Service-specific RBAC |
| PostgreSQL Database | `database.tf` | Database within shared server |
| Service Bus Queue/Topic | `messaging.tf` | Service-specific messaging |
| Key Vault Secrets | `keyvault.tf` | Service-specific secrets (DB connection string) |
| Front Door Origin + Routes | `frontdoor.tf` | Service-specific routing |

## Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     SHARED INFRASTRUCTURE (per environment)                  │
│                         shared/infra/azure/                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                    Azure Front Door Profile                             │ │
│  │                    (users-api-dev endpoint)                             │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                    Virtual Network (10.0.0.0/16)                        │ │
│  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────────┐ │ │
│  │  │ Container Apps  │  │ Private Endpts  │  │ PostgreSQL Subnet       │ │ │
│  │  │ (10.0.0.0/23)   │  │ (10.0.2.0/24)   │  │ (10.0.3.0/24)           │ │ │
│  │  └─────────────────┘  └─────────────────┘  └─────────────────────────┘ │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────────────┐  │
│  │ PostgreSQL       │  │ Service Bus      │  │ Key Vault (shared)       │  │
│  │ Flexible Server  │  │ Namespace        │  │ - dd-api-key             │  │
│  │ (shared cluster) │  │ (Standard)       │  │ - shared secrets         │  │
│  └──────────────────┘  └──────────────────┘  └──────────────────────────┘  │
│                                                                              │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │ Log Analytics Workspace        │ Private DNS Zones                    │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│  OUTPUTS: vnet_id, subnet_ids, postgresql_fqdn, servicebus_namespace_id,    │
│           keyvault_id, frontdoor_profile_id, log_analytics_workspace_id     │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                SERVICE-SPECIFIC (user-management/infra/azure/)               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  DATA SOURCES: References shared infra via terraform_remote_state or        │
│                data "azurerm_*" lookups                                      │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                    Container App Environment                            │ │
│  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────────┐ │ │
│  │  │ API Container   │  │ Worker Container│  │ Migration Job           │ │ │
│  │  │ App (2-10)      │  │ App (1-3)       │  │ (on deploy)             │ │ │
│  │  └─────────────────┘  └─────────────────┘  └─────────────────────────┘ │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────────────┐  │
│  │ PostgreSQL DB    │  │ Service Bus      │  │ Key Vault Secrets        │  │
│  │ stickerlandia_   │  │ - queue: sticker │  │ - db-connection-string   │  │
│  │ users            │  │ - topic: user-   │  │ - servicebus-conn-str    │  │
│  │ (in shared srv)  │  │   registered     │  │                          │  │
│  └──────────────────┘  └──────────────────┘  └──────────────────────────┘  │
│                                                                              │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │ Front Door Origin + Routes: /api/users/*, /auth/*, /.well-known/*    │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Cross-Stack Reference Pattern

### Option A: Terraform Remote State (Recommended)

```hcl
# In service Terraform
data "terraform_remote_state" "shared" {
  backend = "azurerm"
  config = {
    resource_group_name  = "tfstate-rg"
    storage_account_name = "tfstatestickerlandia"
    container_name       = "tfstate"
    key                  = "shared/${var.env}/terraform.tfstate"
  }
}

# Reference shared resources
resource "azurerm_container_app_environment" "main" {
  infrastructure_subnet_id   = data.terraform_remote_state.shared.outputs.container_apps_subnet_id
  log_analytics_workspace_id = data.terraform_remote_state.shared.outputs.log_analytics_workspace_id
}
```

### Option B: Data Source Lookups

```hcl
# In service Terraform - lookup by naming convention
data "azurerm_virtual_network" "shared" {
  name                = "vnet-stickerlandia-${var.env}"
  resource_group_name = "rg-shared-${var.env}"
}

data "azurerm_subnet" "container_apps" {
  name                 = "snet-container-apps"
  virtual_network_name = data.azurerm_virtual_network.shared.name
  resource_group_name  = data.azurerm_virtual_network.shared.resource_group_name
}
```

## File Structure

```
stickerlandia/
├── shared/
│   └── infra/
│       ├── aws/                    # Existing AWS CDK
│       └── azure/                  # NEW: Shared Azure Terraform
│           ├── main.tf             # Resource group, locals
│           ├── networking.tf       # VNet, subnets, private DNS
│           ├── database.tf         # PostgreSQL Flexible Server
│           ├── messaging.tf        # Service Bus namespace
│           ├── keyvault.tf         # Shared Key Vault
│           ├── frontdoor.tf        # Front Door profile + endpoint
│           ├── monitoring.tf       # Log Analytics
│           ├── variables.tf        # Input variables
│           ├── outputs.tf          # Outputs for services
│           └── providers.tf        # Provider config
│
└── user-management/
    └── infra/
        ├── aws/                    # Existing AWS CDK
        └── azure/                  # Service-specific Azure Terraform
            ├── main.tf             # Data sources for shared infra
            ├── application.tf      # Container Apps
            ├── database.tf         # PostgreSQL database (not server)
            ├── messaging.tf        # Service Bus queues/topics
            ├── keyvault.tf         # Service-specific secrets
            ├── frontdoor.tf        # Service-specific routes
            ├── variables.tf        # Input variables
            ├── outputs.tf          # Service outputs
            └── providers.tf        # Provider config
```

## Migration Plan

### Phase 1: Create Shared Infrastructure
1. Create `shared/infra/azure/` directory
2. Move shared resources from current user-management Terraform
3. Add outputs for all shared resources
4. Deploy shared infrastructure

### Phase 2: Refactor Service Infrastructure
1. Update user-management Terraform to use data sources
2. Remove resources that are now shared
3. Add database creation within shared PostgreSQL server
4. Test deployment

### Phase 3: Validate
1. Verify all services can access shared resources
2. Test adding a hypothetical second service
3. Document the pattern for future services

## Open Questions

- [ ] Should Container App Environment be shared or per-service?
  - **Shared**: Cost savings, simpler networking
  - **Per-service**: Better isolation, independent scaling
- [ ] Should each service have its own Key Vault or share one?
  - **Shared**: Simpler management
  - **Per-service**: Better secret isolation
- [ ] Terraform state backend configuration for shared vs service states?
