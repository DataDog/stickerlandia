# Random suffix for globally unique Key Vault name
resource "random_string" "kv_suffix" {
  length  = 6
  special = false
  upper   = false
}

# Shared Key Vault for secrets management
resource "azurerm_key_vault" "shared" {
  name                          = "kv-sticker-${var.env}-${random_string.kv_suffix.result}"
  location                      = azurerm_resource_group.shared.location
  resource_group_name           = azurerm_resource_group.shared.name
  tenant_id                     = data.azurerm_client_config.current.tenant_id
  sku_name                      = "standard"
  soft_delete_retention_days    = 7
  purge_protection_enabled      = false
  public_network_access_enabled = true
  rbac_authorization_enabled    = true
  tags                          = local.tags

  network_acls {
    bypass         = "AzureServices"
    default_action = "Allow"
  }
}

# Private Endpoint for Key Vault
resource "azurerm_private_endpoint" "keyvault" {
  name                = "pe-keyvault-${var.env}"
  location            = azurerm_resource_group.shared.location
  resource_group_name = azurerm_resource_group.shared.name
  subnet_id           = azurerm_subnet.private_endpoints.id
  tags                = local.tags

  private_service_connection {
    name                           = "keyvault-connection"
    private_connection_resource_id = azurerm_key_vault.shared.id
    is_manual_connection           = false
    subresource_names              = ["vault"]
  }

  private_dns_zone_group {
    name                 = "keyvault-dns-group"
    private_dns_zone_ids = [azurerm_private_dns_zone.keyvault.id]
  }
}

# Role assignment for current user to manage secrets during deployment
resource "azurerm_role_assignment" "keyvault_admin" {
  scope                = azurerm_key_vault.shared.id
  role_definition_name = "Key Vault Secrets Officer"
  principal_id         = data.azurerm_client_config.current.object_id
}

# Store Datadog API Key in Key Vault (shared across all services)
resource "azurerm_key_vault_secret" "dd_api_key" {
  name         = "dd-api-key"
  value        = var.dd_api_key
  key_vault_id = azurerm_key_vault.shared.id
  tags         = local.tags

  depends_on = [
    azurerm_role_assignment.keyvault_admin,
    azurerm_private_endpoint.keyvault
  ]
}

# Store PostgreSQL admin password in Key Vault
resource "random_password" "postgresql_admin_password" {
  length           = 32
  special          = true
  override_special = "!#$%&*()-_=+[]{}<>:?"
}

resource "azurerm_key_vault_secret" "postgresql_admin_password" {
  name         = "postgresql-admin-password"
  value        = random_password.postgresql_admin_password.result
  key_vault_id = azurerm_key_vault.shared.id
  tags         = local.tags

  depends_on = [
    azurerm_role_assignment.keyvault_admin,
    azurerm_private_endpoint.keyvault
  ]
}
