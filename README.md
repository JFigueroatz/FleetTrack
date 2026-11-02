# FleetTrack

Plataforma logística de rutas, tracking y entregas: dashboard web, API .NET y aplicación Android para conductores.

Proyecto final del curso 048 — Aseguramiento de la Calidad del Software, Universidad Mariano Gálvez.

## Equipo

| Integrante | Rol |
|---|---|
| Alejandro | Líder técnico · API, identidad, PostgreSQL, Terraform/Azure, Fortify · único aprobador y responsable de merge |
| Manuel | Dashboard web (React), Docker Compose, Ansible, Cypress/ZAP/axe |
| Andrea | Aplicación Android (MAUI), SQLite, consumidor RabbitMQ, Maestro, k6 |

El plan de trabajo completo está en `docs/plan-de-trabajo.docx` y los requisitos del curso en `docs/requisitos-curso.docx`.

## Estructura del repositorio

```
frontend/           Dashboard web (React + TypeScript + Vite)
backend/             API ASP.NET Core, Worker/consumidor .NET
mobile/              Aplicación Android (.NET MAUI)
infrastructure/
  terraform/         Infraestructura como código (Azure)
ansible/
  inventory/
  playbooks/
  roles/
tests/
  unit/ integration/ api/ cypress/ maestro/ performance/ security/
docs/                Plan de pruebas, arquitectura, matriz de riesgos, informe final
reports/
  sonar/ fortify/ dependency-check/ zap/ performance/ accessibility/
```

## Flujo de trabajo

Issue con criterios de aceptación → rama `feat/<numero>-<descripcion>` o `fix/<numero>-<descripcion>` → commits → pruebas → Pull Request con resumen y evidencia → revisión y aprobación de Alejandro → checks de Jenkins en verde → merge → cierre del Issue.

La rama `main` está protegida: requiere Pull Request, checks de Jenkins obligatorios y aprobación de un CODEOWNER.
