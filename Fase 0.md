# FASE 0 — Definición Técnica y Arquitectura

## Objetivo

Definir completamente la arquitectura, límites, contratos y flujo del microservicio antes de comenzar la implementación.

La finalidad de esta fase es evitar:
- deuda técnica temprana
- refactors masivos
- acoplamiento incorrecto
- arquitectura inconsistente
- lógica mezclada
- sobreingeniería innecesaria

---

# Objetivo del Proyecto

Desarrollar un microservicio REST en .NET 9 capaz de generar resúmenes a partir de documentos PDF utilizando LLMs.

Características principales:
- Streaming de respuestas en tiempo real mediante SSE
- Soporte para PDFs con imágenes mediante OCR
- Integración con Groq + Llama
- Semantic Kernel encapsulado
- Arquitectura limpia (Clean Architecture)
- Diseño extensible para múltiples modelos LLM
- Sin persistencia
- Sin autenticación
- Stateless
- Compatible con Linux y Docker

---

# Stack Tecnológico Confirmado

| Tecnología | Uso |
|---|---|
| .NET 9 | Framework principal |
| ASP.NET Core Minimal APIs | API REST |
| Semantic Kernel | Orquestación LLM |
| Groq | Inferencia LLM |
| Llama | Modelo principal |
| SSE | Streaming |
| Serilog | Logging |
| Polly | Resiliencia |
| FluentValidation | Validaciones |
| PdfPig | Extracción textual PDF |
| Tesseract OCR | OCR |
| PDFium | Render PDF → Imagen |

---

# Decisiones Arquitectónicas

## Arquitectura

Se utilizará:
- Clean Architecture
- separación estricta de responsabilidades
- dependencia hacia adentro
- infraestructura desacoplada

---

## Estilo API

- Minimal APIs
- Versionado:
  - `/api/v1`
- Streaming:
  - `text/event-stream`

---

## Persistencia

No existirá:
- base de datos
- almacenamiento de archivos
- historial
- caché persistente

El servicio será completamente stateless.

---

## Autenticación

No se implementará autenticación en la V1.

---

## Compatibilidad

Todo el sistema debe diseñarse pensando en:
- Linux
- Docker
- containers

No deben utilizarse librerías Windows-only.

---

# Estructura de la Solución

```text
src/

├── Rag.Api
├── Rag.Application
├── Rag.Domain
├── Rag.Infrastructure
└── Rag.Shared
```

---

# Responsabilidades por Proyecto

## Rag.Api

Responsabilidades:
- endpoints
- middleware
- swagger
- configuración HTTP
- configuración SSE
- DI bootstrap

NO debe contener:
- lógica negocio
- lógica OCR
- lógica LLM

---

## Rag.Application

Responsabilidades:
- casos de uso
- contratos
- DTOs
- interfaces
- validaciones
- orchestration

---

## Rag.Domain

Responsabilidades:
- enums
- constants
- value objects
- reglas core
- contratos de dominio

NO debe contener:
- SDKs
- HTTP
- Semantic Kernel
- OCR
- Pdf parsing

---

## Rag.Infrastructure

Responsabilidades:
- Groq integration
- Semantic Kernel integration
- OCR
- Pdf parsing
- streaming implementations
- external services

---

## Rag.Shared

Responsabilidades:
- utilidades compartidas
- extensiones
- helpers reutilizables

---

# Flujo Principal del Sistema

```text
Request
 ↓
Validate Request
 ↓
Extract PDF Text
 ↓
OCR Fallback (si es necesario)
 ↓
Normalize Text
 ↓
Chunk Document
 ↓
Generate Chunk Summaries
 ↓
Reduce Summaries
 ↓
Stream Final Response
```

---

# Flujo OCR

```text
PDF
 ↓
Try Text Extraction
 ↓
Text Found?
 ├── YES → Continue
 └── NO  → OCR Pipeline
              ↓
         Render Pages
              ↓
         OCR Per Page
              ↓
         Combine Text
```

---

# Contratos SSE

## Content-Type

```http
text/event-stream
```

---

## Eventos soportados

| Evento | Descripción |
|---|---|
| status | Estado actual |
| chunk | Fragmento resumen |
| completed | Finalización |
| error | Error procesado |

---

## Estados del procesamiento

```text
extracting_text
running_ocr
chunking_document
generating_summary
reducing_summary
completed
```

---

# Endpoint Principal

## POST

```http
/api/v1/summaries/stream
```

---

## Request

Multipart form-data:
- file
- style
- maxTokens

---

## Response

```http
text/event-stream
```

---

# Límites Operativos

| Configuración | Valor Inicial |
|---|---|
| Max file size | 15MB |
| Max PDF pages | Pendiente definir |
| Max chunks | Pendiente definir |
| Request timeout | Pendiente definir |
| OCR timeout | Pendiente definir |

---

# Reglas Técnicas Obligatorias

## Código

- Async/await obligatorio
- CancellationToken obligatorio
- No magic strings
- No lógica en endpoints
- No lógica en infraestructura dentro del dominio
- Strongly typed configuration
- Dependency injection obligatoria

---

## Arquitectura

- El dominio no conoce infraestructura
- Semantic Kernel debe estar encapsulado
- Los LLM providers deben abstraerse
- Los parsers deben abstraerse
- OCR desacoplado

---

# Abstracciones Principales

## Document Processing

```csharp
IDocumentParser
IPdfTextExtractor
IPdfOcrExtractor
```

---

## LLM

```csharp
IStreamingTextGenerator
ISummaryGenerator
IPromptProvider
```

---

## Chunking

```csharp
ITextChunker
ITokenEstimator
```

---

# Estrategia de PDFs

## PDFs Textuales

Se utilizará:
- PdfPig

---

## PDFs Escaneados

Pipeline:
- Render página → imagen
- OCR por página
- consolidación textual

---

## Estrategia Inteligente

El sistema:
1. intentará extracción textual
2. si el texto es insuficiente:
   - activará OCR automáticamente

---

# Estrategia de Resumen

## Tipo

Map-Reduce Summarization

---

## MAP Phase

Resumen individual por chunk.

---

## REDUCE Phase

Consolidación de todos los resúmenes parciales.

---

# Estrategia de Prompts

Los prompts NO deben escribirse inline.

Estructura:

```text
Prompts/

├── summarize.txt
├── reduce.txt
└── executive-summary.txt
```

---

# Configuración

## appsettings.json
SummaryService
```json
{
  "Groq": {
    "ApiKey": "",
    "Model": "llama-3.3-70b-versatile"
  },
  "Summary": {
    "MaxFileSizeMb": 15,
    "ChunkSize": 12000,
    "MaxTokens": 2048
  }
}
```

---

# Logging y Observabilidad

## Logging

Se utilizará:
- Serilog
- structured logging
- correlation ids

---

## Métricas futuras

- request duration
- OCR duration
- LLM duration
- failed requests

---

# Resiliencia

Se utilizará Polly para:
- retry
- timeout
- circuit breaker

---

# Fuera de Alcance V1

No se implementará:
- autenticación
- base de datos
- embeddings
- RAG
- vector DB
- multimodal LLM
- chat conversacional
- plugins avanzados
- workflows multi-agent

---

# Resultado Esperado al Final del Proyecto

El microservicio deberá:
- recibir PDFs
- detectar PDFs escaneados
- ejecutar OCR automáticamente
- resumir documentos grandes
- hacer streaming en tiempo real
- soportar múltiples modelos en el futuro
- mantener arquitectura limpia
- ser fácilmente dockerizable
- escalar horizontalmente

---

# Criterios para Finalizar la Fase 0

La fase termina cuando estén definidos:
- arquitectura final
- estructura solución
- responsabilidades por capa
- flujo completo
- contratos SSE
- interfaces core
- límites operativos
- estrategia OCR
- estrategia chunking
- estrategia prompts

Solo después de esto comenzará la implementación.