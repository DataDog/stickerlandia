# Design: Azure Container Apps Production Deployment

Generated: 2026-02-02
Status: Approved

## Decisions Made
- **Azure Front Door**: Standard tier (no WAF)
- **PostgreSQL Tier**: Burstable B1ms (cheapest)
- **Custom Domain**: Not required
- **Migration Strategy**: Run on every deployment
- **Service Bus**: Standard tier with VNet Service Endpoints (not Premium with Private Endpoints)

## Problem Statement

### Goal
Deploy the User Management Service to Azure Container Apps in a "production-ready" configuration that provides comparable capabilities to the existing AWS deployment, including:
- High availability and auto-scaling
- Secure networking and secrets management
- Managed PostgreSQL database
- Reliable messaging with Service Bus
- Comprehensive observability with Datadog
- Database migration strategy
- Background worker processing

### Constraints
- Must use Azure Container Apps (not AKS or App Service)
- Must use Azure Database for PostgreSQL (Flexible Server)
- Must integrate with existing Service Bus messaging
- Must maintain Datadog observability parity with AWS deployment
- Should follow Azure best practices and Well-Architected Framework
- Infrastructure defined in Terraform (existing pattern)

### Success Criteria
- [ ] API service running with auto-scaling (2-10 replicas)
- [ ] Worker service processing Service Bus messages
- [ ] Database migrations run before service deployment
- [ ] Secrets stored in Azure Key Vault (not plaintext)
- [ ] Network isolation with VNet integration
- [ ] Health checks and liveness probes configured
- [ ] Datadog APM, logs, and metrics flowing
- [ ] Infrastructure cost-optimized for production workloads

## Context

### Current State

The existing Azure Terraform defines a **minimal, development-grade** deployment:

| Component | Current State | Production Gap |
|-----------|---------------|----------------|
| Container App (API) | Single container, 1 replica | No scaling, no health checks |
| Container App (Worker) | **Not deployed** | Missing entirely |
| Database | Connection string passed as variable | No managed database, secrets in plaintext |
| Networking | Default Container App Environment | No VNet, no private endpoints |
| Secrets | Plaintext in environment variables | No Key Vault integration |
| Monitoring | Datadog sidecar (basic) | No Log Analytics, incomplete APM |
| Migrations | **Not deployed** | No migration strategy |
| Container Registry | Public ECR | No ACR, security concern |

### AWS Reference Architecture

The AWS CDK deployment includes:
- **ECS Fargate** with auto-scaling and health checks
- **Aurora PostgreSQL Serverless v2** with credentials in Secrets Manager
- **VPC** with private subnets, NAT Gateway, security groups
- **CloudFront** distribution for API access
- **Lambda workers** for background processing (SQS-triggered)
- **Migration task** that runs before services start
- **EventBridge** integration for event-driven patterns
- **Datadog** full APM with profiling enabled

### Related Decisions
- Application uses ports and adapters architecture with `DRIVEN=AZURE` for Azure-specific implementations
- Service Bus is the messaging backbone (already deployed)
- Datadog is the observability platform (non-negotiable)

## Alternatives Considered

---

### Option A: Minimal Production (Incremental Enhancement)

**Summary**: Add essential production features to existing Terraform while minimizing complexity.

**Architecture**:
```
+------------------------------------------------------------------+
|                    Azure Resource Group                           |
+------------------------------------------------------------------+
|                                                                   |
|  +------------------------------------------------------------+  |
|  |         Container App Environment (Consumption)             |  |
|  |  +-------------------+    +-----------------------------+   |  |
|  |  |   API Service     |    |      Worker Service         |   |  |
|  |  |  (2-10 replicas)  |    |      (1-3 replicas)         |   |  |
|  |  |  + DD sidecar     |    |      + DD sidecar           |   |  |
|  |  +---------+---------+    +-------------+---------------+   |  |
|  +------------|---------------------------|--------------------+  |
|               |                           |                       |
|  +------------v---------------------------v--------------------+  |
|  |              Azure Database for PostgreSQL                  |  |
|  |                   (Flexible Server)                         |  |
|  +-------------------------------------------------------------+  |
|                                                                   |
|  +-------------------------------------------------------------+  |
|  |                    Service Bus Namespace                     |  |
|  |     [users.stickerClaimed.v1]  [users.userRegistered.v1]    |  |
|  +-------------------------------------------------------------+  |
|                                                                   |
|  +-------------------------------------------------------------+  |
|  |                      Key Vault                               |  |
|  |   [db-password] [dd-api-key] [servicebus-connection]         |  |
|  +-------------------------------------------------------------+  |
|                                                                   |
+-------------------------------------------------------------------+
```

**What's Added**:
1. Azure Database for PostgreSQL Flexible Server (Burstable B1ms)
2. Azure Key Vault for secrets
3. Worker Container App with Service Bus scale rule
4. Container App Job for migrations
5. Health probes and scaling rules
6. Log Analytics workspace (required by Container Apps)

**Pros**:
- Fastest path to production
- Lowest complexity
- Builds on existing Terraform
- Cost-effective (consumption plan + burstable DB)

**Cons**:
- No network isolation (public endpoints)
- Limited security controls
- No private container registry
- Database publicly accessible (firewall rules only)

**Coupling Analysis**:
| Component | Afferent (Ca) | Efferent (Ce) | Instability (I) |
|-----------|---------------|---------------|-----------------|
| API Container App | 1 (ingress) | 3 (DB, SB, KV) | 0.75 |
| Worker Container App | 1 (SB trigger) | 3 (DB, SB, KV) | 0.75 |
| PostgreSQL | 2 (API, Worker) | 0 | 0 |
| Key Vault | 2 (API, Worker) | 0 | 0 |

New dependencies introduced: Key Vault, Log Analytics, PostgreSQL Flexible Server
Coupling impact: **Low** - Standard Azure PaaS coupling

**Failure Modes**:
| Mode | Severity | Occurrence | Detection | RPN |
|------|----------|------------|-----------|-----|
| Database connection failure | High (8) | Low (2) | High (2) | 32 |
| Key Vault access denied | High (8) | Low (2) | Medium (4) | 64 |
| Service Bus throttling | Medium (5) | Medium (4) | High (2) | 40 |
| Container App cold start | Low (3) | Medium (5) | High (2) | 30 |

**Evolvability Assessment**:
- Adding VNet later: **Hard** - Requires recreation of Container App Environment
- Adding ACR: **Easy** - Just change image source
- Scaling database: **Easy** - Change SKU
- Adding more workers: **Easy** - Add scale rules

**Effort Estimate**: Small

---

### Option B: Secure Production (VNet Integrated)

**Summary**: Full network isolation with private endpoints, VNet integration, and enterprise security controls.

**Architecture**:
```
+----------------------------------------------------------------------+
|                       Azure Resource Group                            |
+----------------------------------------------------------------------+
|  +----------------------------------------------------------------+  |
|  |                    Virtual Network (10.0.0.0/16)                |  |
|  |  +----------------------------------------------------------+  |  |
|  |  |           Container App Environment Subnet                |  |  |
|  |  |                   (10.0.0.0/23)                           |  |  |
|  |  |  +-------------------+    +---------------------------+   |  |  |
|  |  |  |   API Service     |    |      Worker Service       |   |  |  |
|  |  |  |  (2-10 replicas)  |    |      (1-3 replicas)       |   |  |  |
|  |  |  +-------------------+    +---------------------------+   |  |  |
|  |  +----------------------------------------------------------+  |  |
|  |                                                                 |  |
|  |  +----------------------------------------------------------+  |  |
|  |  |              Private Endpoint Subnet (10.0.2.0/24)        |  |  |
|  |  |  +------------+ +------------+ +--------------------+     |  |  |
|  |  |  | PostgreSQL | |  Key Vault | |  Service Bus       |     |  |  |
|  |  |  |  Endpoint  | |  Endpoint  | |  Endpoint          |     |  |  |
|  |  |  +------------+ +------------+ +--------------------+     |  |  |
|  |  +----------------------------------------------------------+  |  |
|  +----------------------------------------------------------------+  |
|                                                                       |
|  +----------------------------------------------------------------+  |
|  |     Azure Container Registry (Premium - Private Endpoint)      |  |
|  +----------------------------------------------------------------+  |
|                                                                       |
|  +----------------------------------------------------------------+  |
|  |            Azure Front Door (Global Load Balancer)             |  |
|  +----------------------------------------------------------------+  |
|                                                                       |
+-----------------------------------------------------------------------+
```

**What's Added** (on top of Option A):
1. Virtual Network with dedicated subnets
2. Container App Environment with VNet integration
3. Private endpoints for PostgreSQL, Key Vault, Service Bus
4. Azure Container Registry (Premium) with private endpoint
5. Azure Front Door for global routing and WAF
6. Private DNS zones for name resolution
7. Network Security Groups

**Pros**:
- Enterprise-grade security
- No public endpoints for backend services
- Defense in depth
- Compliance-ready (SOC2, HIPAA eligible)
- Private container registry

**Cons**:
- Higher complexity
- Higher cost (Premium SKUs required for private endpoints)
- Longer deployment time
- More difficult to debug

**Coupling Analysis**:
| Component | Afferent (Ca) | Efferent (Ce) | Instability (I) |
|-----------|---------------|---------------|-----------------|
| API Container App | 2 (FD, internal) | 4 (DB, SB, KV, ACR) | 0.67 |
| Worker Container App | 1 (SB trigger) | 4 (DB, SB, KV, ACR) | 0.80 |
| VNet | 5 (all services) | 0 | 0 |
| Front Door | 1 (internet) | 1 (Container App) | 0.50 |

New dependencies introduced: VNet, NSGs, Private DNS, Front Door, ACR
Coupling impact: **Medium** - More components but better isolation

**Failure Modes**:
| Mode | Severity | Occurrence | Detection | RPN |
|------|----------|------------|-----------|-----|
| Private DNS resolution failure | Critical (9) | Low (2) | Medium (4) | 72 |
| VNet misconfiguration | Critical (9) | Low (2) | Low (6) | 108 |
| Front Door origin failure | High (7) | Low (2) | High (2) | 28 |
| ACR pull failure | High (8) | Low (2) | High (2) | 32 |

**Evolvability Assessment**:
- Adding new services: **Easy** - VNet already in place
- Multi-region: **Medium** - Front Door supports it, DB needs geo-replica
- Adding AKS later: **Easy** - VNet ready for additional subnets
- Changing database tier: **Easy** - No networking changes

**Effort Estimate**: Medium

---

### Option C: AWS-Parity Production (Full Feature Set)

**Summary**: Match AWS deployment capabilities exactly, including managed containers, serverless workers, CDN, and full observability.

**Architecture**:
```
+--------------------------------------------------------------------------+
|                          Azure Resource Group                             |
+--------------------------------------------------------------------------+
|                                                                           |
|  +---------------------------------------------------------------------+  |
|  |                     Azure Front Door (Premium)                       |  |
|  |              WAF Policy + Custom Domain + SSL/TLS                    |  |
|  +--------------------------------+------------------------------------+  |
|                                   |                                       |
|  +--------------------------------v------------------------------------+  |
|  |                    Virtual Network (10.0.0.0/16)                     |  |
|  |  +---------------------------------------------------------------+  |  |
|  |  |              Container App Environment (Workload)              |  |  |
|  |  |                      (10.0.0.0/23)                             |  |  |
|  |  |  +----------------+  +----------------+  +-----------------+   |  |  |
|  |  |  |  API Service   |  | Outbox Worker  |  |   Migration     |   |  |  |
|  |  |  | (2-10 replicas)|  | (KEDA scaled)  |  |     Job         |   |  |  |
|  |  |  |  + DD sidecar  |  | + DD sidecar   |  |   (one-time)    |   |  |  |
|  |  |  +----------------+  +----------------+  +-----------------+   |  |  |
|  |  +---------------------------------------------------------------+  |  |
|  |                                                                      |  |
|  |  +---------------------------------------------------------------+  |  |
|  |  |              Azure Functions (Flex Consumption)                |  |  |
|  |  |  +-------------------------+  +----------------------------+   |  |  |
|  |  |  | StickerClaimedFunction  |  |  OutboxTimerFunction       |   |  |  |
|  |  |  | (Service Bus Trigger)   |  |  (Timer Trigger - backup)  |   |  |  |
|  |  |  +-------------------------+  +----------------------------+   |  |  |
|  |  +---------------------------------------------------------------+  |  |
|  |                                                                      |  |
|  |  +---------------------------------------------------------------+  |  |
|  |  |              Private Endpoint Subnet (10.0.2.0/24)             |  |  |
|  |  +---------------------------------------------------------------+  |  |
|  +----------------------------------------------------------------------+  |
|                                                                           |
|  +---------------------------------------------------------------------+  |
|  |                        Data and Messaging                            |  |
|  |  +------------------+  +-----------------+  +--------------------+   |  |
|  |  |  PostgreSQL      |  |  Service Bus    |  |  Key Vault         |   |  |
|  |  |  Flexible Server |  |  Premium        |  |  (Secrets)         |   |  |
|  |  |  (General Purpose|  |  (Private EP)   |  |  (Private EP)      |   |  |
|  |  |   + Read Replica)|  |                 |  |                    |   |  |
|  |  +------------------+  +-----------------+  +--------------------+   |  |
|  +---------------------------------------------------------------------+  |
|                                                                           |
|  +---------------------------------------------------------------------+  |
|  |                         Observability                                |  |
|  |  +------------------+  +-----------------+  +--------------------+   |  |
|  |  |  Log Analytics   |  |  App Insights   |  |  Datadog           |   |  |
|  |  |  Workspace       |  |  (Backup)       |  |  (Primary)         |   |  |
|  |  +------------------+  +-----------------+  +--------------------+   |  |
|  +---------------------------------------------------------------------+  |
|                                                                           |
|  +---------------------------------------------------------------------+  |
|  |                    Azure Container Registry (Premium)                |  |
|  +---------------------------------------------------------------------+  |
|                                                                           |
+---------------------------------------------------------------------------+
```

**What's Added** (on top of Option B):
1. Azure Functions (Flex Consumption) for serverless workers
2. KEDA scaling rules matching AWS Lambda behavior
3. Azure Front Door Premium with WAF
4. PostgreSQL read replica for scaling
5. Application Insights as backup observability
6. Dedicated workload profiles for Container Apps
7. Container App Jobs for migrations
8. Service Bus Premium (for private endpoints)

**Pros**:
- Full feature parity with AWS
- Enterprise-grade everything
- Best performance characteristics
- Multi-region ready
- Serverless cost optimization

**Cons**:
- Highest complexity
- Highest cost
- Over-engineered for most use cases
- Longer time to implement
- More components to maintain

**Coupling Analysis**:
| Component | Afferent (Ca) | Efferent (Ce) | Instability (I) |
|-----------|---------------|---------------|-----------------|
| API Container App | 2 | 5 | 0.71 |
| Azure Functions | 2 | 4 | 0.67 |
| Worker Container App | 1 | 4 | 0.80 |
| Front Door | 1 | 1 | 0.50 |
| PostgreSQL | 4 | 0 | 0 |

New dependencies: Functions, KEDA, WAF, App Insights, Read Replica
Coupling impact: **High** - Many interdependent components

**Failure Modes**:
| Mode | Severity | Occurrence | Detection | RPN |
|------|----------|------------|-----------|-----|
| Function cold start | Low (3) | Medium (5) | High (2) | 30 |
| KEDA scaling lag | Medium (5) | Medium (4) | Medium (4) | 80 |
| Read replica lag | Medium (5) | Low (3) | High (2) | 30 |
| WAF false positive | Medium (5) | Medium (4) | Medium (4) | 80 |

**Evolvability Assessment**:
- Adding new event handlers: **Easy** - Add new Azure Function
- Multi-region: **Easy** - All components support it
- Changing compute model: **Hard** - Significant refactoring
- Cost optimization: **Medium** - Many knobs to tune

**Effort Estimate**: Large

---

## Comparison Matrix

| Criterion | Option A (Minimal) | Option B (Secure) | Option C (Full) |
|-----------|-------------------|-------------------|-----------------|
| Complexity | Low | Medium | High |
| Security | Basic | Enterprise | Enterprise |
| Cost (monthly) | ~$150-300 | ~$400-800 | ~$800-1500+ |
| Time to Implement | Days | 1-2 weeks | 2-4 weeks |
| Coupling Impact | Low | Medium | High |
| Failure Resilience | Medium | High | High |
| Evolvability | Medium | High | High |
| AWS Feature Parity | 60% | 80% | 95% |

## Recommendation

**Recommended Option**: **Option B (Secure Production)**

### Rationale

Option B provides the best balance of security, cost, and complexity for a production deployment:

1. **Security First**: VNet integration and private endpoints are table stakes for production workloads handling user data. Option A's public endpoints are a non-starter for compliance-conscious organizations.

2. **Right-Sized Complexity**: Option B adds necessary security controls without the over-engineering of Option C. The Azure Functions layer in Option C is redundant when Container Apps with KEDA can handle the same workloads.

3. **Cost Efficiency**: While more expensive than Option A, Option B's ~$400-800/month is reasonable for production. Option C's Premium SKUs (Service Bus Premium alone is ~$600/month) are overkill.

4. **Evolvability**: Starting with VNet integration means future enhancements (AKS migration, additional services, multi-region) won't require infrastructure recreation.

5. **Team Capability**: Option B is achievable with standard Azure/Terraform knowledge. Option C requires deep expertise in KEDA, Azure Functions, and complex networking.

### Tradeoffs Accepted

- **No serverless workers**: Container App workers with KEDA provide equivalent functionality. Lambda-style serverless isn't necessary.
- **No read replica**: For the expected load, a single PostgreSQL instance is sufficient. Replicas can be added later.
- **No Application Insights**: Datadog is the primary observability platform. Native Azure monitoring is redundant.

### Risks to Monitor

1. **Container App Environment Recreation**: If VNet configuration is wrong, the entire environment must be recreated. Mitigation: Thorough testing in dev environment first.

2. **Private DNS Resolution**: Misconfigurations can cause complete outages. Mitigation: Use Azure-provided private DNS zones, validate with nslookup tests.

3. **Key Vault Access**: RBAC misconfigurations can prevent service startup. Mitigation: Use managed identities with explicit role assignments, test access before deployment.

## Implementation Plan

### Phase 1: Foundation Infrastructure
- [ ] Create Virtual Network with subnets
- [ ] Deploy Azure Key Vault with private endpoint
- [ ] Deploy Azure Container Registry with private endpoint
- [ ] Create Log Analytics Workspace
- [ ] Set up Private DNS Zones

### Phase 2: Data Layer
- [ ] Deploy PostgreSQL Flexible Server with private endpoint
- [ ] Configure firewall rules (VNet only)
- [ ] Store credentials in Key Vault
- [ ] Update Service Bus with private endpoint (Premium SKU)

### Phase 3: Container App Environment
- [ ] Create Container App Environment with VNet integration
- [ ] Configure workload profile (Consumption)
- [ ] Set up managed identity with Key Vault access
- [ ] Create Container App Job for migrations

### Phase 4: Application Services
- [ ] Deploy API Container App with:
  - Health probes (liveness, readiness, startup)
  - Auto-scaling rules (HTTP concurrent requests)
  - Key Vault references for secrets
  - Datadog sidecar
- [ ] Deploy Worker Container App with:
  - Service Bus KEDA scale rule
  - Key Vault references
  - Datadog sidecar

### Phase 5: External Access
- [ ] Deploy Azure Front Door
- [ ] Configure origin to Container App
- [ ] Set up custom domain and SSL
- [ ] (Optional) Configure WAF rules

### Phase 6: Validation
- [ ] Run integration tests
- [ ] Verify Datadog traces and metrics
- [ ] Load test scaling behavior
- [ ] Security scan with Azure Security Center

## Open Questions

- [x] Should we use Azure Front Door Standard or Premium? **Decision: Standard (no WAF needed)**
- [x] What is the expected request volume for sizing the PostgreSQL tier? **Decision: Burstable B1ms (cheapest, scale later)**
- [x] Is there a custom domain requirement for the API endpoint? **Decision: No, use Azure-provided domain**
- [x] Should the migration job run on every deployment or only when schema changes? **Decision: Run on every deployment**
- [ ] What is the retention policy for Log Analytics data? (Default 30 days is acceptable)
