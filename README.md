# RAG Microservice — Phase 0

This workspace contains a minimal scaffold implementing the artifacts required by Fase 0:

- Domain enum `ProcessingState`.
- Application DTO `SummaryRequestDto` and interfaces (`ISummaryGenerator`, `IPdfTextExtractor`).
- Infrastructure placeholder implementations for `ISummaryGenerator` and `IPdfTextExtractor`.
- API minimal endpoint: POST `/api/v1/summaries/stream` that streams SSE events (placeholder implementation).

Purpose: satisfy Phase 0 by defining contracts, project layout, and a working SSE endpoint skeleton. Real implementations (PdfPig, Tesseract, Groq/Llama integrations, Semantic Kernel) should be added in subsequent phases.

How to build:

```bash
dotnet build
```

How to run the API (development):

```bash
cd src/Rag.Api
dotnet run
```

The SSE endpoint accepts `multipart/form-data` with `file`, `style`, and `maxTokens` fields.
