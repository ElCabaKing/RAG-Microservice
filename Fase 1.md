# FASE 1 — Scaffold de la Solución y Configuración Base

## Objetivo

Construir la base técnica del proyecto siguiendo la arquitectura definida en la Fase 0.

Al finalizar esta fase se debe tener:
- solución creada
- proyectos organizados
- referencias configuradas
- configuración centralizada
- logging funcional
- dependency injection configurada
- health checks funcionales
- estructura limpia y mantenible

Esta fase NO implementa lógica de negocio.

---

# Objetivos Técnicos

## La solución debe quedar:

- compilando correctamente
- con dependencias limpias
- lista para comenzar desarrollo real
- desacoplada
- extensible
- preparada para Docker/Linux
- preparada para streaming SSE

---

# Estructura Final Esperada

```text
src/

├── SummaryService.Api
├── SummaryService.Application
├── SummaryService.Domain
├── SummaryService.Infrastructure
└── SummaryService.Shared
```

---

# Paso 1 — Crear Solution

## Crear directorio raíz

```bash
mkdir SummaryService
cd SummaryService
```

---

## Crear carpeta src

```bash
mkdir src
```

---

## Crear solución

```bash
dotnet new sln -n SummaryService
```

---

# Paso 2 — Crear proyectos

## API

```bash
dotnet new web -n SummaryService.Api -o src/SummaryService.Api
```

---

## Application

```bash
dotnet new classlib -n SummaryService.Application -o src/SummaryService.Application
```

---

## Domain

```bash
dotnet new classlib -n SummaryService.Domain -o src/SummaryService.Domain
```

---

## Infrastructure

```bash
dotnet new classlib -n SummaryService.Infrastructure -o src/SummaryService.Infrastructure
```

---

## Shared

```bash
dotnet new classlib -n SummaryService.Shared -o src/SummaryService.Shared
```

---

# Paso 3 — Agregar proyectos a la solución

```bash
dotnet sln add src/SummaryService.Api
dotnet sln add src/SummaryService.Application
dotnet sln add src/SummaryService.Domain
dotnet sln add src/SummaryService.Infrastructure
dotnet sln add src/SummaryService.Shared
```

---

# Paso 4 — Configurar referencias entre proyectos

## Application

```bash
dotnet add src/SummaryService.Application reference src/SummaryService.Domain
dotnet add src/SummaryService.Application reference src/SummaryService.Shared
```

---

## Infrastructure

```bash
dotnet add src/SummaryService.Infrastructure reference src/SummaryService.Application
dotnet add src/SummaryService.Infrastructure reference src/SummaryService.Domain
dotnet add src/SummaryService.Infrastructure reference src/SummaryService.Shared
```

---

## API

```bash
dotnet add src/SummaryService.Api reference src/SummaryService.Application
dotnet add src/SummaryService.Api reference src/SummaryService.Infrastructure
dotnet add src/SummaryService.Api reference src/SummaryService.Shared
```

---

# Reglas de Dependencia

## Permitido

```text
Api → Application
Api → Infrastructure

Infrastructure → Application
Infrastructure → Domain

Application → Domain

Shared → cualquiera
```

---

## Prohibido

```text
Domain → Infrastructure
Domain → Api

Application → Infrastructure
```

---

# Paso 5 — Configurar .NET 9

## Validar SDK instalado

```bash
dotnet --list-sdks
```

---

## Validar Target Framework

Todos los proyectos deben usar:

```xml
<TargetFramework>net9.0</TargetFramework>
```

---

# Paso 6 — Instalar paquetes base

# API

## Serilog

```bash
dotnet add src/SummaryService.Api package Serilog.AspNetCore
dotnet add src/SummaryService.Api package Serilog.Sinks.Console
```

---

## Swagger

```bash
dotnet add src/SummaryService.Api package Swashbuckle.AspNetCore
```

---

## FluentValidation

```bash
dotnet add src/SummaryService.Api package FluentValidation.AspNetCore
```

---

## Health Checks

```bash
dotnet add src/SummaryService.Api package Microsoft.Extensions.Diagnostics.HealthChecks
```

---

# Infrastructure

## Semantic Kernel

```bash
dotnet add src/SummaryService.Infrastructure package Microsoft.SemanticKernel
```

---

## Polly

```bash
dotnet add src/SummaryService.Infrastructure package Polly
dotnet add src/SummaryService.Infrastructure package Microsoft.Extensions.Http.Polly
```

---

## PDF Parsing

```bash
dotnet add src/SummaryService.Infrastructure package UglyToad.PdfPig
```

---

## OCR

```bash
dotnet add src/SummaryService.Infrastructure package Tesseract
```

---

# Paso 7 — Eliminar archivos basura

Eliminar:
- `Class1.cs`
- archivos innecesarios

---

# Paso 8 — Crear estructura interna de carpetas

# SummaryService.Api

```text
Endpoints/
Middleware/
Extensions/
Configurations/
```

---

# SummaryService.Application

```text
Features/
Common/
Interfaces/
DTOs/
Validators/
Behaviors/
```

---

# SummaryService.Domain

```text
Enums/
Constants/
ValueObjects/
Exceptions/
```

---

# SummaryService.Infrastructure

```text
AI/
OCR/
Documents/
Streaming/
DependencyInjection/
Configurations/
```

---

# SummaryService.Shared

```text
Extensions/
Helpers/
Results/
```

---

# Paso 9 — Configuración base appsettings

## appsettings.json

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
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information"
    }
  }
}
```

---

# Paso 10 — Configuración strongly typed

## Crear clases Options

```text
Configurations/
 ├── GroqOptions.cs
 └── SummaryOptions.cs
```

---

# Reglas

- NO usar IConfiguration directamente en servicios
- usar IOptions<T>
- evitar strings mágicos

---

# Paso 11 — Configurar Serilog

## Requisitos

- logging estructurado
- request logging
- error logging
- correlation ids

---

# Logging mínimo esperado

Debe registrar:
- inicio request
- errores
- tiempo procesamiento
- warnings críticos

---

# Paso 12 — Configurar Dependency Injection

## Crear extensión

```text
Extensions/
 └── ServiceCollectionExtensions.cs
```

---

## Responsabilidades

Registrar:
- services
- parsers
- generators
- configurations
- http clients

---

# Paso 13 — Configurar Health Checks

## Endpoint requerido

```http
/health
```

---

# Resultado esperado

```json
{
  "status": "Healthy"
}
```

---

# Paso 14 — Configurar Swagger

## Requisitos

- Swagger UI funcional
- endpoint documentado
- preparado para SSE

---

# Paso 15 — Configurar Middleware Global de Excepciones

## Objetivo

Centralizar manejo de errores.

---

# Requisitos

- manejar errores inesperados
- evitar stack traces al cliente
- logging automático
- response consistente

---

# Paso 16 — Configurar User Secrets

## Objetivo

No exponer API keys.

---

# Inicializar

```bash
dotnet user-secrets init --project src/SummaryService.Api
```

---

# Agregar API key

```bash
dotnet user-secrets set "Groq:ApiKey" "YOUR_KEY" --project src/SummaryService.Api
```

---

# Paso 17 — Validar compilación

## Debe funcionar:

```bash
dotnet build
```

---

# Paso 18 — Validar ejecución

## Debe iniciar correctamente

```bash
dotnet run --project src/SummaryService.Api
```

---

# Endpoints mínimos funcionales

| Endpoint | Estado esperado |
|---|---|
| /health | OK |
| /swagger | OK |

---

# Reglas Técnicas Obligatorias

## Código

- Nullable enabled
- Implicit usings enabled
- Async/await obligatorio
- CancellationToken obligatorio
- No magic strings
- No lógica negocio en Api

---

## Arquitectura

- Infrastructure implementa
- Application abstrae
- Domain desacoplado
- Api orquesta HTTP

---

# Resultado Esperado al Final de la Fase

Al terminar esta fase se debe tener:

- solución limpia
- proyectos organizados
- referencias correctas
- configuración centralizada
- logging funcional
- health checks
- swagger
- dependency injection preparada
- compilación estable
- base lista para desarrollo real

---

# Criterios para Finalizar la Fase 1

La fase termina cuando:

- [ ] La solución compila
- [ ] Swagger funciona
- [ ] Health checks funcionan
- [ ] Serilog funciona
- [ ] User secrets configurados
- [ ] Arquitectura respetada
- [ ] Dependencias correctas
- [ ] No existen archivos basura
- [ ] La estructura interna está creada
- [ ] La solución está lista para implementación funcional