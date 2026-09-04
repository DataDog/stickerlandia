# Required variables
variable "env" {
  description = "The environment (dev, staging, prod, or ephemeral name)"
  type        = string
}

variable "location" {
  description = "The Azure Region for all resources"
  type        = string
}

variable "dd_api_key" {
  description = "The Datadog API key"
  type        = string
  sensitive   = true
}

# Optional variables with defaults
variable "resource_group_name" {
  description = "Override the resource group name (defaults to rg-stickerlandia-shared-{env})"
  type        = string
  default     = null
}

variable "dd_site" {
  description = "The Datadog site"
  type        = string
  default     = "datadoghq.com"
}

variable "vnet_address_space" {
  description = "The address space for the Virtual Network"
  type        = string
  default     = "10.0.0.0/16"
}

variable "container_apps_subnet_prefix" {
  description = "The address prefix for the Container Apps subnet (minimum /23)"
  type        = string
  default     = "10.0.0.0/23"
}

variable "private_endpoints_subnet_prefix" {
  description = "The address prefix for the Private Endpoints subnet"
  type        = string
  default     = "10.0.2.0/24"
}

variable "postgresql_subnet_prefix" {
  description = "The address prefix for the PostgreSQL subnet"
  type        = string
  default     = "10.0.3.0/24"
}

variable "postgresql_admin_username" {
  description = "The administrator username for PostgreSQL"
  type        = string
  default     = "pgadmin"
}

variable "postgresql_sku_name" {
  description = "The SKU name for PostgreSQL Flexible Server"
  type        = string
  default     = "B_Standard_B1ms"
}

variable "tags" {
  description = "Additional tags to apply to all resources"
  type        = map(string)
  default     = {}
}
