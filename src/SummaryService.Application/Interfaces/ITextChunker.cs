using SummaryService.Domain.ValueObjects;

namespace SummaryService.Application.Interfaces;

public interface ITextChunker
{
    IReadOnlyList<ChunkData> Chunk(string content, int chunkSize);
}