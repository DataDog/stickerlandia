variable "subscription_id" {
  description = "The Azure Subscription ID"
  type        = string
}

variable "location" {
  description = "The Azure Region for all resources"
  type        = string
  default     = "uksouth"
}

variable "env" {
  description = "The environment (dev, staging, prod)"
  type        = string
}

variable "dd_api_key" {
  description = "The Datadog API key"
  type        = string
  sensitive   = true
}

variable "dd_site" {
  description = "The Datadog site"
  type        = string
  default     = "datadoghq.com"
}

# Networking
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

# Database
variable "postgresql_admin_username" {
  description = "The administrator username for PostgreSQL"
  type        = string
  default     = "pgadmin"
}
