# Service Outputs

output "api_url" {
  description = "The public URL for the User Management API"
  value       = "https://${local.shared.frontdoor_endpoint_host_name}/api/users/v1"
}

output "api_container_app_name" {
  description = "The name of the API Container App"
  value       = var.deploy_container_apps ? azurerm_container_app.api[0].name : null
}

output "api_container_app_fqdn" {
  description = "The FQDN of the API Container App (internal)"
  value       = var.deploy_container_apps ? azurerm_container_app.api[0].ingress[0].fqdn : null
}

output "worker_container_app_name" {
  description = "The name of the Worker Container App"
  value       = var.deploy_container_apps ? azurerm_container_app.worker[0].name : null
}

output "migration_job_name" {
  description = "The name of the migration Container App Job"
  value       = var.deploy_container_apps ? azurerm_container_app_job.migration[0].name : null
}

output "database_name" {
  description = "The name of the PostgreSQL database"
  value       = azurerm_postgresql_flexible_server_database.users.name
}

# Shared infrastructure mode indicator
output "using_shared_infrastructure" {
  description = "Whether this deployment uses existing shared infrastructure or creates its own"
  value       = var.use_shared_infrastructure
}

output "resource_group_name" {
  description = "The name of the resource group containing the service"
  value       = local.shared.resource_group_name
}

# ACR outputs (when enabled)
output "acr_login_server" {
  description = "The login server URL for the Azure Container Registry (when create_acr=true)"
  value       = var.create_acr ? azurerm_container_registry.acr[0].login_server : null
}

output "acr_name" {
  description = "The name of the Azure Container Registry (when create_acr=true)"
  value       = var.create_acr ? azurerm_container_registry.acr[0].name : null
}

output "container_apps_deployed" {
  description = "Whether Container Apps were deployed in this run"
  value       = var.deploy_container_apps
}
