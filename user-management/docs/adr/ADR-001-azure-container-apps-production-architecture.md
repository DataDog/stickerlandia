# ADR-001: Azure Container Apps Production Architecture

Date: 2026-02-02
Status: Accepted

## Context

The User Management Service needs to be deployed to Azure in a production-ready configuration. The service currently has:

1. **AWS CDK deployment** - Full production deployment with ECS Fargate, Aurora PostgreSQL, Lambda workers, CloudFront, and comprehensive Datadog integration
2. **Azure Terraform (existing)** - Minimal deployment with single-replica Container App, no managed database, plaintext secrets, and no network isolation

The Azure deployment needs to reach production parity with appropriate security controls, high availability, and observability while remaining cost-effective and maintainable.

### Key Requirements
- Azure Container Apps as the compute platform (not AKS)
- Azure Database for PostgreSQL Flexible Server
- Integration with existing Service Bus messaging
- Datadog as the primary observability platform
- Terraform as IaC tool (existing pattern)

### Alternatives Evaluated
See [design-azure-container-apps-production.md](../design-azure-container-apps-production.md) for detailed analysis of:
- **Option A**: Minimal Production (public endpoints, basic security)
- **Option B**: Secure Production (VNet integrated, private endpoints)
- **Option C**: AWS-Parity Production (full feature set with Functions)

## Decision

We will implement **Option B: Secure Production (VNet Integrated)** architecture.

### Architecture Components

1. **Networking**
   - Virtual Network (10.0.0.0/16) with dedicated subnets
   - Container App Environment subnet (10.0.0.0/23 - /23 required by Azure)
   - Private Endpoint subnet (10.0.2.0/24)
   - Private DNS Zones for Azure PaaS services

2. **Compute**
   - Container App Environment with VNet integration (Consumption workload profile)
   - API Container App (2-10 replicas, HTTP scaling)
   - Worker Container App (1-3 replicas, Service Bus KEDA scaling)
   - Container App Job for database migrations

3. **Data**
   - Azure Database for PostgreSQL Flexible Server (General Purpose D2s_v3)
   - Private endpoint (no public access)
   - Automated backups enabled

4. **Messaging**
   - Azure Service Bus Standard (cost-optimized)
   - VNet Service Endpoint (traffic via Azure backbone)
   - Firewall rules restricting access to VNet only
   - Existing queues and topics preserved

5. **Security**
   - Azure Key Vault with private endpoint
   - Managed identities for all services
   - RBAC-based access to Key Vault secrets
   - No plaintext secrets in configuration

6. **Container Registry**
   - Azure Container Registry Premium (private endpoint)
   - Managed identity pull authentication

7. **External Access**
   - Azure Front Door (Standard or Premium based on WAF needs)
   - Origin pointing to Container App Environment

8. **Observability**
   - Datadog sidecar containers in all apps
   - Log Analytics Workspace (required by Container Apps)
   - Datadog APM, logs, and infrastructure metrics

## Consequences

### Positive
- **Enterprise Security**: All backend services accessible only via private endpoints
- **Compliance Ready**: Network isolation meets SOC2, HIPAA, and similar requirements
- **Scalable Foundation**: VNet infrastructure supports future growth and additional services
- **Consistent with AWS**: Similar security posture to existing AWS deployment
- **Cost Predictable**: Fixed infrastructure costs with consumption-based compute

### Negative
- **Higher Cost**: Premium SKUs required for private endpoints (~$400-800/month vs ~$150-300)
- **Increased Complexity**: More Terraform resources and networking concepts to manage
- **Debugging Difficulty**: Private networking complicates troubleshooting
- **Environment Recreation Risk**: VNet configuration errors require full environment rebuild

### Neutral
- Service Bus upgrade from Standard to Premium required
- Log Analytics Workspace added (Azure requirement, not used for primary monitoring)
- Azure Front Door replaces direct Container App ingress

## Implementation Notes

### Terraform Module Structure
```
infra/azure/
  main.tf              # Resource group, locals
  networking.tf        # VNet, subnets, NSGs, private DNS
  database.tf          # PostgreSQL Flexible Server
  keyvault.tf          # Key Vault and secrets
  registry.tf          # Azure Container Registry
  messaging.tf         # Service Bus (updated to Premium)
  container-apps.tf    # Environment, API, Worker, Migration Job
  frontdoor.tf         # Azure Front Door
  monitoring.tf        # Log Analytics Workspace
  variables.tf         # Input variables
  outputs.tf           # Output values
  providers.tf         # Azure provider config
```

### Key Configuration Decisions
1. **PostgreSQL Tier**: Burstable B1ms (1 vCore, 2GB RAM) - cheapest tier, can scale up later
2. **Azure Front Door**: Standard tier (no WAF) - cost-effective for current needs
3. **Container App Scaling**: HTTP concurrent requests (API), Service Bus message count (Worker)
4. **Migration Strategy**: Container App Job runs on every deployment
5. **Secrets Pattern**: Key Vault references in Container App configuration
6. **Custom Domain**: Not required - will use Azure-provided Front Door domain

### Migration Path from Current State
1. Deploy new VNet-integrated environment in parallel
2. Migrate database (dump/restore or Azure Database Migration Service)
3. Update DNS/Front Door to point to new environment
4. Decommission old environment

## Related Decisions
- Service Bus was chosen over Kafka for Azure (existing decision)
- Datadog over Azure Monitor (organizational standard)
- Terraform over Bicep (team preference and AWS CDK parity)

## Notes
- PostgreSQL Flexible Server supports high availability with zone redundancy (not enabled initially)
- Container Apps Jobs are GA and preferred over one-time containers for migrations
- Azure Front Door supports managed certificates for custom domains
- KEDA Service Bus scaler is built into Container Apps (no additional KEDA installation needed)
