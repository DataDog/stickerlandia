# User Management Service Infrastructure
# Supports two modes:
# 1. use_shared_infrastructure=true  → Looks up existing shared resources (dev/prod)
# 2. use_shared_infrastructure=false → Creates shared resources inline (ephemeral/test)

# ============================================================================
# MODE 1: Use existing shared infrastructure
# ============================================================================

data "azurerm_resource_group" "shared" {
  count = var.use_shared_infrastructure ? 1 : 0
  name  = "rg-stickerlandia-shared-${var.env}"
}

data "azurerm_container_app_environment" "shared" {
  count               = var.use_shared_infrastructure ? 1 : 0
  name                = "cae-stickerlandia-${var.env}"
  resource_group_name = data.azurerm_resource_group.shared[0].name
}

data "azurerm_key_vault" "shared" {
  count               = var.use_shared_infrastructure ? 1 : 0
  name                = "kv-sticker-${var.env}-${var.shared_keyvault_suffix}"
  resource_group_name = data.azurerm_resource_group.shared[0].name
}

data "azurerm_key_vault_secret" "dd_api_key" {
  count        = var.use_shared_infrastructure ? 1 : 0
  name         = "dd-api-key"
  key_vault_id = data.azurerm_key_vault.shared[0].id
}

data "azurerm_key_vault_secret" "postgresql_admin_password" {
  count        = var.use_shared_infrastructure ? 1 : 0
  name         = "postgresql-admin-password"
  key_vault_id = data.azurerm_key_vault.shared[0].id
}

data "azurerm_postgresql_flexible_server" "shared" {
  count               = var.use_shared_infrastructure ? 1 : 0
  name                = "psql-stickerlandia-${var.env}"
  resource_group_name = data.azurerm_resource_group.shared[0].name
}

data "azurerm_servicebus_namespace" "shared" {
  count               = var.use_shared_infrastructure ? 1 : 0
  name                = "sb-stickerlandia-${var.env}"
  resource_group_name = data.azurerm_resource_group.shared[0].name
}

data "azurerm_cdn_frontdoor_profile" "shared" {
  count               = var.use_shared_infrastructure ? 1 : 0
  name                = "afd-stickerlandia-${var.env}"
  resource_group_name = data.azurerm_resource_group.shared[0].name
}

data "azurerm_cdn_frontdoor_endpoint" "shared" {
  count               = var.use_shared_infrastructure ? 1 : 0
  name                = "stickerlandia-api-${var.env}"
  profile_name        = data.azurerm_cdn_frontdoor_profile.shared[0].name
  resource_group_name = data.azurerm_resource_group.shared[0].name
}

# ============================================================================
# MODE 2: Create shared resources inline (ephemeral environments)
# ============================================================================

module "inline_shared" {
  count  = var.use_shared_infrastructure ? 0 : 1
  source = "../../../shared/infra/azure/modules/shared-resources"

  env        = var.env
  location   = var.location
  dd_api_key = var.dd_api_key
  dd_site    = var.dd_site

  # Use a unique resource group name for ephemeral environments
  resource_group_name = "rg-users-${var.env}"
}

# ============================================================================
# Unified locals - abstracts the source of shared resources
# ============================================================================

data "azurerm_subscription" "current" {}

data "azurerm_client_config" "current" {}

locals {
  # Unified references to shared resources (works for both modes)
  shared = {
    resource_group_name     = var.use_shared_infrastructure ? data.azurerm_resource_group.shared[0].name : module.inline_shared[0].resource_group_name
    resource_group_location = var.use_shared_infrastructure ? data.azurerm_resource_group.shared[0].location : module.inline_shared[0].location

    container_app_environment_id = var.use_shared_infrastructure ? data.azurerm_container_app_environment.shared[0].id : module.inline_shared[0].container_app_environment_id

    key_vault_id   = var.use_shared_infrastructure ? data.azurerm_key_vault.shared[0].id : module.inline_shared[0].key_vault_id
    key_vault_name = var.use_shared_infrastructure ? data.azurerm_key_vault.shared[0].name : module.inline_shared[0].key_vault_name

    dd_api_key_secret_id = var.use_shared_infrastructure ? data.azurerm_key_vault_secret.dd_api_key[0].id : module.inline_shared[0].dd_api_key_secret_id

    postgresql_admin_password           = var.use_shared_infrastructure ? data.azurerm_key_vault_secret.postgresql_admin_password[0].value : module.inline_shared[0].postgresql_admin_password
    postgresql_admin_password_secret_id = var.use_shared_infrastructure ? data.azurerm_key_vault_secret.postgresql_admin_password[0].id : module.inline_shared[0].postgresql_admin_password_secret_id
    postgresql_server_id                = var.use_shared_infrastructure ? data.azurerm_postgresql_flexible_server.shared[0].id : module.inline_shared[0].postgresql_server_id
    postgresql_server_fqdn              = var.use_shared_infrastructure ? data.azurerm_postgresql_flexible_server.shared[0].fqdn : module.inline_shared[0].postgresql_server_fqdn
    postgresql_admin_username           = var.use_shared_infrastructure ? data.azurerm_postgresql_flexible_server.shared[0].administrator_login : module.inline_shared[0].postgresql_admin_username

    servicebus_namespace_id      = var.use_shared_infrastructure ? data.azurerm_servicebus_namespace.shared[0].id : module.inline_shared[0].servicebus_namespace_id
    servicebus_namespace_name    = var.use_shared_infrastructure ? data.azurerm_servicebus_namespace.shared[0].name : module.inline_shared[0].servicebus_namespace_name
    servicebus_connection_string = var.use_shared_infrastructure ? data.azurerm_servicebus_namespace.shared[0].default_primary_connection_string : module.inline_shared[0].servicebus_connection_string

    frontdoor_profile_id         = var.use_shared_infrastructure ? data.azurerm_cdn_frontdoor_profile.shared[0].id : module.inline_shared[0].frontdoor_profile_id
    frontdoor_endpoint_id        = var.use_shared_infrastructure ? data.azurerm_cdn_frontdoor_endpoint.shared[0].id : module.inline_shared[0].frontdoor_endpoint_id
    frontdoor_endpoint_host_name = var.use_shared_infrastructure ? data.azurerm_cdn_frontdoor_endpoint.shared[0].host_name : module.inline_shared[0].frontdoor_endpoint_host_name
  }

  tags = {
    env     = var.env
    service = "user-management"
    version = var.app_version
    source  = "terraform"
  }

  # Compute the effective image registry (ACR if enabled, otherwise the variable default)
  effective_registry = var.create_acr ? azurerm_container_registry.acr[0].login_server : var.container_image_registry

  # Datadog environment variables for sidecars
  datadog_env = [
    { name = "DD_SITE", value = var.dd_site },
    { name = "DD_ENV", value = var.env },
    { name = "DD_VERSION", value = var.app_version },
    { name = "DD_SERVICE", value = "user-management" },
    { name = "DD_LOGS_ENABLED", value = "true" },
    { name = "DD_LOGS_INJECTION", value = "true" },
    { name = "DD_APM_COMPUTE_STATS_BY_SPAN_KIND", value = "true" },
    { name = "DD_APM_PEER_TAGS_AGGREGATION", value = "true" },
    { name = "DD_TRACE_REMOVE_INTEGRATION_SERVICE_NAMES_ENABLED", value = "true" },
    { name = "DD_APM_IGNORE_RESOURCES", value = "/opentelemetry.proto.collector.trace.v1.TraceService/Export$" },
    { name = "DD_OTLP_CONFIG_RECEIVER_PROTOCOLS_GRPC_ENDPOINT", value = "0.0.0.0:4317" },
    { name = "DD_AZURE_SUBSCRIPTION_ID", value = data.azurerm_subscription.current.subscription_id },
    { name = "DD_AZURE_RESOURCE_GROUP", value = local.shared.resource_group_name },
  ]
}
