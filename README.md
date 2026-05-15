# RAG Microservice — Resumen de Documentos con LLMs

Microservicio REST en .NET 9 para generar resúmenes automáticos de documentos PDF y TXT usando LLMs (Groq + Llama).

## Estado del Proyecto

| Fase | Estado | Descripción |
|------|--------|-------------|
| Fase 0 | ✅ Completada | Arquitectura y estructura base |
| Fase 1 | ✅ Completada | Configuración e infraestructura |
| Fase 2 | ✅ Completada | Enums, DTOs, validators, exceptions |
| Fase 3 | ✅ Completada | Pipeline de documentos y OCR |
| Fase 4 | ⏳ En curso | Chunking de documentos |

**Última actualización:** 15 de mayo de 2026

## Características Implementadas

### 🏗️ Arquitectura
- Clean Architecture (Domain → Application → Infrastructure → API)
- Inyección de dependencias centralizada
- Logging estratégico con Serilog
- Manejo de errores estructurado

### 📄 Procesamiento de Documentos
- **Soporte de formatos:**
  - PDF (texto nativo + OCR automático)
  - TXT (UTF-8)

- **Extracción de texto:**
  - PDFs textuales con PdfPig
  - OCR automático con Tesseract (español + inglés)
  - Detección inteligente de necesidad OCR

- **Normalización:**
  - Limpieza de espacios excesivos
  - Eliminación de artefactos OCR
  - Preservación de semántica

### 🔧 Componentes Core

#### Documentos
- `DocumentProcessingService` — Orquestador principal
- `DocumentParserFactory` — Fábrica de parsers
- `SmartPdfProcessor` — Procesamiento inteligente PDF
- `TxtDocumentParser` — Parser para TXT
- `TextNormalizer` — Normalización de contenido
- `PdfOcrDetectionStrategy` — Decisión de OCR

#### Infraestructura OCR/PDF
- `PdfRenderer` — Renderización de páginas
- `PdfTextExtractor` — Extracción de texto
- `PdfOcrExtractor` — Extracción OCR

### 🌐 API
- Endpoint SSE: `POST /api/v1/summaries/stream`
- Health check: `GET /health`
- Swagger/OpenAPI: `/swagger`

### 📋 Contrato SSE
```json
event: status
data: {"status": "extracting_text"}

event: chunk  
data: {"content": "..."}

event: completed
data: {}

event: error
data: {"error": "..."}
```

## Instalación y Ejecución

### Requisitos
- .NET 9 SDK
- Tesseract OCR (datos de idiomas)

### Build
```bash
dotnet build
```

### Ejecución
```bash
cd src/SummaryService.Api
dotnet run
```

La API estará disponible en `https://localhost:7160` (o el puerto configurado).

### Variables de Entorno
```bash
# API key de Groq (obligatoria para Fase 4+)
export Groq__ApiKey=your-key-here

# OCR Options
export Ocr__Language=eng+spa
export Ocr__Dpi=300
```

## Estructura de Carpetas

```
src/
├── SummaryService.Api           # Minimal APIs
├── SummaryService.Application   # DTOs, validators, interfaces
├── SummaryService.Domain        # Enums, exceptions, value objects
├── SummaryService.Infrastructure # Implementaciones, parsers, OCR
└── SummaryService.Shared        # Helpers, result pattern

Prompts/
├── summarize.txt               # Prompt para resumen
└── reduce.txt                 # Prompt para consolidación
```

## Stack Tecnológico

| Componente | Librería |
|-----------|----------|
| Framework | ASP.NET Core 9 |
| LLM | Groq (Llama) |
| Orquestación | Semantic Kernel |
| Extracción PDF | UglyToad.PdfPig |
| Renderización PDF | Docnet.Core |
| OCR | Tesseract |
| Logging | Serilog |
| Resiliencia | Polly |
| Validación | FluentValidation |

## Próximos Pasos (Fase 4)

- [ ] Implementar ITextChunker
- [ ] Integrar Semantic Kernel
- [ ] Conectar con API de Groq
- [ ] Streaming SSE completo
- [ ] Tests unitarios e integración

## Notas de Desarrollo

### Logging
El logging estratégico está activo en todos los componentes:
- INFO: eventos principales
- DEBUG: detalles de procesamiento
- ERROR: fallos y excepciones

Ver `appsettings.json` para configuración de Serilog.

### Configuración
Las opciones están strongly-typed:
- `OcrOptions` — Parámetros OCR
- `ChunkingOptions` — Configuración de chunking
- `SummaryOptions` — Opciones de resumen

### Excepciones Personalizadas
- `InvalidDocumentException` — Documento inválido
- `UnsupportedDocumentException` — Tipo no soportado
- `OcrProcessingException` — Error en OCR
- `ChunkingException` — Error en chunking
- `SummaryGenerationException` — Error en generación

## Contacto

Para más información, consultar `Context.md` y documentación de fases.

