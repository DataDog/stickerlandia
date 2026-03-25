# PostgreSQL Flexible Server (shared cluster - services create their own databases)
resource "azurerm_postgresql_flexible_server" "shared" {
  name                          = "psql-stickerlandia-${var.env}"
  location                      = azurerm_resource_group.shared.location
  resource_group_name           = azurerm_resource_group.shared.name
  version                       = "16"
  delegated_subnet_id           = azurerm_subnet.postgresql.id
  private_dns_zone_id           = azurerm_private_dns_zone.postgresql.id
  public_network_access_enabled = false
  administrator_login           = var.postgresql_admin_username
  administrator_password        = random_password.postgresql_admin_password.result
  zone                          = "1"
  storage_mb                    = 32768
  storage_tier                  = "P4"
  sku_name                      = var.postgresql_sku_name
  tags                          = local.tags

  depends_on = [
    azurerm_private_dns_zone_virtual_network_link.postgresql
  ]
}
