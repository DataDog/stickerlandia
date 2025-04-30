resource "azurerm_resource_group" "rg" {
  location = var.location
  name     = "${var.resourceGroupName}-${var.env}"
  tags = local.tags
}

resource "azurerm_storage_account" "functions_storage_account" {
  name                     = "usersfunc${var.env}"
  resource_group_name      = azurerm_resource_group.rg.name
  location                 = azurerm_resource_group.rg.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  tags = local.tags
}

resource "azurerm_storage_container" "functions_storage_container" {
  name                  = "users-container"
  storage_account_id    = azurerm_storage_account.functions_storage_account.id
  container_access_type = "private"
}

resource "azurerm_storage_container" "functions_flex_storage_container" {
  name                  = "users-flex-container"
  storage_account_id    = azurerm_storage_account.functions_storage_account.id
  container_access_type = "private"
}

resource "azurerm_service_plan" "functions_app_service_plan" {
  name                = "stickerlandia-users-service-plan-${var.env}"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  os_type             = "Linux"
  sku_name            = "Y1"
  tags = local.tags
}

locals {
  environment_variables_base = {
    DD_API_KEY                 = var.dd_api_key
    DD_SITE = var.dd_site
    DD_ENV                     = var.env
    DD_SERVICE                 = "stickerlandia-users"
    DD_VERSION                 = "latest"
    WEBSITE_RUN_FROM_PACKAGE   = 1
  }
  environment_variables_flex_base = {
    DD_API_KEY                 = var.dd_api_key
    DD_SITE = var.dd_site
    DD_ENV                     = var.env
    DD_SERVICE                 = "stickerlandia-users"
    DD_VERSION                 = "latest"
  }
  dotnet_tracer_home = "/home/site/wwwroot/datadog"
  environment_variables_dotnet = {
    CORECLR_ENABLE_PROFILING                     = "1"
    CORECLR_PROFILER                             = "{846F5F1C-F9AE-4B07-969E-05C26BC060D8}"
    CORECLR_PROFILER_PATH                        = "${local.dotnet_tracer_home}/linux-x64/Datadog.Trace.ClrProfiler.Native.so"
    DD_DOTNET_TRACER_HOME                        = local.dotnet_tracer_home
    DD_TRACE_HTTP_CLIENT_EXCLUDED_URL_SUBSTRINGS = "monitor.azure, applicationinsights.azure, metadata/instance/compute, admin/host, AzureFunctionsRpcMessages.FunctionRpc"
    DD_TRACE_AspNetCore_ENABLED                  = true
    FUNCTIONS_INPROC_NET8_ENABLED                = 0
  }

  app_settings = {
    "messaging" = azurerm_servicebus_namespace.stickerlandia_users_service_bus.default_primary_connection_string
    "ConnectionStrings__messaging" = azurerm_servicebus_namespace.stickerlandia_users_service_bus.default_primary_connection_string
    "ConnectionStrings__cosmosdb" = "AccountEndpoint=${azurerm_cosmosdb_account.user_management.endpoint};AccountKey=${azurerm_cosmosdb_account.user_management.primary_key};"
    "cosmosdb" = "AccountEndpoint=${azurerm_cosmosdb_account.user_management.endpoint};AccountKey=${azurerm_cosmosdb_account.user_management.primary_key};"
    "Auth__Issuer"= "https://stickerlandia.com"
    "Auth__Audience"= "https://stickerlandia.com"
    "Auth__Key"= "This is a super secret key that should not be used in production'"
  }

  tags = {
    env = var.env
    service = "stickerlandia-users"
    version = "latest"
  }
  environment_variables = merge(
    local.environment_variables_base,
    local.environment_variables_dotnet,
    local.app_settings
  )
  flex_environment_variables = merge(
    local.environment_variables_flex_base,
    local.environment_variables_dotnet,
    local.app_settings
  )
}

resource "azurerm_application_insights" "ai" {
  name                = "ai-stickerlandia-users-${var.env}"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  application_type    = "web"
  workspace_id = "/subscriptions/4ee0be82-09a4-484d-a7e7-789e02eb88fe/resourceGroups/ai_ai-stickerlandia-users-local_84db782e-6908-4bbe-8f2a-3811f89893b7_managed/providers/Microsoft.OperationalInsights/workspaces/managed-ai-stickerlandia-users-local-ws"
  tags = local.tags
}

resource "azurerm_linux_function_app" "function_app" {
  name                = "stickerlandia-users-functions-${var.env}"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location

  storage_account_name       = azurerm_storage_account.functions_storage_account.name
  storage_account_access_key = azurerm_storage_account.functions_storage_account.primary_access_key
  service_plan_id            = azurerm_service_plan.functions_app_service_plan.id

  site_config {
    application_stack {
      use_dotnet_isolated_runtime = true
      dotnet_version              = "8.0"
    }
    application_insights_connection_string = azurerm_application_insights.ai.connection_string
    application_insights_key               = azurerm_application_insights.ai.instrumentation_key
  }
  app_settings = local.environment_variables
  tags = local.tags

  lifecycle {
    ignore_changes = [
      tags["hidden-link: /app-insights-conn-string"],
      tags["hidden-link: /app-insights-instrumentation-key"],
      tags["hidden-link: /app-insights-resource-id"]
    ]
  }
}

resource "azurerm_service_plan" "flex_service_plan" {
  name                = "stickerlandia-users-flex-${var.env}"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  sku_name            = "FC1"
  os_type             = "Linux"
}

resource "azurerm_function_app_flex_consumption" "flex_function_app" {
  name                = "stickerlandia-users-flex-${var.env}"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  service_plan_id     = azurerm_service_plan.flex_service_plan.id

  storage_container_type      = "blobContainer"
  storage_container_endpoint  = "${azurerm_storage_account.functions_storage_account.primary_blob_endpoint}${azurerm_storage_container.functions_flex_storage_container.name}"
  storage_authentication_type = "StorageAccountConnectionString"
  storage_access_key          = azurerm_storage_account.functions_storage_account.primary_access_key
  runtime_name                = "dotnet-isolated"
  runtime_version             = "8.0"
  maximum_instance_count      = 40
  instance_memory_in_mb       = 2048

  app_settings = local.flex_environment_variables
  tags = local.tags

  site_config {
    application_insights_connection_string = azurerm_application_insights.ai.connection_string
    application_insights_key               = azurerm_application_insights.ai.instrumentation_key
  }
  lifecycle {
    ignore_changes = [
      tags["hidden-link: /app-insights-conn-string"],
      tags["hidden-link: /app-insights-instrumentation-key"],
      tags["hidden-link: /app-insights-resource-id"]
    ]
  }
}