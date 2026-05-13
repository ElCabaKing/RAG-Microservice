# Contexto del Proyecto — Microservicio de Resúmenes con LLMs

## Objetivo del proyecto

Desarrollar un microservicio REST en .NET capaz de generar resúmenes a partir de documentos usando LLMs.

El sistema debe ser:
- limpio arquitectónicamente
- extensible
- mantenible
- preparado para streaming
- preparado para múltiples modelos LLM
- sin sobreingeniería innecesaria

---

# Stack y decisiones técnicas tomadas

## Plataforma
- .NET 9
- ASP.NET Core Minimal APIs

---

## Arquitectura
- Clean Architecture

Estructura esperada:

```text
src/
├── SummaryService.Api
├── SummaryService.Application
├── SummaryService.Domain
├── SummaryService.Infrastructure
└── SummaryService.Shared
```

---

# LLMs

## Provider inicial
- Groq

## Modelo inicial
- Llama

Ejemplos esperados:
- llama-3.3-70b-versatile
- llama-3.1-8b-instant

---

# Semantic Kernel

Semantic Kernel será usado:
- como herramienta de orquestación
- encapsulado en Infrastructure

NO debe:
- contaminar Domain
- contaminar Application
- ser el centro de la arquitectura

La app debe depender de abstracciones propias.

---

# Funcionalidad principal

## Solo resumen de documentos

NO:
- chat
- embeddings
- vector DB
- RAG
- agentes
- workflows complejos

---

# Persistencia

NO habrá:
- base de datos
- almacenamiento persistente

El servicio será:
- stateless
- orientado a procesamiento en memoria

---

# Seguridad

Inicialmente:
- sin autenticación
- sin autorización

---

# Streaming

Se usará:
- Server-Sent Events (SSE)

NO WebSockets.

---

# Endpoint esperado

```http
POST /api/v1/summaries/stream
```

Response:
```http
text/event-stream
```

---

# Eventos SSE esperados

```text
event: status
event: chunk
event: completed
event: error
```

Posibles estados:
- extracting_text
- chunking_document
- generating_summary
- reducing_summary

---

# Archivos soportados

## Inicialmente
- PDF
- TXT

---

# PDFs con imágenes

El sistema debe soportar:
- PDFs textuales
- PDFs escaneados
- PDFs con imágenes

---

# OCR

Se decidió usar:
- OCR tradicional
- NO multimodal LLMs

---

# Librerías OCR/PDF sugeridas

## Extracción PDF
- PdfPig

## OCR
- Tesseract OCR
- Tesseract .NET wrapper

## Render PDF → imagen
- PDFiumSharp

---

# Estrategia PDF

Pipeline esperado:

```text
Upload PDF
   ↓
Detectar si tiene texto
   ↓
SI tiene texto
   → extracción directa
NO
   ↓
OCR
   ↓
Texto normalizado
```

---

# Estrategia de procesamiento

Pipeline esperado:

```text
Upload
 ↓
Validation
 ↓
Parse document
 ↓
Normalize text
 ↓
Chunking
 ↓
MAP summaries
 ↓
REDUCE summary
 ↓
Streaming SSE
```

---

# Estrategia de resumen

Se usará:
- map-reduce summarization

---

# Chunking

Será obligatorio debido a:
- límites de contexto
- PDFs grandes
- control de tokens

---

# Token control

Debe existir:
- control de max tokens
- token estimation
- límites por modelo

---

# Restricciones operativas

## Tamaño máximo inicial
- 15 MB por archivo

## Recomendación técnica
Agregar:
- límite de páginas
- timeouts
- cancellation tokens

---

# Recomendaciones arquitectónicas importantes

## NO usar magic strings

Usar:
- enums
- constants
- strongly typed settings

---

# Semantic Kernel

NO hacer:

```text
Controller → Kernel.InvokePromptAsync(...)
```

SÍ hacer:

```text
Controller
 ↓
Application
 ↓
Abstracción propia
 ↓
Infrastructure
 ↓
Semantic Kernel
```

---

# Interfaces sugeridas

## Streaming generator

```csharp
public interface IStreamingTextGenerator
{
    IAsyncEnumerable<string> GenerateAsync(
        PromptRequest request,
        CancellationToken cancellationToken);
}
```

---

# Document parser

```csharp
public interface IDocumentParser
{
    Task<string> ParseAsync(
        Stream stream,
        CancellationToken cancellationToken);
}
```

---

# OCR strategy

```csharp
public interface IPdfProcessingStrategy
{
    Task<string> ProcessAsync(
        Stream pdf,
        CancellationToken cancellationToken);
}
```

---

# Convenciones técnicas

## Obligatorias
- Async/await everywhere
- CancellationToken everywhere
- Minimal APIs
- Strong typing
- DTOs separados
- Configuración tipada
- Logging estructurado

---

# Logging y resiliencia

## Recomendados
- Serilog
- Polly
- OpenTelemetry

---

# Lo que NO se quiere

- lógica en controllers/endpoints
- acoplamiento a SDKs
- sobreabstracción
- CQRS innecesario
- repositories fake
- Domain Events innecesarios
- Event Sourcing
- arquitectura centrada en Semantic Kernel

---

# Estado actual del proyecto

## Fase actual

```text
FASE 0 — Definición técnica y arquitectura
```

---

# Objetivo de la Fase 0

Definir:
- estructura final
- dependencias
- contratos
- estándares
- estrategia operacional
- arquitectura definitiva

ANTES de comenzar implementación.

---

# Checklist maestro ya definido

Existe un roadmap completo de:
- Fase 0 → Fase 14

Incluyendo:
- arquitectura
- OCR
- chunking
- streaming
- integración Groq
- resiliencia
- testing
- Docker
- hardening
- release funcional final

El objetivo es terminar con un proyecto completamente funcional y consistente sin deuda técnica temprana.