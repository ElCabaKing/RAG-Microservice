# FASE 3 — Pipeline de Documentos y OCR

## Objetivo

Implementar el pipeline completo de procesamiento documental capaz de:

- recibir PDFs
- extraer texto
- detectar PDFs escaneados
- ejecutar OCR automáticamente
- normalizar contenido
- devolver texto limpio listo para chunking

Esta fase construye el sistema de ingestión documental del microservicio.

---

# Objetivos Técnicos

Al finalizar esta fase se debe tener:

- parsing PDF funcional
- parsing TXT funcional
- OCR funcional
- estrategia inteligente PDF/OCR
- normalización textual
- manejo errores documentos
- soporte cancellation tokens
- pipeline desacoplado
- arquitectura limpia mantenida

---

# Resultado Esperado

El sistema debe poder:

```text
PDF/TXT
 ↓
Extraer texto
 ↓
Aplicar OCR si necesario
 ↓
Normalizar contenido
 ↓
Retornar texto limpio
```

SIN implementar todavía:
- chunking real
- Groq
- Semantic Kernel
- summaries
- streaming SSE completo

---

# Flujo Oficial del Pipeline

```text
Upload File
 ↓
Validate File
 ↓
Detect Document Type
 ↓
Try Text Extraction
 ↓
Text Sufficient?
 ├── YES → Normalize Text
 └── NO  → OCR Pipeline
               ↓
          Render Pages
               ↓
          OCR Per Page
               ↓
          Combine Text
               ↓
          Normalize Text
```

---

# Paso 1 — Crear estructura interna Documents

# Carpeta

```text
SummaryService.Infrastructure/Documents/
```

---

# Subestructura recomendada

```text
Documents/
 ├── Parsers/
 ├── Pdf/
 ├── Txt/
 ├── OCR/
 ├── Normalization/
 └── Strategies/
```

---

# Paso 2 — Implementar TXT Parser

## Objetivo

Soportar documentos TXT simples.

---

# Clase requerida

```text
TxtDocumentParser
```

---

# Responsabilidades

- leer stream
- validar contenido
- retornar texto limpio

---

# Requisitos

- async
- cancellation token
- UTF-8 safe
- stream safe

---

# Paso 3 — Implementar PDF Text Extractor

## Objetivo

Extraer texto nativo desde PDFs.

---

# Librería

### [PdfPig GitHub](https://github.com/UglyToad/PdfPig?utm_source=chatgpt.com)

---

# Clase requerida

```text
PdfTextExtractor
```

---

# Responsabilidades

- abrir PDF
- iterar páginas
- extraer texto
- concatenar contenido

---

# Requisitos

- async compatible
- manejo errores PDF corrupto
- soporte cancellation token
- logging
- protección memoria

---

# Reglas importantes

## NO hacer OCR todavía aquí

Este componente:
- SOLO extrae texto nativo

---

# Paso 4 — Crear estrategia detección OCR

## Objetivo

Determinar cuándo un PDF necesita OCR.

---

# Clase requerida

```text
PdfOcrDetectionStrategy
```

---

# Reglas iniciales sugeridas

## Considerar OCR si:

```text
text.Length < 500
```

o:
- demasiados caracteres vacíos
- texto ilegible
- contenido inválido

---

# IMPORTANTE

La estrategia debe ser configurable.

NO hardcodear thresholds.

---

# Paso 5 — Implementar render PDF → imagen

## Objetivo

Convertir páginas PDF en imágenes para OCR.

---

# Recomendación

### [PDFiumSharp GitHub](https://github.com/ArgusMagnus/PDFiumSharp?utm_source=chatgpt.com)

---

# Carpeta

```text
Documents/OCR/
```

---

# Clase requerida

```text
PdfPageRenderer
```

---

# Responsabilidades

- renderizar páginas
- convertir página → imagen
- manejar resolución
- liberar memoria correctamente

---

# Requisitos

- evitar memory leaks
- dispose correcto
- configurable DPI

---

# Paso 6 — Implementar OCR

## Objetivo

Extraer texto desde imágenes.

---

# Librería

### [Tesseract OCR](https://github.com/tesseract-ocr/tesseract?utm_source=chatgpt.com)

---

# Wrapper .NET

### [charlesw/tesseract GitHub](https://github.com/charlesw/tesseract?utm_source=chatgpt.com)

---

# Clase requerida

```text
PdfOcrExtractor
```

---

# Responsabilidades

- OCR por página
- concatenar texto
- manejar páginas vacías
- manejar timeouts

---

# Idiomas recomendados

## Iniciales

```text
spa
eng
```

---

# Requisitos

- cancellation token
- logging
- timeout
- control memoria

---

# Paso 7 — Implementar estrategia inteligente PDF

## Objetivo

Combinar extracción textual + OCR.

---

# Clase requerida

```text
SmartPdfProcessor
```

---

# Flujo esperado

```text
Try Text Extraction
 ↓
Enough Text?
 ├── YES → Return Text
 └── NO  → Run OCR
```

---

# Reglas

- minimizar OCR innecesario
- OCR como fallback
- logging decisiones

---

# Paso 8 — Implementar Document Parser Factory

## Objetivo

Resolver parser correcto según tipo documento.

---

# Clase requerida

```text
DocumentParserFactory
```

---

# Debe soportar

- PDF
- TXT

---

# Reglas

- extensible
- desacoplado
- no switch gigantes

---

# Paso 9 — Implementar Normalización Texto

## Objetivo

Limpiar texto antes del chunking.

---

# Carpeta

```text
Documents/Normalization/
```

---

# Clase requerida

```text
TextNormalizer
```

---

# Responsabilidades

- remover espacios excesivos
- normalizar saltos línea
- remover caracteres inválidos
- limpiar OCR artifacts

---

# Reglas importantes

## NO destruir semántica

Evitar:
- alterar párrafos
- destruir puntuación
- eliminar contexto útil

---

# Paso 10 — Implementar servicio orquestador documentos

## Objetivo

Centralizar procesamiento documental.

---

# Clase requerida

```text
DocumentProcessingService
```

---

# Responsabilidades

- detectar tipo documento
- resolver parser
- ejecutar extracción
- ejecutar OCR fallback
- normalizar texto
- retornar DocumentContent

---

# Flujo esperado

```text
File
 ↓
Detect Type
 ↓
Resolve Parser
 ↓
Extract Text
 ↓
Normalize
 ↓
Return Content
```

---

# Paso 11 — Implementar validaciones avanzadas

## Objetivo

Evitar inputs problemáticos.

---

# Validaciones requeridas

## Tamaño

```text
Max: 15MB
```

---

## Tipos permitidos

```text
application/pdf
text/plain
```

---

## PDFs

- páginas máximas
- PDFs corruptos
- PDFs protegidos

---

# Paso 12 — Configurar límites operativos OCR

## Objetivo

Proteger CPU y memoria.

---

# Configuraciones recomendadas

## OcrOptions

```csharp
public sealed class OcrOptions
{
    public int MaxPages { get; init; }

    public int Dpi { get; init; }

    public int TimeoutSeconds { get; init; }
}
```

---

# Recomendaciones iniciales

| Configuración | Valor |
|---|---|
| MaxPages | 100 |
| DPI | 300 |
| TimeoutSeconds | 60 |

---

# Paso 13 — Agregar logging estratégico

## Objetivo

Trazabilidad procesamiento documentos.

---

# Debe registrarse

- inicio procesamiento
- tipo documento
- OCR activado
- tiempo OCR
- páginas procesadas
- errores parsing
- PDFs corruptos

---

# Paso 14 — Manejo errores especializado

## Exceptions requeridas

- InvalidDocumentException
- OcrProcessingException
- UnsupportedDocumentException

---

# Reglas

- errores claros
- logs completos
- responses consistentes

---

# Paso 15 — Testing básico documentos

## Objetivo

Validar pipeline antes chunking.

---

# Debe probarse

## PDFs normales

- texto simple
- múltiples páginas

---

## PDFs OCR

- escaneados
- imágenes
- calidad baja

---

## TXT

- UTF-8
- multilinea

---

## Casos inválidos

- corruptos
- vacíos
- enormes

---

# Paso 16 — Verificar memoria y performance

## Objetivo

Evitar problemas tempranos.

---

# Debe verificarse

- dispose streams
- dispose imágenes
- dispose OCR resources
- evitar cargar PDFs gigantes completos en RAM

---

# Reglas Técnicas Obligatorias

## Código

- async obligatorio
- cancellation token obligatorio
- no lógica OCR en Api
- no lógica parsing en Domain
- no hardcoded thresholds

---

## Arquitectura

- Infrastructure implementa
- Application abstrae
- Domain desacoplado

---

# Resultado Esperado al Final de la Fase

Al terminar esta fase el sistema debe poder:

- recibir PDFs/TXT
- extraer texto correctamente
- detectar PDFs escaneados
- ejecutar OCR automáticamente
- normalizar contenido
- manejar errores correctamente
- retornar texto listo para chunking

SIN implementar todavía:
- IA
- chunking real
- summaries
- Groq
- Semantic Kernel
- streaming real

---

# Criterios para Finalizar la Fase 3

La fase termina cuando:

- [x] TXT parsing funciona
- [x] PDF parsing funciona
- [x] OCR funciona
- [x] OCR fallback funciona
- [x] PDFs escaneados funcionan
- [x] Normalización funciona
- [x] Exceptions funcionan
- [x] Logging funciona
- [x] Cancellation tokens funcionan
- [x] Arquitectura sigue limpia
- [x] La solución compila
- [x] El texto final queda listo para chunking

---

# FASE 3 — COMPLETADA ✅

**Fecha de Finalización:** 15 de mayo de 2026

**Compilación:** Exitosa sin errores

**Componentes Implementados:**

## Parsers y Extractores
- ✅ `TxtDocumentParser` — Lectura y validación de archivos TXT
- ✅ `PdfTextExtractor` — Extracción de texto nativo con PdfPig
- ✅ `PdfRenderer` — Renderización de páginas a imágenes (Docnet)
- ✅ `PdfOcrExtractor` — Extracción OCR con Tesseract

## Estrategias y Orquestación
- ✅ `PdfOcrDetectionStrategy` — Decisión inteligente de OCR
- ✅ `SmartPdfProcessor` — Procesamiento inteligente PDF
- ✅ `DocumentParserFactory` — Fábrica de parsers
- ✅ `DocumentProcessingService` — Orquestador principal

## Normalización
- ✅ `TextNormalizer` — Limpieza y normalización de texto

## Infraestructura
- ✅ Inyección de dependencias configurada
- ✅ Logging estratégico en todos los componentes
- ✅ Manejo de excepciones personalizado
- ✅ Cancellation tokens integrados

**Próximo paso:** Fase 4 — Chunking de Documentos