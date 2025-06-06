terraform {
    required_providers {
      azurerm = {
        version = "4.27.0"
        source = "hashicorp/azurerm"
      }
    }
}

provider "azurerm" {
  features {}
  subscription_id = var.subscription_id
  resource_provider_registrations = "none"
}