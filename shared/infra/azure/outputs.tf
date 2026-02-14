# Re-export all outputs from the shared module

# Resource Group
output "resource_group_name" {
  description = "The name of the shared resource group"
  value       = module.shared.resource_group_name
}

output "resource_group_id" {
  description = "The ID of the shared resource group"
  value       = module.shared.resource_group_id
}

output "location" {
  description = "The Azure region"
  value       = module.shared.location
}

# Networking
output "vnet_id" {
  description = "The ID of the shared Virtual Network"
  value       = module.shared.vnet_id
}

output "vnet_name" {
  description = "The name of the shared Virtual Network"
  value       = module.shared.vnet_name
}

output "container_apps_subnet_id" {
  description = "The ID of the Container Apps subnet"
  value       = module.shared.container_apps_subnet_id
}

output "private_endpoints_subnet_id" {
  description = "The ID of the Private Endpoints subnet"
  value       = module.shared.private_endpoints_subnet_id
}

output "postgresql_subnet_id" {
  description = "The ID of the PostgreSQL subnet"
  value       = module.shared.postgresql_subnet_id
}

# Monitoring
output "log_analytics_workspace_id" {
  description = "The ID of the Log Analytics Workspace"
  value       = module.shared.log_analytics_workspace_id
}

output "log_analytics_workspace_name" {
  description = "The name of the Log Analytics Workspace"
  value       = module.shared.log_analytics_workspace_name
}

# Key Vault
output "key_vault_id" {
  description = "The ID of the shared Key Vault"
  value       = module.shared.key_vault_id
}

output "key_vault_name" {
  description = "The name of the shared Key Vault"
  value       = module.shared.key_vault_name
}

output "key_vault_uri" {
  description = "The URI of the shared Key Vault"
  value       = module.shared.key_vault_uri
}

output "key_vault_suffix" {
  description = "The random suffix used for the Key Vault name"
  value       = module.shared.key_vault_suffix
}

output "dd_api_key_secret_id" {
  description = "The Key Vault secret ID for the Datadog API key"
  value       = module.shared.dd_api_key_secret_id
}

output "postgresql_admin_password_secret_id" {
  description = "The Key Vault secret ID for the PostgreSQL admin password"
  value       = module.shared.postgresql_admin_password_secret_id
}

# Database
output "postgresql_server_id" {
  description = "The ID of the PostgreSQL Flexible Server"
  value       = module.shared.postgresql_server_id
}

output "postgresql_server_fqdn" {
  description = "The FQDN of the PostgreSQL Flexible Server"
  value       = module.shared.postgresql_server_fqdn
}

output "postgresql_server_name" {
  description = "The name of the PostgreSQL Flexible Server"
  value       = module.shared.postgresql_server_name
}

output "postgresql_admin_username" {
  description = "The administrator username for PostgreSQL"
  value       = module.shared.postgresql_admin_username
}

# Messaging
output "servicebus_namespace_id" {
  description = "The ID of the Service Bus Namespace"
  value       = module.shared.servicebus_namespace_id
}

output "servicebus_namespace_name" {
  description = "The name of the Service Bus Namespace"
  value       = module.shared.servicebus_namespace_name
}

output "servicebus_connection_string" {
  description = "The primary connection string for the Service Bus Namespace"
  value       = module.shared.servicebus_connection_string
  sensitive   = true
}

# Container Apps
output "container_app_environment_id" {
  description = "The ID of the shared Container App Environment"
  value       = module.shared.container_app_environment_id
}

output "container_app_environment_name" {
  description = "The name of the shared Container App Environment"
  value       = module.shared.container_app_environment_name
}

output "container_app_environment_default_domain" {
  description = "The default domain of the Container App Environment"
  value       = module.shared.container_app_environment_default_domain
}

# Front Door
output "frontdoor_profile_id" {
  description = "The ID of the Front Door Profile"
  value       = module.shared.frontdoor_profile_id
}

output "frontdoor_profile_name" {
  description = "The name of the Front Door Profile"
  value       = module.shared.frontdoor_profile_name
}

output "frontdoor_endpoint_id" {
  description = "The ID of the Front Door Endpoint"
  value       = module.shared.frontdoor_endpoint_id
}

output "frontdoor_endpoint_host_name" {
  description = "The hostname of the Front Door Endpoint"
  value       = module.shared.frontdoor_endpoint_host_name
}
