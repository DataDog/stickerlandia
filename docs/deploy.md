# Deployment Targets

This page describes the deployment targets Stickerlandia supports or aims to support. It aims to pull forward any and all details that may impact early-stages implementation.

## Platform Overview

Stickerlandia will support both serverless and container-based platforms, self-hosted, as well as in the cloud.

### Serverless

| Platform | Description | IaC | Messaging | Blob Storage | Database | Ingress | Ingress Features |
|----------|-------------|-----|-----------|--------------|----------|---------|------------------|
| AWS (Serverless) | Containers on Amazon ECS/Fargate & Lambda functions with managed services | CDK | Amazon SQS/SNS/EventBridge | Amazon S3 | Amazon RDS/DynamoDB | API Gateway | Rate limiting, CORS, JWT/OIDC validation, API keys |
| Azure (Serverless) | Azure Container Apps & Azure Functions with managed services | Terraform | Azure Service Bus | Azure Blob Storage | Azure SQL | Azure API Management | Path routing, rate limiting, JWT/OIDC validation |

### Containerised

| Platform | Description | IaC | Messaging | Blob Storage | Database | Ingress | Ingress Features |
|----------|-------------|-----|-----------|--------------|----------|---------|------------------|
| AWS (EKS) | Kubernetes on Elastic Kubernetes Service | K8S Manifests, CDK | Apache Kafka on MSK | Amazon S3 | Amazon RDS | ALB + Ingress Controller | Path routing, SSL termination (No native OIDC) |
| Docker-Compose | Local development environment | docker-compose.yml | Kafka | MinIO | PostgreSQL | Traefik | Path routing, SSL (No native OIDC) |
| Kubernetes | Generic Kubernetes deployment | K8S Manifests | Kafka | MinIO | PostgreSQL | Ingress Controller | SSL termination, path routing (No native OIDC) |


