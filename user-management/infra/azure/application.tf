# User Assigned Identity for this service's Container Apps
resource "azurerm_user_assigned_identity" "app_identity" {
  location            = local.shared.resource_group_location
  name                = "id-users-${var.env}"
  resource_group_name = local.shared.resource_group_name
  tags                = local.tags
}

# Grant identity access to shared Key Vault secrets
resource "azurerm_role_assignment" "keyvault_secrets_user" {
  scope                = local.shared.key_vault_id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = azurerm_user_assigned_identity.app_identity.principal_id
}

# Migration Job - runs on every deployment
resource "azurerm_container_app_job" "migration" {
  count                        = var.deploy_container_apps ? 1 : 0
  name                         = "job-users-migration-${var.env}"
  location                     = local.shared.resource_group_location
  resource_group_name          = local.shared.resource_group_name
  container_app_environment_id = local.shared.container_app_environment_id
  tags                         = local.tags

  replica_timeout_in_seconds = 300
  replica_retry_limit        = 1

  manual_trigger_config {
    parallelism              = 1
    replica_completion_count = 1
  }

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.app_identity.id]
  }

  # Registry configuration for ACR (managed identity auth)
  dynamic "registry" {
    for_each = var.create_acr ? [1] : []
    content {
      server   = azurerm_container_registry.acr[0].login_server
      identity = azurerm_user_assigned_identity.app_identity.id
    }
  }

  secret {
    name                = "db-connection-string"
    key_vault_secret_id = azurerm_key_vault_secret.db_connection_string.id
    identity            = azurerm_user_assigned_identity.app_identity.id
  }

  secret {
    name                = "dd-api-key"
    key_vault_secret_id = local.shared.dd_api_key_secret_id
    identity            = azurerm_user_assigned_identity.app_identity.id
  }

  template {
    container {
      name   = "migration"
      image  = "${local.effective_registry}/user-management-migration:${var.app_version}"
      cpu    = 0.5
      memory = "1Gi"

      env {
        name        = "ConnectionStrings__database"
        secret_name = "db-connection-string"
      }
      env {
        name  = "DRIVING"
        value = "ASPNET"
      }
      env {
        name  = "DRIVEN"
        value = "AZURE"
      }
      env {
        name  = "DISABLE_SSL"
        value = "true"
      }
      env {
        name        = "DD_API_KEY"
        secret_name = "dd-api-key"
      }
    }
  }

  depends_on = [azurerm_role_assignment.keyvault_secrets_user, azurerm_role_assignment.acr_pull]
}

# Automatically start the migration job after creation
resource "azapi_resource_action" "start_migration_job" {
  count       = var.deploy_container_apps ? 1 : 0
  type        = "Microsoft.App/jobs@2024-03-01"
  resource_id = azurerm_container_app_job.migration[0].id
  action      = "start"
  method      = "POST"

  depends_on = [
    azurerm_container_app_job.migration,
    azurerm_role_assignment.acr_pull
  ]
}

# API Container App
resource "azurerm_container_app" "api" {
  count                        = var.deploy_container_apps ? 1 : 0
  name                         = "ca-users-api-${var.env}"
  container_app_environment_id = local.shared.container_app_environment_id
  resource_group_name          = local.shared.resource_group_name
  revision_mode                = "Single"
  tags                         = local.tags

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.app_identity.id]
  }

  # Registry configuration for ACR (managed identity auth)
  dynamic "registry" {
    for_each = var.create_acr ? [1] : []
    content {
      server   = azurerm_container_registry.acr[0].login_server
      identity = azurerm_user_assigned_identity.app_identity.id
    }
  }

  secret {
    name                = "db-connection-string"
    key_vault_secret_id = azurerm_key_vault_secret.db_connection_string.id
    identity            = azurerm_user_assigned_identity.app_identity.id
  }

  secret {
    name                = "servicebus-connection-string"
    key_vault_secret_id = azurerm_key_vault_secret.servicebus_connection_string.id
    identity            = azurerm_user_assigned_identity.app_identity.id
  }

  secret {
    name                = "dd-api-key"
    key_vault_secret_id = local.shared.dd_api_key_secret_id
    identity            = azurerm_user_assigned_identity.app_identity.id
  }

  ingress {
    external_enabled = true
    target_port      = 8080
    transport        = "http"

    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }

  template {
    min_replicas = 2
    max_replicas = 10

    http_scale_rule {
      name                = "http-scaling"
      concurrent_requests = 100
    }

    container {
      name   = "api"
      image  = "${local.effective_registry}/user-management-service:${var.app_version}"
      cpu    = 0.5
      memory = "1Gi"

      env {
        name        = "ConnectionStrings__database"
        secret_name = "db-connection-string"
      }
      env {
        name        = "ConnectionStrings__messaging"
        secret_name = "servicebus-connection-string"
      }
      env {
        name  = "DRIVING"
        value = "ASPNET"
      }
      env {
        name  = "DRIVEN"
        value = "AZURE"
      }
      env {
        name  = "DISABLE_SSL"
        value = "true"
      }
      env {
        name  = "DEPLOYMENT_HOST_URL"
        value = "https://${local.shared.frontdoor_endpoint_host_name}"
      }

      liveness_probe {
        transport               = "HTTP"
        path                    = "/api/users/v1/health"
        port                    = 8080
        initial_delay           = 30
        interval_seconds        = 30
        timeout                 = 5
        failure_count_threshold = 3
      }

      readiness_probe {
        transport               = "HTTP"
        path                    = "/api/users/v1/health"
        port                    = 8080
        interval_seconds        = 10
        timeout                 = 5
        failure_count_threshold = 3
      }

      startup_probe {
        transport               = "HTTP"
        path                    = "/api/users/v1/health"
        port                    = 8080
        interval_seconds        = 10
        timeout                 = 5
        failure_count_threshold = 30
      }
    }

    container {
      name   = "datadog"
      image  = "index.docker.io/datadog/serverless-init:latest"
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name        = "DD_API_KEY"
        secret_name = "dd-api-key"
      }

      dynamic "env" {
        for_each = local.datadog_env
        content {
          name  = env.value.name
          value = env.value.value
        }
      }
    }
  }

  depends_on = [
    azapi_resource_action.start_migration_job,
    azurerm_role_assignment.keyvault_secrets_user,
    azurerm_role_assignment.acr_pull
  ]
}

# Worker Container App
resource "azurerm_container_app" "worker" {
  count                        = var.deploy_container_apps ? 1 : 0
  name                         = "ca-users-worker-${var.env}"
  container_app_environment_id = local.shared.container_app_environment_id
  resource_group_name          = local.shared.resource_group_name
  revision_mode                = "Single"
  tags                         = local.tags

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.app_identity.id]
  }

  # Registry configuration for ACR (managed identity auth)
  dynamic "registry" {
    for_each = var.create_acr ? [1] : []
    content {
      server   = azurerm_container_registry.acr[0].login_server
      identity = azurerm_user_assigned_identity.app_identity.id
    }
  }

  secret {
    name                = "db-connection-string"
    key_vault_secret_id = azurerm_key_vault_secret.db_connection_string.id
    identity            = azurerm_user_assigned_identity.app_identity.id
  }

  secret {
    name                = "servicebus-connection-string"
    key_vault_secret_id = azurerm_key_vault_secret.servicebus_connection_string.id
    identity            = azurerm_user_assigned_identity.app_identity.id
  }

  secret {
    name                = "dd-api-key"
    key_vault_secret_id = local.shared.dd_api_key_secret_id
    identity            = azurerm_user_assigned_identity.app_identity.id
  }

  template {
    min_replicas = 1
    max_replicas = 3

    custom_scale_rule {
      name             = "servicebus-scaling"
      custom_rule_type = "azure-servicebus"
      metadata = {
        queueName    = azurerm_servicebus_queue.sticker_claimed.name
        namespace    = local.shared.servicebus_namespace_name
        messageCount = "10"
      }
      authentication {
        secret_name       = "servicebus-connection-string"
        trigger_parameter = "connection"
      }
    }

    container {
      name   = "worker"
      image  = "${local.effective_registry}/user-management-worker:${var.app_version}"
      cpu    = 0.5
      memory = "1Gi"

      env {
        name        = "ConnectionStrings__database"
        secret_name = "db-connection-string"
      }
      env {
        name        = "ConnectionStrings__messaging"
        secret_name = "servicebus-connection-string"
      }
      env {
        name  = "DRIVING"
        value = "ASPNET"
      }
      env {
        name  = "DRIVEN"
        value = "AZURE"
      }
      env {
        name  = "DISABLE_SSL"
        value = "true"
      }

      liveness_probe {
        transport               = "HTTP"
        path                    = "/health"
        port                    = 8080
        initial_delay           = 30
        interval_seconds        = 30
        timeout                 = 5
        failure_count_threshold = 3
      }
    }

    container {
      name   = "datadog"
      image  = "index.docker.io/datadog/serverless-init:latest"
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name        = "DD_API_KEY"
        secret_name = "dd-api-key"
      }

      dynamic "env" {
        for_each = local.datadog_env
        content {
          name  = env.value.name
          value = env.value.value
        }
      }
    }
  }

  depends_on = [
    azapi_resource_action.start_migration_job,
    azurerm_role_assignment.keyvault_secrets_user,
    azurerm_role_assignment.acr_pull
  ]
}
