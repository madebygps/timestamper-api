provider "azurerm" {
  features {}
}

resource "azurerm_resource_group" "resourcegroup" {
  name     = "rg-timestamperapi"
  location = "East US 2"
}

resource "azurerm_storage_account" "storageaccount" {
  name                     = "timestamperapistor"
  resource_group_name      = azurerm_resource_group.resourcegroup.name
  location                 = azurerm_resource_group.resourcegroup.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
}


resource "azurerm_service_plan" "plan" {
  name                = "timestamperapiplan"
  location            = azurerm_resource_group.resourcegroup.location
  resource_group_name = azurerm_resource_group.resourcegroup.name
  os_type             = "Linux"
  sku_name            = "Y1"
}

resource "azurerm_linux_function_app" "functionapp" {
  name                       = "timestamperapiapp"
  resource_group_name        = azurerm_resource_group.resourcegroup.name
  location                   = azurerm_resource_group.resourcegroup.location
  storage_account_name       = azurerm_storage_account.storageaccount.name
  storage_account_access_key = azurerm_storage_account.storageaccount.primary_access_key
  service_plan_id            = azurerm_service_plan.plan.id

  site_config {}

  app_settings = {
    "FUNCTIONS_WORKER_RUNTIME" = "dotnet-isolated"
  }

  depends_on = [
    azurerm_service_plan.plan,
  ]
}

resource "azurerm_linux_function_app_slot" "example" {
  name                       = "staging"
  function_app_id            = azurerm_linux_function_app.functionapp.id
  storage_account_name       = azurerm_storage_account.storageaccount.name

  site_config {
    
  }
}
