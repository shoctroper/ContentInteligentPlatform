# ADR-006: Reactivación del frontend Angular

**Estado:** Aceptado — supersede parcialmente a ADR-005

## Contexto
ADR-005 difirió el frontend hasta que el usuario pudiera revisar wireframes/UX en detalle, dado que no tenía
disponibilidad para iterar. El usuario ahora dio luz verde explícita a construir una versión básica sin esperar
esa revisión completa, priorizando tener un cliente funcional del contrato OpenAPI ya definido.

## Decisión
Se construye un frontend Angular 18 (standalone components, sin NgModules) con alcance mínimo:
- Formulario para generar un guion (texto libre + selector de perfil + formato de salida).
- Vista de historial de generaciones con detalle.
- Calificación de una generación (1-5 + comentario).

Sin autenticación (uso personal, ADR-002/consistente con el resto del backend). Sin diseño visual elaborado:
prioridad es que el flujo funcione contra la Api real, no la estética. Ajustes de UX quedan para cuando el
usuario tenga tiempo de revisar.

## Consecuencias
- Se habilita CORS en la Api para el origen de desarrollo de Angular (`http://localhost:4200`).
- El cliente HTTP se genera a mano (no autogenerado desde `openapi.yaml` todavía) por velocidad; si el contrato
  cambia frecuentemente, vale la pena automatizar esto después con `openapi-generator` o `orval`.
