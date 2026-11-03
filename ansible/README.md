# Ansible — configuración del ambiente QA

Deja una VM Ubuntu (aprovisionada por `infrastructure/terraform`) lista para correr FleetTrack: instala Docker,
clona el repo, levanta `docker compose up -d --build` y publica el dashboard/API detrás de nginx como reverse proxy.

## Estado de esta entrega (semana 5)

Los playbooks están completos pero **no se ejecutaron contra un host real**: depende de que exista la VM de
Terraform (pendiente de la elegibilidad de Azure, ver `infrastructure/terraform/README.md`). Se usó
`ansible-playbook --syntax-check` / `--check` localmente para validar sintaxis; no hay `ansible` instalado en esta
máquina de desarrollo, así que ni siquiera eso se corrió — queda como siguiente paso explícito.

## Uso previsto

```bash
cd ansible
cp inventory.example.ini inventory.ini   # completar con la IP que entrega Terraform
ansible-playbook -i inventory.ini site.yml \
  -e postgres_password=*** -e rabbitmq_password=*** -e jwt_secret=***
```

Los secretos se pasan por `-e` o un vault de Ansible (`ansible-vault`), nunca se versionan en `inventory.ini` ni en
los templates.

## Estructura

- `roles/docker`: instala Docker Engine + Compose desde el repositorio oficial.
- `roles/fleettrack_app`: clona el repo, genera `.env` desde una plantilla y levanta el stack.
- `roles/reverse_proxy`: nginx como proxy de `/` (dashboard, puerto 3000) y `/api` (API, puerto 8080).

## Ambiente QA reproducible

"Reproducible" significa: otra persona con el inventario y los secretos corre `ansible-playbook site.yml` sobre una
VM nueva y obtiene el mismo ambiente, sin pasos manuales ni documentación aparte del propio playbook.
