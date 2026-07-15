# ADR-005: Frontend diferido

**Estado:** Aceptado

## Contexto
El usuario programa principalmente en .NET y no tiene disponibilidad para iterar en este ciclo. El PRD pide Angular, pero construir frontend sin poder validar UX con el usuario es alto riesgo de retrabajo.

## Decisión
Esta iteración entrega **solo backend**: API REST + Swagger/OpenAPI, consumible con Postman/curl/Swagger UI. Angular queda para una iteración posterior una vez el usuario pueda revisar wireframes y flujo de UX.

## Consecuencias
- El valor del MVP (generar guiones) es verificable hoy vía Swagger, sin bloquear por frontend.
- Cuando el usuario esté disponible, el contrato OpenAPI ya definido acelera el desarrollo Angular (generación de cliente HTTP tipado).
