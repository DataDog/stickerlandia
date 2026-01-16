resource "azurerm_container_app_environment" "user_service_environment" {
  name                = var.env
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name

  tags = {
    source = "terraform"
    env    = var.env
  }
}

resource "azurerm_user_assigned_identity" "app_identity" {
  location            = azurerm_resource_group.rg.location
  name                = "userServiceAppIdentity-${var.env}"
  resource_group_name = azurerm_resource_group.rg.name
}

resource "azurerm_container_app" "user-management-api" {
  name                         = "user-management"
  container_app_environment_id = azurerm_container_app_environment.user_service_environment.id
  resource_group_name          = azurerm_resource_group.rg.name
  revision_mode                = "Single"
  identity {
    identity_ids = [azurerm_user_assigned_identity.app_identity.id]
    type         = "UserAssigned"
  }
  ingress {
    external_enabled = true
    target_port      = 8080
    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }
  template {
    min_replicas = 1
    max_replicas = 1
    container {
      name   = "application"
      image  = "public.ecr.aws/h1j5s6w8/stickerlandia-user-management:${var.app_version}"
      cpu    = 0.25
      memory = "0.5Gi"
      env {
        name  = "ConnectionStrings__database"
        value = var.database_connection_string
      }
      env {
        name  = "ConnectionStrings__messaging"
        value = azurerm_servicebus_namespace.stickerlandia_users_service_bus.default_primary_connection_string
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
        name = "DISABLE_SSL"
        value = "true"
      }
    }
    container {
      name   = "datadog"
      image  = "index.docker.io/datadog/serverless-init:latest"
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name  = "DD_SITE"
        value = var.dd_site
      }
      env {
        name  = "DD_API_KEY"
        value = var.dd_api_key
      }
      env {
        name  = "DD_ENV"
        value = var.env
      }
      env {
        name  = "DD_VERSION"
        value = var.app_version
      }
      env {
        name  = "DD_SERVICE"
        value = "user-management"
      }
      env {
        name  = "DD_LOGS_ENABLED"
        value = "true"
      }
      env {
        name  = "DD_LOGS_INJECTION"
        value = "true"
      }
      env {
        name  = "DD_APM_COMPUTE_STATS_BY_SPAN_KIND"
        value = "true"
      }
      env {
        name  = "DD_APM_PEER_TAGS_AGGREGATION"
        value = "true"
      }
      env {
        name  = "DD_TRACE_REMOVE_INTEGRATION_SERVICE_NAMES_ENABLED"
        value = "true"
      }
      env {
        name  = "DD_APM_IGNORE_RESOURCES"
        value = "/opentelemetry.proto.collector.trace.v1.TraceService/Export$"
      }
      env {
        name  = "DD_OTLP_CONFIG_RECEIVER_PROTOCOLS_GRPC_ENDPOINT"
        value = "0.0.0.0:4317"
      }
      env {
        name  = "DD_AZURE_SUBSCRIPTION_ID"
        value = data.azurerm_subscription.primary.subscription_id
      }
      env {
        name  = "DD_AZURE_RESOURCE_GROUP"
        value = azurerm_resource_group.rg.name
      }
    }
  }
}