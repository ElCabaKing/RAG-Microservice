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

# Estado Actual

## Fases completadas
- ✅ Fase 1 — Completada y validada
- ✅ Fase 2 — Completada y validada
- ✅ Fase 3 — Completada y validada (15 de mayo de 2026)
- ⏳ Fase 4 — Por comenzar (Chunking)

## Estado técnico actual
- Solución renombrada a `SummaryService.*`.
- `Swagger` y `/health` funcionan.
- `Serilog` está activo con logging estratégico.
- `User Secrets` está inicializado en la API.
- El endpoint SSE responde con `status`, `chunk`, `completed` y `error` con contrato estructurado.
- Se implementaron enums, constants, value objects y exceptions de dominio.
- Se definieron DTOs de request/response para streaming.
- Se crearon validators base y contratos core de Application.
- Se creó estructura `Features/Summaries` para CQRS ligero.
- Se agregó result pattern (`Result` y `Result<T>`) y helpers base en Shared.
- Se agregó carpeta `Prompts/` con `summarize.txt` y `reduce.txt`.
- Se agregaron opciones strongly typed: `OcrOptions`, `ChunkingOptions`, `SummaryOptions`.

## Componentes de Fase 3 Implementados
- ✅ `TxtDocumentParser` — Lectura de archivos TXT
- ✅ `PdfTextExtractor` — Extracción nativa con PdfPig
- ✅ `PdfRenderer` — Renderización a imágenes (Docnet)
- ✅ `PdfOcrExtractor` — OCR con Tesseract
- ✅ `PdfOcrDetectionStrategy` — Decisión inteligente de OCR
- ✅ `SmartPdfProcessor` — Procesamiento inteligente de PDFs
- ✅ `DocumentParserFactory` — Fábrica de parsers
- ✅ `DocumentProcessingService` — Orquestador principal
- ✅ `TextNormalizer` — Normalización de texto

## Compilación
- ✅ Exitosa sin errores
- ✅ Todas las dependencias resueltas
- ✅ Arquitectura limpia mantenida

## Convención de secretos
- La API key de Groq debe guardarse como `Groq:ApiKey`.
- En desarrollo local se usa `User Secrets`.
- En despliegues se puede usar la variable de entorno `Groq__ApiKey`.

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
FASE 3 — Completada ✅
FASE 4 — En Preparación ⏳
```

---

# Progreso Actual

## Fases Completadas
- ✅ **Fase 0** — Arquitectura y estructura
- ✅ **Fase 1** — Configuración e infraestructura
- ✅ **Fase 2** — Enums, DTOs, validators, exceptions
- ✅ **Fase 3** — Pipeline de documentos y OCR
- ⏳ **Fase 4** — Chunking de documentos (próxima)

## Compilación
- ✅ Exitosa sin errores
- ✅ Todas las dependencias resueltas

## Estructura Actual
```
Domain → Application → Infrastructure → API
  (desacoplado)  (interfaces)  (implementación)  (endpoints)
```

**Documentación por Fase:**
- Detalles Fase 0: [Fase 0.md](Fase%200.md)
- Detalles Fase 1: [Fase 1.md](Fase%201.md)
- Detalles Fase 2: [Fase 2.md](Fase%202.md)
- Detalles Fase 3: [Fase 3.md](Fase%203.md) ✅
- Detalles Fase 4: [Fase 4.md](Fase%204.md) ⏳

**Progreso General:** [PROGRESS.md](PROGRESS.md)

---

# Objetivo de la Próxima Fase (Fase 4)

Implementar el pipeline de **segmentación inteligente** de documentos:

```text
Texto Normalizado
 ↓
ITextChunker (estrategias múltiples)
 ↓
Chunks con control de tokens
 ↓
Validación
 ↓
Listo para MAP phase
```

**Duración estimada:** 2-3 días de desarrollo

---

# Objetivo General del Proyecto

Terminar con un microservicio completamente funcional:
- recibir documentos PDF/TXT
- procesar y normalizar
- fragmentar inteligentemente
- generar resúmenes con LLM (Groq + Llama)
- hacer streaming en tiempo real
- arquitectura limpia y mantenible
- sin deuda técnica temprana

El objetivo es terminar con un proyecto completamente funcional y consistente sin deuda técnica temprana.