
## Versión 1.0

**Autor:** Mario Alberto Colli Ek

**Estado:** Draft

**Fecha:** Julio 2026

---

# 1. Visión

## Objetivo

Construir una plataforma capaz de transformar cualquier fuente de información en contenido optimizado para diferentes plataformas sociales utilizando Inteligencia Artificial.

La plataforma NO debe depender de un proveedor de IA específico y debe poder funcionar con OpenAI, Claude, Gemini, Ollama o cualquier modelo futuro.

El conocimiento editorial del sistema nunca deberá vivir dentro del modelo de IA.

Todo el conocimiento deberá almacenarse en archivos versionables.

---

# 2. Problema

Actualmente los LLM requieren repetir instrucciones constantemente.

Cada nuevo prompt implica volver a explicar:

* personalidad
* tono
* formato
* reglas
* estilo
* duración
* estructura

Esto provoca:

* consumo innecesario de tokens
* poca mantenibilidad
* dificultad para evolucionar
* duplicación de información

El proyecto busca separar completamente el conocimiento editorial del modelo de IA.

---

# 3. Objetivos

La plataforma deberá:

* producir guiones de alta calidad
* mantener una personalidad consistente
* permitir múltiples perfiles editoriales
* permitir múltiples formatos de salida
* permitir cambiar de proveedor IA sin modificar la lógica
* ser completamente versionable mediante Git
* ser modular
* ser extensible

---

# 4. Objetivos NO funcionales

La plataforma NO deberá:

* depender de prompts gigantes
* entrenar modelos propios
* almacenar conocimiento dentro del código
* estar acoplada a OpenAI
* generar información inventada

---

# 5. Casos de uso

## Caso 1

Entrada:

Artículo periodístico

Salida:

TikTok 90 segundos

---

## Caso 2

Entrada:

Resumen escrito de un podcast

Salida:

TikTok estilo documental

---

## Caso 3

Entrada:

Notas personales

Salida:

Short de YouTube

---

## Caso 4

Entrada:

URL

Salida:

Guion

---

## Caso 5

Entrada:

Transcripción de podcast

Salida:

Video estilo "Breaking News"

---

## Caso 6

Entrada:

Idea del usuario

Salida:

Tres propuestas de guion

---

# 6. Arquitectura Conceptual

```
Fuentes

↓

Normalización

↓

Análisis

↓

Enriquecimiento

↓

Selección de Perfil

↓

Prompt Builder

↓

Proveedor IA

↓

Validación

↓

Salida
```

---

# 7. Tipos de Entrada

La plataforma deberá aceptar:

* Artículos
* Texto libre
* Transcripciones
* Podcasts
* Videos
* RSS
* Reddit
* Twitter/X
* Threads
* Noticias manuales
* Ideas del usuario

---

# 8. Normalización

Toda fuente deberá convertirse a un modelo común.

Ejemplo:

```
NewsItem

Title

Summary

Facts

Timeline

People

Organizations

Topics

Sources

Quotes

Context

Confidence
```

Una vez normalizado, el sistema dejará de importar de dónde provino la información.

---

# 9. Knowledge Engine

Todo el conocimiento vivirá en archivos Markdown.

Ejemplo:

```
knowledge/

identity.md

journalism.md

ethics.md

rules.md

fact_checking.md

tiktok.md

youtube.md

podcast.md

hooks.md

cta.md

storytelling.md

profiles/

examples/

templates/

glossary/
```

Ningún archivo deberá superar una única responsabilidad.

---

# 10. Sistema de Perfiles

Los perfiles modificarán únicamente la narrativa.

Nunca modificarán los hechos.

Ejemplos:

Serio

Documental

Periodístico

Anime

Película

Humor

Misterio

Conspiración

Tecnología

Historia

Economía

Gaming

Ciencia

Finanzas

Política

Cada perfil podrá heredar de otro.

Ejemplo:

```
Periodístico

↓

Documental

↓

Historia
```

---

# 11. Prompt Builder

El Prompt Builder será completamente dinámico.

Ejemplo:

Base

*

Periodismo

*

TikTok

*

Perfil Anime

*

Storytelling

*

CTA

*

Noticia

↓

Prompt Final

---

# 12. Pipeline IA

El modelo nunca escribirá inmediatamente.

Siempre trabajará por etapas.

## Etapa 1

Comprensión

---

## Etapa 2

Extracción de hechos

---

## Etapa 3

Validación

---

## Etapa 4

Contexto

---

## Etapa 5

Planeación narrativa

---

## Etapa 6

Redacción

---

## Etapa 7

Autoevaluación

---

## Etapa 8

Salida

---

# 13. Formatos de Salida

TikTok

YouTube Shorts

Instagram Reel

Podcast

YouTube largo

Newsletter

Artículo

Thread

Markdown

JSON

HTML

---

# 14. Formato JSON

Toda generación deberá devolver además un JSON.

Ejemplo:

```
Title

Hook

Introduction

Body

Ending

CTA

Hashtags

Keywords

Category

EstimatedDuration

Confidence

MissingInformation

Sources
```

---

# 15. Memoria

Todo contenido generado deberá almacenarse.

Guardar:

Fecha

Modelo

Perfil

Prompt

Resultado

Tiempo

Costo

Tokens

Calificación

Comentarios

Esto permitirá mejorar continuamente.

---

# 16. Sistema de Versionado

Todo conocimiento será versionado.

Ejemplo:

```
profiles/

serious/

v1

v2

v3
```

Nunca sobrescribir versiones.

---

# 17. Proveedores IA

La plataforma deberá soportar:

OpenAI

Claude

Gemini

Ollama

LM Studio

vLLM

Azure OpenAI

Future Providers

Todos mediante una interfaz común.

---

# 18. Arquitectura de Software

Frontend

Angular

Backend

ASP.NET Core

Arquitectura

Clean Architecture

DDD

CQRS

Infrastructure

SQLite (MVP)

PostgreSQL (Producción)

Repositorio Git

---

# 19. Roadmap

## MVP

Generar guiones desde texto.

Perfiles.

Markdown.

JSON.

Historial.

---

## V2

Transcripción automática.

RSS.

Captura web.

Versionado de perfiles.

---

## V3

Evaluación automática.

A/B Testing.

Embeddings.

Vector Database.

Recuperación de ejemplos.

---

## V4

Agentes especializados.

Editor.

Investigador.

Fact Checker.

Narrador.

Director Editorial.

---

# 20. Principios del Proyecto

La información es más importante que el modelo.

Los perfiles nunca modificarán los hechos.

Todo conocimiento debe ser versionable.

Los prompts son código.

La IA es intercambiable.

La plataforma debe ser reproducible.

Cada componente debe tener una única responsabilidad.

El sistema debe poder evolucionar durante años sin requerir una reescritura completa.

---

# 21. Visión a Largo Plazo

La plataforma evolucionará desde un generador de guiones hacia un Sistema Operativo para Creadores de Contenido (Content OS).

Este sistema permitirá ingerir información desde múltiples fuentes, analizarla, enriquecerla con contexto, aplicar un estilo editorial consistente y producir contenido optimizado para distintas plataformas, manteniendo siempre la separación entre conocimiento editorial, lógica de negocio y proveedor de IA.

El objetivo final no es generar un video, sino construir una plataforma de producción de contenido escalable, reutilizable y preparada para adaptarse a futuras tecnologías de inteligencia artificial.
