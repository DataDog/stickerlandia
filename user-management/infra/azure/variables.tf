variable "subscription_id" {
  description = "The Azure Subscription ID"
  type        = string
}

variable "env" {
  description = "The environment (dev, staging, prod, or ephemeral name)"
  type        = string
}

variable "location" {
  description = "The Azure Region (required when creating inline shared resources)"
  type        = string
  default     = "uksouth"
}

variable "app_version" {
  description = "The version of the application to deploy (container image tag)"
  type        = string
  default     = "latest"
}

variable "container_image_registry" {
  description = "The container image registry base path"
  type        = string
  default     = "ghcr.io/datadog/stickerlandia"
}

variable "dd_site" {
  description = "The Datadog site"
  type        = string
  default     = "datadoghq.com"
}

# Shared infrastructure mode
variable "use_shared_infrastructure" {
  description = "If true, looks up existing shared infrastructure. If false, creates it inline."
  type        = bool
  default     = true
}

variable "shared_keyvault_suffix" {
  description = "The random suffix of the shared Key Vault (required when use_shared_infrastructure=true)"
  type        = string
  default     = ""
}

# Required when creating inline shared resources (use_shared_infrastructure=false)
variable "dd_api_key" {
  description = "The Datadog API key (required when use_shared_infrastructure=false)"
  type        = string
  sensitive   = true
  default     = ""
}

# Local build support
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
