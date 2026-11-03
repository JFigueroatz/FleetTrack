terraform {
  required_version = ">= 1.7"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.110"
    }
  }

  # Estado local por ahora (sin backend remoto): el curso no financia un storage account
  # dedicado solo para el tfstate. Pasar a un backend "azurerm" si el equipo crece.
}

provider "azurerm" {
  features {}
  subscription_id = var.azure_subscription_id
}
