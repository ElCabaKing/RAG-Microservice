# FASE 2 — Contratos, Dominio y Estructuras Base

## Objetivo

Definir correctamente:
- contratos del sistema
- objetos core
- enums
- constantes
- DTOs
- validaciones
- value objects
- respuestas SSE

Esta fase construye el núcleo conceptual del microservicio antes de implementar OCR, parsing o integración LLM.

---

# Objetivos Técnicos

Al finalizar esta fase se debe tener:

- contratos estables
- DTOs definidos
- enums centralizados
- constants organizadas
- validaciones base
- value objects funcionales
- respuestas SSE estructuradas
- cero magic strings
- base sólida para Application e Infrastructure

---

# Resultado Esperado

El sistema debe tener definidos:
- requests
- responses
- eventos streaming
- límites
- contratos internos
- objetos compartidos

SIN implementar aún:
- OCR
- Semantic Kernel
- Groq
- parsing real
- chunking real

---

# Paso 1 — Definir Enums

## Objetivo

Eliminar strings mágicos y centralizar estados/tipos.

---

# Carpeta

```text
SummaryService.Domain/Enums/
```

---

# Enums requeridos

## DocumentType

```csharp
public enum DocumentType
{
    Pdf = 1,
    Txt = 2
}
```

---

## SummaryStyle

```csharp
public enum SummaryStyle
{
    General = 1,
    Executive = 2,
    Technical = 3,
    BulletPoints = 4
}
```

---

## StreamEventType

```csharp
public enum StreamEventType
{
    Status = 1,
    Chunk = 2,
    Completed = 3,
    Error = 4
}
```

---

## ProcessingStatus

```csharp
public enum ProcessingStatus
{
    ExtractingText = 1,
    RunningOcr = 2,
    ChunkingDocument = 3,
    GeneratingSummary = 4,
    ReducingSummary = 5,
    Completed = 6,
    Failed = 7
}
```

---

# Paso 2 — Crear Constants

## Objetivo

Centralizar valores repetitivos y evitar magic strings.

---

# Carpeta

```text
SummaryService.Domain/Constants/
```

---

# Archivos requeridos

## MimeTypes

```csharp
public static class MimeTypes
{
    public const string Pdf = "application/pdf";
    public const string TextPlain = "text/plain";
}
```

---

## SseEvents

```csharp
public static class SseEvents
{
    public const string Status = "status";
    public const string Chunk = "chunk";
    public const string Completed = "completed";
    public const string Error = "error";
}
```

---

## GroqModels

```csharp
public static class GroqModels
{
    public const string Llama33_70B =
        "llama-3.3-70b-versatile";
}
```

---

## PromptNames

```csharp
public static class PromptNames
{
    public const string Summarize = "summarize";
    public const string Reduce = "reduce";
}
```

---

# Paso 3 — Crear Value Objects

## Objetivo

Representar configuraciones y estructuras core correctamente.

---

# Carpeta

```text
SummaryService.Domain/ValueObjects/
```

---

# SummaryOptions

```csharp
public sealed record SummaryOptions(
    int MaxTokens,
    double Temperature,
    SummaryStyle Style);
```

---

# DocumentContent

```csharp
public sealed record DocumentContent(
    string Content,
    DocumentType Type,
    long SizeInBytes);
```

---

# ChunkData

```csharp
public sealed record ChunkData(
    int Index,
    string Content);
```

---

# TokenLimits

```csharp
public sealed record TokenLimits(
    int MaxInputTokens,
    int MaxOutputTokens);
```

---

# Paso 4 — Crear DTOs

## Objetivo

Separar contratos HTTP del dominio.

---

# Carpeta

```text
SummaryService.Application/DTOs/
```

---

# Request DTOs

## SummaryRequestDto

```csharp
public sealed class SummaryRequestDto
{
    public IFormFile File { get; init; } = default!;

    public SummaryStyle Style { get; init; }

    public int MaxTokens { get; init; }
}
```

---

# Response DTOs

## StreamResponseDto

```csharp
public sealed class StreamResponseDto
{
    public string Event { get; init; } = string.Empty;

    public object Data { get; init; } = default!;
}
```

---

## SummaryChunkResponseDto

```csharp
public sealed class SummaryChunkResponseDto
{
    public string Content { get; init; } = string.Empty;
}
```

---

## ErrorResponseDto

```csharp
public sealed class ErrorResponseDto
{
    public string Message { get; init; } = string.Empty;
}
```

---

## StatusResponseDto

```csharp
public sealed class StatusResponseDto
{
    public string Status { get; init; } = string.Empty;
}
```

---

# Paso 5 — Crear Validators

## Objetivo

Validar requests antes del pipeline.

---

# Carpeta

```text
SummaryService.Application/Validators/
```

---

# SummaryRequestValidator

## Validaciones requeridas

- archivo obligatorio
- tamaño máximo 15MB
- mime type válido
- max tokens válido
- style válido

---

# Reglas mínimas

## File size

```text
Max: 15MB
```

---

## Mime types permitidos

```text
application/pdf
text/plain
```

---

## Max tokens

```text
Debe ser > 0
```

---

# Paso 6 — Crear Exceptions

## Objetivo

Centralizar errores del dominio y aplicación.

---

# Carpeta

```text
SummaryService.Domain/Exceptions/
```

---

# Exceptions requeridas

## InvalidDocumentException

## UnsupportedDocumentException

## OcrProcessingException

## SummaryGenerationException

## ChunkingException

---

# Reglas

- exceptions específicas
- mensajes claros
- NO usar Exception genérica

---

# Paso 7 — Crear Interfaces Core

## Objetivo

Definir contratos principales del sistema.

---

# Carpeta

```text
SummaryService.Application/Interfaces/
```

---

# Interfaces requeridas

## Document Processing

```csharp
IDocumentParser
IPdfTextExtractor
IPdfOcrExtractor
```

---

## AI

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

## Streaming

```csharp
ISseStreamWriter
```

---

# Reglas importantes

## Interfaces deben:

- representar capacidades reales
- ser cohesivas
- evitar métodos innecesarios
- evitar interfaces gigantes

---

# Paso 8 — Crear estructura Features

## Objetivo

Preparar arquitectura Application.

---

# Carpeta

```text
Features/
 └── Summaries/
```

---

# Subestructura

```text
Features/
 └── Summaries/
      ├── Commands/
      ├── Handlers/
      ├── DTOs/
      ├── Validators/
      └── Services/
```

---

# Resultado esperado

Arquitectura lista para:
- CQRS ligero
- casos de uso
- orquestación limpia

---

# Paso 9 — Crear Result Pattern

## Objetivo

Evitar abuso de excepciones para flujos controlados.

---

# Carpeta

```text
SummaryService.Shared/Results/
```

---

# Clases requeridas

## Result

## Result<T>

---

# Deben soportar

- success
- failure
- mensajes
- errores tipados

---

# Paso 10 — Crear Helpers base

## Objetivo

Centralizar utilidades reutilizables.

---

# Carpeta

```text
SummaryService.Shared/Helpers/
```

---

# Helpers recomendados

## FileHelper

## StreamHelper

## TokenHelper

---

# Paso 11 — Configurar Nullable y Code Style

## Objetivo

Mantener calidad desde el inicio.

---

# Reglas obligatorias

## Todos los proyectos

```xml
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>
```

---

# Reglas

- evitar null-forgiving innecesario
- evitar warnings ignorados
- usar records donde aporte valor

---

# Paso 12 — Definir contratos SSE oficiales

## Objetivo

Estandarizar streaming.

---

# Formato oficial

## Status

```text
event: status
data: { "status": "extracting_text" }
```

---

## Chunk

```text
event: chunk
data: { "content": "..." }
```

---

## Completed

```text
event: completed
data: {}
```

---

## Error

```text
event: error
data: { "message": "..." }
```

---

# Paso 13 — Preparar estructura Prompts

## Objetivo

Evitar prompts inline.

---

# Carpeta

```text
Prompts/
```

---

# Archivos iniciales

```text
summarize.txt
reduce.txt
```

---

# Reglas

- prompts versionables
- prompts externos
- NO hardcoded prompts

---

# Paso 14 — Crear configuraciones strongly typed

## Objetivo

Evitar IConfiguration disperso.

---

# Configurations

## GroqOptions

## SummaryOptions

## OcrOptions

## ChunkingOptions

---

# Paso 15 — Validar arquitectura

## Verificaciones

- Domain no referencia Infrastructure
- Application no referencia Infrastructure
- DTOs separados del dominio
- interfaces en Application
- implementations aún NO creadas

---

# Resultado Esperado al Final de la Fase

Al terminar esta fase se debe tener:

- enums definidos
- constants centralizadas
- DTOs listos
- value objects listos
- validators listos
- exceptions definidas
- interfaces core definidas
- SSE contracts definidos
- prompts organizados
- estructura features preparada

SIN implementar todavía:
- OCR
- parsing
- streaming real
- Groq
- Semantic Kernel
- chunking real

---

# Criterios para Finalizar la Fase 2

La fase termina cuando:

- [ ] No existen magic strings críticos
- [ ] DTOs están definidos
- [ ] Validators funcionan
- [ ] Exceptions están definidas
- [ ] Interfaces core están definidas
- [ ] SSE contracts están definidos
- [ ] Value objects están listos
- [ ] Prompts folder existe
- [ ] Arquitectura sigue limpia
- [ ] La solución sigue compilando