resource "azurerm_cosmosdb_account" "user_management" {
  name                = "stickerlandia-users"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  offer_type          = "Standard"
  kind                = "GlobalDocumentDB" # SQL API

  free_tier_enabled = true

  consistency_policy {
    consistency_level       = "Session"
    max_interval_in_seconds = 5
    max_staleness_prefix    = 100
  }

  geo_location {
    location          = azurerm_resource_group.rg.location
    failover_priority = 0
  }

  tags = {
    environment = var.env
    application = "stickerlandia-user-management"
  }
}

resource "azurerm_cosmosdb_sql_database" "user_database" {
  name                = "users"
  resource_group_name = azurerm_resource_group.rg.name
  account_name        = azurerm_cosmosdb_account.user_management.name
  # Free tier automatically adjusts throughput settings
}
