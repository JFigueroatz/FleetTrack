variable "azure_subscription_id" {
  description = "Subscription de Azure for Students. Se pasa por variable de entorno TF_VAR_azure_subscription_id, nunca en el repo."
  type        = string
}

variable "project_name" {
  description = "Prefijo de los recursos."
  type        = string
  default     = "fleettrack"
}

variable "environment" {
  description = "Ambiente (qa, demo). Entra en el nombre de los recursos y en las etiquetas."
  type        = string
  default     = "qa"
}

variable "location" {
  description = "Región de Azure más barata/disponible para Azure for Students."
  type        = string
  default     = "eastus"
}

variable "vm_size" {
  description = "Tamaño de la VM. B1s/B2s entran en el crédito de Azure for Students; no usar nada mayor sin revisar costo."
  type        = string
  default     = "Standard_B2s"
}

variable "admin_username" {
  description = "Usuario administrador de la VM Linux."
  type        = string
  default     = "fleettrack-admin"
}

variable "ssh_public_key_path" {
  description = "Ruta local a la llave pública SSH que se inyecta en la VM (ninguna llave privada se versiona)."
  type        = string
  default     = "~/.ssh/id_ed25519_alejandro.pub"
}

variable "allowed_ssh_cidr" {
  description = "Rango IP autorizado para SSH (puerto 22). Restringir a la IP del equipo, nunca 0.0.0.0/0."
  type        = string
}

variable "allowed_http_cidr" {
  description = "Rango IP autorizado para HTTP/HTTPS (80/443) hacia el reverse proxy. Por defecto abierto, como un sitio QA público."
  type        = string
  default     = "0.0.0.0/0"
}
