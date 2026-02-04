# Virtual Network
resource "azurerm_virtual_network" "main" {
  name                = "vnet-stickerlandia-${var.env}"
  location            = azurerm_resource_group.shared.location
  resource_group_name = azurerm_resource_group.shared.name
  address_space       = [var.vnet_address_space]
  tags                = local.tags
}

# Container App Environment Subnet (minimum /23 required by Azure)
resource "azurerm_subnet" "container_apps" {
  name                 = "snet-container-apps"
  resource_group_name  = azurerm_resource_group.shared.name
  virtual_network_name = azurerm_virtual_network.main.name
  address_prefixes     = [var.container_apps_subnet_prefix]

  delegation {
    name = "container-apps-delegation"
    service_delegation {
      name    = "Microsoft.App/environments"
      actions = ["Microsoft.Network/virtualNetworks/subnets/join/action"]
    }
  }
}

# Private Endpoint Subnet
resource "azurerm_subnet" "private_endpoints" {
  name                 = "snet-private-endpoints"
  resource_group_name  = azurerm_resource_group.shared.name
  virtual_network_name = azurerm_virtual_network.main.name
  address_prefixes     = [var.private_endpoints_subnet_prefix]
}

# PostgreSQL Flexible Server Subnet (requires delegation)
resource "azurerm_subnet" "postgresql" {
  name                 = "snet-postgresql"
  resource_group_name  = azurerm_resource_group.shared.name
  virtual_network_name = azurerm_virtual_network.main.name
  address_prefixes     = [var.postgresql_subnet_prefix]

  delegation {
    name = "postgresql-delegation"
    service_delegation {
      name    = "Microsoft.DBforPostgreSQL/flexibleServers"
      actions = ["Microsoft.Network/virtualNetworks/subnets/join/action"]
    }
  }
}

# Private DNS Zone for PostgreSQL Flexible Server with VNet Integration
# Note: VNet-integrated Flexible Servers require a zone ending in .postgres.database.azure.com
# The zone name cannot match the server name, so we use the format: stickerlandia-{env}.postgres.database.azure.com
# This is different from privatelink.postgres.database.azure.com which is for Private Endpoint connections
resource "azurerm_private_dns_zone" "postgresql" {
  name                = "stickerlandia-${var.env}.postgres.database.azure.com"
  resource_group_name = azurerm_resource_group.shared.name
  tags                = local.tags
}

resource "azurerm_private_dns_zone_virtual_network_link" "postgresql" {
  name                  = "postgresql-vnet-link"
  resource_group_name   = azurerm_resource_group.shared.name
  private_dns_zone_name = azurerm_private_dns_zone.postgresql.name
  virtual_network_id    = azurerm_virtual_network.main.id
  registration_enabled  = false
  tags                  = local.tags
}

# Private DNS Zone for Key Vault
resource "azurerm_private_dns_zone" "keyvault" {
  name                = "privatelink.vaultcore.azure.net"
  resource_group_name = azurerm_resource_group.shared.name
  tags                = local.tags
}

resource "azurerm_private_dns_zone_virtual_network_link" "keyvault" {
  name                  = "keyvault-vnet-link"
  resource_group_name   = azurerm_resource_group.shared.name
  private_dns_zone_name = azurerm_private_dns_zone.keyvault.name
  virtual_network_id    = azurerm_virtual_network.main.id
  registration_enabled  = false
  tags                  = local.tags
}
