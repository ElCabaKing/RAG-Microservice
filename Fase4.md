# FASE 4 — Chunking, Control de Tokens y Preparación para LLM

## Objetivo

Implementar el sistema de:
- división inteligente de documentos
- estimación de tokens
- control de límites
- protección del contexto del modelo
- preparación del pipeline para IA

Esta fase transforma el texto limpio en unidades procesables por el LLM.

---

# Objetivos Técnicos

Al finalizar esta fase se debe tener:

- chunking funcional
- token estimation funcional
- límites configurables
- protección overflow contexto
- chunks coherentes
- pipeline preparado para Groq
- arquitectura extensible

---

# Resultado Esperado

El sistema debe poder:

```text
Document Text
 ↓
Normalize
 ↓
Estimate Tokens
 ↓
Split Into Chunks
 ↓
Return Ordered Chunks
```

SIN implementar todavía:
- Groq
- Semantic Kernel
- summaries reales
- streaming IA
- map-reduce final

---

# Problema que resuelve esta fase

Los LLMs tienen límites de contexto.

Por ejemplo:

```text
Documento gigante
 ↓
NO cabe completo en el modelo
```

Entonces necesitamos:

```text
Documento
 ↓
Chunks
 ↓
LLM por chunk
```

---

# Flujo Oficial

```text
Document Content
 ↓
Token Estimation
 ↓
Validate Limits
 ↓
Chunk Strategy
 ↓
Generate Chunks
 ↓
Validate Chunk Sizes
 ↓
Return Ordered Chunks
```

---

# Paso 1 — Crear estructura Chunking

# Carpeta

```text
SummaryService.Infrastructure/Chunking/
```

---

# Subestructura recomendada

```text
Chunking/
 ├── Strategies/
 ├── Estimation/
 ├── Validation/
 └── Services/
```

---

# Paso 2 — Implementar Token Estimator

## Objetivo

Estimar cantidad de tokens antes de enviar al LLM.

---

# Interface ya definida

```csharp
ITokenEstimator
```

---

# Clase requerida

```text
ApproximateTokenEstimator
```

---

# Estrategia inicial recomendada

## Aproximación simple

```text
1 token ≈ 4 caracteres
```

---

# IMPORTANTE

La estimación:
- NO necesita ser perfecta inicialmente
- debe ser consistente

---

# Responsabilidades

- estimar tokens input
- estimar tokens chunks
- validar límites modelo

---

# Métodos sugeridos

```csharp
Estimate(string text)

EstimateChunk(string chunk)
```

---

# Paso 3 — Configurar límites modelo

## Objetivo

Evitar overflow contexto LLM.

---

# Clase requerida

```text
ModelTokenLimits
```

---

# Ejemplo

```csharp
public sealed record ModelTokenLimits(
    int MaxContextTokens,
    int ReservedOutputTokens);
```

---

# Configuración inicial sugerida

| Configuración | Valor |
|---|---|
| MaxContextTokens | 8192 |
| ReservedOutputTokens | 2048 |

---

# Regla importante

## Siempre reservar output tokens

NO usar:
```text
100% contexto para input
```

---

# Paso 4 — Implementar Chunking Strategy

## Objetivo

Dividir documentos correctamente.

---

# Clase requerida

```text
ParagraphChunkingStrategy
```

---

# Reglas iniciales

## Chunking por:

- párrafos
- saltos línea
- bloques semánticos

---

# IMPORTANTE

NO chunkear:
- carácter por carácter
- palabras arbitrarias

---

# Objetivo principal

Preservar:
- contexto
- coherencia
- semántica

---

# Paso 5 — Implementar Chunk Generator

## Objetivo

Construir chunks válidos.

---

# Clase requerida

```text
TextChunker
```

---

# Responsabilidades

- dividir texto
- validar tamaño chunk
- mantener orden
- generar ChunkData

---

# Reglas importantes

## Cada chunk debe:

- ser coherente
- respetar límites tokens
- mantener orden original

---

# Resultado esperado

```text
Chunk 1
Chunk 2
Chunk 3
...
```

---

# Paso 6 — Implementar Chunk Validation

## Objetivo

Evitar chunks inválidos.

---

# Validaciones requeridas

## Validar:

- chunk vacío
- chunk demasiado grande
- demasiados chunks
- chunk corrupto

---

# Configuración sugerida

| Configuración | Valor |
|---|---|
| MaxChunkTokens | 6000 |
| MaxChunks | 100 |

---

# Paso 7 — Implementar Chunking Options

## Objetivo

Configuración centralizada.

---

# Clase requerida

```csharp
public sealed class ChunkingOptions
{
    public int MaxChunkTokens { get; init; }

    public int MaxChunks { get; init; }

    public int OverlapTokens { get; init; }
}
```

---

# Configuración sugerida

| Configuración | Valor |
|---|---|
| MaxChunkTokens | 6000 |
| MaxChunks | 100 |
| OverlapTokens | 300 |

---

# Paso 8 — Implementar Chunk Overlap

## Objetivo

Preservar continuidad entre chunks.

---

# Problema

Sin overlap:

```text
Chunk 1 termina idea
Chunk 2 pierde contexto
```

---

# Solución

Agregar:
- overlap parcial
- continuidad semántica

---

# Recomendación inicial

```text
5% - 10% overlap
```

---

# Paso 9 — Implementar Chunking Service

## Objetivo

Orquestar pipeline chunking.

---

# Clase requerida

```text
ChunkingService
```

---

# Responsabilidades

- estimar tokens
- validar límites
- generar chunks
- validar chunks
- retornar lista final

---

# Flujo esperado

```text
Text
 ↓
Estimate Tokens
 ↓
Apply Strategy
 ↓
Validate Chunks
 ↓
Return ChunkData[]
```

---

# Paso 10 — Implementar manejo límites documentos gigantes

## Objetivo

Proteger el sistema.

---

# Validaciones requeridas

## Rechazar si:

- demasiados chunks
- demasiados tokens
- documento gigantesco

---

# Exceptions requeridas

## ChunkingException

## TokenLimitExceededException

---

# Paso 11 — Logging estratégico chunking

## Objetivo

Trazabilidad pipeline IA.

---

# Debe registrarse

- tokens estimados
- chunks generados
- tamaño promedio chunks
- overlaps aplicados
- documentos rechazados

---

# Paso 12 — Preparar prompts por chunk

## Objetivo

Preparar integración IA futura.

---

# Carpeta

```text
Prompts/
```

---

# Archivo requerido

```text
summarize-chunk.txt
```

---

# Responsabilidad

Prompt especializado para:
- chunks individuales
- contexto parcial
- summaries parciales

---

# Paso 13 — Implementar pruebas chunking

## Objetivo

Validar coherencia.

---

# Debe probarse

## Casos normales

- documentos pequeños
- documentos medianos
- documentos grandes

---

## Casos edge

- párrafos enormes
- textos vacíos
- caracteres raros
- OCR imperfecto

---

# Validar especialmente

- orden chunks
- continuidad
- límites tokens
- overlaps

---

# Paso 14 — Performance y memoria

## Objetivo

Evitar explosiones memoria.

---

# Reglas importantes

## Evitar:

- duplicar strings gigantes
- listas innecesarias
- concatenaciones excesivas

---

# Preferir

- streaming interno
- StringBuilder
- lazy processing donde aporte valor

---

# Paso 15 — Validar preparación para LLM

## Objetivo

Dejar sistema listo para IA.

---

# Debe garantizarse

Cada chunk:
- cabe en contexto
- mantiene semántica
- es resumible
- tiene tamaño consistente

---

# Reglas Técnicas Obligatorias

## Código

- async obligatorio
- cancellation token obligatorio
- sin hardcoded limits
- sin chunking en Api
- sin lógica tokens en Domain

---

## Arquitectura

- Application abstrae
- Infrastructure implementa
- Domain desacoplado

---

# Resultado Esperado al Final de la Fase

Al terminar esta fase el sistema debe poder:

- tomar texto completo
- estimar tokens
- dividir correctamente
- aplicar overlap
- validar chunks
- proteger límites contexto
- generar chunks listos para IA

SIN implementar todavía:
- Groq
- Semantic Kernel
- summaries reales
- streaming IA
- map-reduce final

---

# Criterios para Finalizar la Fase 4

La fase termina cuando:

- [ ] Token estimation funciona
- [ ] Chunking funciona
- [ ] Overlap funciona
- [ ] Validaciones funcionan
- [ ] Límites funcionan
- [ ] Exceptions funcionan
- [ ] Logging funciona
- [ ] Chunks preservan coherencia
- [ ] Arquitectura sigue limpia
- [ ] La solución compila
- [ ] Los chunks están listos para IA