# Shared Resources Module
# This module can be called from:
# 1. The shared infrastructure stack (for integrated environments like dev/prod)
# 2. Service stacks directly (for ephemeral/test environments)

terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.27"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.8"
    }
  }
}

# Local values
locals {
  resource_group_name = coalesce(var.resource_group_name, "rg-stickerlandia-shared-${var.env}")

  default_tags = {
    env     = var.env
    project = "stickerlandia"
    scope   = "shared"
    source  = "terraform"
  }

  tags = merge(local.default_tags, var.tags)
}

# Data sources
data "azurerm_subscription" "current" {}

data "azurerm_client_config" "current" {}

# Resource Group
resource "azurerm_resource_group" "shared" {
  name     = local.resource_group_name
  location = var.location
  tags     = local.tags
}
