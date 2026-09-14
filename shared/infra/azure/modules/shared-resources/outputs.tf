# Resource Group
output "resource_group_name" {
  description = "The name of the shared resource group"
  value       = azurerm_resource_group.shared.name
}

output "resource_group_id" {
  description = "The ID of the shared resource group"
  value       = azurerm_resource_group.shared.id
}

output "location" {
  description = "The Azure region"
  value       = azurerm_resource_group.shared.location
}

# Networking
output "vnet_id" {
  description = "The ID of the shared Virtual Network"
  value       = azurerm_virtual_network.main.id
}

output "vnet_name" {
  description = "The name of the shared Virtual Network"
  value       = azurerm_virtual_network.main.name
}

output "container_apps_subnet_id" {
  description = "The ID of the Container Apps subnet"
  value       = azurerm_subnet.container_apps.id
}

output "private_endpoints_subnet_id" {
  description = "The ID of the Private Endpoints subnet"
  value       = azurerm_subnet.private_endpoints.id
}

output "postgresql_subnet_id" {
  description = "The ID of the PostgreSQL subnet"
  value       = azurerm_subnet.postgresql.id
}

# Monitoring
output "log_analytics_workspace_id" {
  description = "The ID of the Log Analytics Workspace"
  value       = azurerm_log_analytics_workspace.main.id
}

output "log_analytics_workspace_name" {
  description = "The name of the Log Analytics Workspace"
  value       = azurerm_log_analytics_workspace.main.name
}

# Key Vault
output "key_vault_id" {
  description = "The ID of the shared Key Vault"
  value       = azurerm_key_vault.shared.id
}

output "key_vault_name" {
  description = "The name of the shared Key Vault"
  value       = azurerm_key_vault.shared.name
}

output "key_vault_uri" {
  description = "The URI of the shared Key Vault"
  value       = azurerm_key_vault.shared.vault_uri
}

output "key_vault_suffix" {
  description = "The random suffix used for the Key Vault name"
  value       = random_string.kv_suffix.result
}

output "dd_api_key_secret_id" {
  description = "The Key Vault secret ID for the Datadog API key"
  value       = azurerm_key_vault_secret.dd_api_key.id
}

output "dd_api_key_secret_name" {
  description = "The Key Vault secret name for the Datadog API key"
  value       = azurerm_key_vault_secret.dd_api_key.name
}

output "postgresql_admin_password_secret_id" {
  description = "The Key Vault secret ID for the PostgreSQL admin password"
  value       = azurerm_key_vault_secret.postgresql_admin_password.id
}

output "postgresql_admin_password_secret_name" {
  description = "The Key Vault secret name for the PostgreSQL admin password"
  value       = azurerm_key_vault_secret.postgresql_admin_password.name
}

output "postgresql_admin_password" {
  description = "The PostgreSQL admin password (sensitive)"
  value       = random_password.postgresql_admin_password.result
  sensitive   = true
}

# Database
output "postgresql_server_id" {
  description = "The ID of the PostgreSQL Flexible Server"
  value       = azurerm_postgresql_flexible_server.shared.id
}

output "postgresql_server_fqdn" {
  description = "The FQDN of the PostgreSQL Flexible Server"
  value       = azurerm_postgresql_flexible_server.shared.fqdn
}

output "postgresql_server_name" {
  description = "The name of the PostgreSQL Flexible Server"
  value       = azurerm_postgresql_flexible_server.shared.name
}

output "postgresql_admin_username" {
  description = "The administrator username for PostgreSQL"
  value       = azurerm_postgresql_flexible_server.shared.administrator_login
}

# Messaging
output "servicebus_namespace_id" {
  description = "The ID of the Service Bus Namespace"
  value       = azurerm_servicebus_namespace.shared.id
}

output "servicebus_namespace_name" {
  description = "The name of the Service Bus Namespace"
  value       = azurerm_servicebus_namespace.shared.name
}

output "servicebus_connection_string" {
  description = "The primary connection string for the Service Bus Namespace"
  value       = azurerm_servicebus_namespace.shared.default_primary_connection_string
  sensitive   = true
}

# Container Apps
output "container_app_environment_id" {
  description = "The ID of the shared Container App Environment"
  value       = azurerm_container_app_environment.shared.id
}

output "container_app_environment_name" {
  description = "The name of the shared Container App Environment"
  value       = azurerm_container_app_environment.shared.name
}

output "container_app_environment_default_domain" {
  description = "The default domain of the Container App Environment"
  value       = azurerm_container_app_environment.shared.default_domain
}

# Front Door
output "frontdoor_profile_id" {
  description = "The ID of the Front Door Profile"
  value       = azurerm_cdn_frontdoor_profile.shared.id
}

output "frontdoor_profile_name" {
  description = "The name of the Front Door Profile"
  value       = azurerm_cdn_frontdoor_profile.shared.name
}

output "frontdoor_endpoint_id" {
  description = "The ID of the Front Door Endpoint"
  value       = azurerm_cdn_frontdoor_endpoint.shared.id
}

output "frontdoor_endpoint_host_name" {
  description = "The hostname of the Front Door Endpoint"
  value       = azurerm_cdn_frontdoor_endpoint.shared.host_name
}
