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

# Secret for the api-testing OAuth client (used by Datadog Synthetics).
# Inject as APITESTING_CLIENT_SECRET into the migration service environment.
resource "random_password" "api_testing_client_secret" {
  length  = 48
  special = false
}

output "api_testing_client_secret" {
  value     = random_password.api_testing_client_secret.result
  sensitive = true
  description = "Secret for the api-testing OAuth client (pass to Datadog Synthetics)"
}