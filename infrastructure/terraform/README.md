# Terraform — ambiente QA en Azure

Aprovisiona una VM Linux (Ubuntu 22.04) con IP pública, red y reglas de firewall (NSG) para correr FleetTrack vía
Docker Compose detrás de un reverse proxy, configurado después por `infrastructure/ansible`.

## Estado de esta entrega (semana 5)

El código está completo y es el que se aplicaría, pero **no se ejecutó `terraform apply`** en esta sesión: la
elegibilidad de la cuenta Azure for Students (crédito, cuota) está pendiente de confirmar (ver sección 12 del plan
de trabajo). Ejecutarlo crea recursos reales y puede generar costo, así que es una decisión de Alejandro con la
suscripción en mano, no algo que se automatice sin supervisión.

## Ciclo de vida

```bash
cd infrastructure/terraform
terraform init
cp terraform.tfvars.example terraform.tfvars   # completar allowed_ssh_cidr y la llave publica
export TF_VAR_azure_subscription_id="<subscription id>"

terraform plan            # revisar antes de aplicar
terraform apply           # aprovisionar

# modificar: cambiar una variable (p.ej. vm_size) y volver a aplicar
terraform apply -var="vm_size=Standard_B1s"

terraform destroy         # destruir todo lo creado por este estado
```

## Qué crea

Resource group, VNet/subnet, IP pública estática, NSG (22 restringido a `allowed_ssh_cidr`, 80/443 abiertos) y la VM.
El `output vm_public_ip` alimenta el inventario de Ansible.

## Respaldo antes de destruir

Si la VM tiene datos de PostgreSQL que importan, respaldar antes de `destroy` (ver el procedimiento de
`docs/semana-4-mensajeria-y-recuperacion.md`); el disco de la VM no tiene `prevent_destroy` porque este es un
ambiente QA recreable, no producción.
