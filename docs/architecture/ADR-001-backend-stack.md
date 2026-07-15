# ADR-001: Stack de backend

**Estado:** Aceptado (auto-aprobado — usuario sin disponibilidad para gate manual)

## Contexto
El PRD fija Angular + ASP.NET Core + Clean Architecture + DDD + CQRS. Usuario único, sin requisitos de escala horizontal inmediatos.

## Decisión
.NET 8 (LTS) + ASP.NET Core Minimal API. Clean Architecture con 4 capas: `Domain`, `Application` (CQRS con MediatR), `Infrastructure`, `Api`. Validación con FluentValidation. Mapeo de errores a `ProblemDetails` (RFC 7807).

## Alternativas descartadas
- Controllers MVC clásicos: más ceremonia sin beneficio para el tamaño del MVP.
- Microservicios: prematuro para un solo usuario y un solo desarrollador (R4 del análisis: riesgo de alcance).

## Consecuencias
- Fácil de testear (handlers aislados vía MediatR).
- Escalar a microservicios más adelante es viable porque los bounded contexts ya están separados en carpetas/namespaces.
