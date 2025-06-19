variable "subscription_id" {
  description = "The Azure Subscription ID in which all resources in this example should be created."
}

variable "resourceGroupName" {
  description = "The Azure Resource Group name in which all resources in this example should be created."
}

variable "database_connection_string" {
  description = "The connection string to the database used by the application."
}

variable "location" {
  description = "The Azure Region in which all resources in this example should be created."
}

variable "env" {
  description = "The environment you are deploying to"
}

variable "app_version" {
  description = "The version of the application to deploy"
  default     = "latest"
}

variable "dd_api_key" {
  description = "The Datadog API key"
}

variable "dd_site" {
  default = "datadoghq.com"
  description = "The Datadog site"
}