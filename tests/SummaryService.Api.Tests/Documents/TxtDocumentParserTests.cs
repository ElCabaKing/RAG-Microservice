using Microsoft.Extensions.Logging;
using Moq;
using SummaryService.Domain.Enums;
using SummaryService.Infrastructure.Documents.Txt;

namespace SummaryService.Api.Tests.Documents;

public class TxtDocumentParserTests
{
    private readonly Mock<ILogger<TxtDocumentParser>> _loggerMock;
    private readonly TxtDocumentParser _parser;

    public TxtDocumentParserTests()
    {
        _loggerMock = new Mock<ILogger<TxtDocumentParser>>();
        _parser = new TxtDocumentParser(_loggerMock.Object);
    }

    #region Valid Content Tests

    [Fact]
    public async Task ParseAsync_WithValidTextFile_ReturnsDocumentContent()
    {
        // Arrange
        const string content = "Este es un archivo de texto válido.";
        using var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        await writer.WriteAsync(content);
        await writer.FlushAsync();
        stream.Position = 0;

        // Act
        var result = await _parser.ParseAsync(stream, "test.txt", stream.Length, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DocumentType.Txt, result.Type);
        Assert.NotEmpty(result.Content);
        Assert.Contains(content, result.Content);
    }

    [Fact]
    public async Task ParseAsync_WithMultilineText_NormalizesCorrectly()
    {
        // Arrange
        const string content = "Primera línea\r\nSegunda línea\nTercera línea";
        using var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        await writer.WriteAsync(content);
        await writer.FlushAsync();
        stream.Position = 0;

        // Act
        var result = await _parser.ParseAsync(stream, "test.txt", stream.Length, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Primera línea", result.Content);
        Assert.Contains("Segunda línea", result.Content);
        Assert.Contains("Tercera línea", result.Content);
        // Verificar que los saltos de línea están normalizados (CRLF -> LF)
        Assert.DoesNotContain("\r\n", result.Content);
    }

    [Fact]
    public async Task ParseAsync_WithLeadingAndTrailingWhitespace_TrimsCorrectly()
    {
        // Arrange
        const string content = "   \n\n  Contenido con espacios  \n\n   ";
        using var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        await writer.WriteAsync(content);
        await writer.FlushAsync();
        stream.Position = 0;

        // Act
        var result = await _parser.ParseAsync(stream, "test.txt", stream.Length, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Content.StartsWith(" "));
        Assert.False(result.Content.EndsWith(" "));
        Assert.False(result.Content.StartsWith("\n"));
        Assert.False(result.Content.EndsWith("\n"));
    }

    [Fact]
    public async Task ParseAsync_WithMultipleConsecutiveEmptyLines_RemovesExtra()
    {
        // Arrange
        const string content = "Línea 1\n\n\n\nLínea 2\n\n\nLínea 3";
        using var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        await writer.WriteAsync(content);
        await writer.FlushAsync();
        stream.Position = 0;

        // Act
        var result = await _parser.ParseAsync(stream, "test.txt", stream.Length, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        // No debe haber más de una línea vacía consecutiva
        Assert.DoesNotContain("\n\n\n", result.Content);
    }

    [Fact]
    public async Task ParseAsync_WithSpecialCharacters_PreservesContent()
    {
        // Arrange
        const string content = "Contenido con caracteres especiales: áéíóú, ñ, €, 中文, 😀";
        using var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        await writer.WriteAsync(content);
        await writer.FlushAsync();
        stream.Position = 0;

        // Act
        var result = await _parser.ParseAsync(stream, "test.txt", stream.Length, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("áéíóú", result.Content);
        Assert.Contains("ñ", result.Content);
        Assert.Contains("€", result.Content);
    }

    [Fact]
    public async Task ParseAsync_SetsCorrectDocumentType()
    {
        // Arrange
        const string content = "Test content";
        using var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        await writer.WriteAsync(content);
        await writer.FlushAsync();
        stream.Position = 0;

        // Act
        var result = await _parser.ParseAsync(stream, "test.txt", stream.Length, CancellationToken.None);

        // Assert
        Assert.Equal(DocumentType.Txt, result.Type);
    }

    [Fact]
    public async Task ParseAsync_PreservesSizeInBytes()
    {
        // Arrange
        const string content = "Test content";
        using var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        await writer.WriteAsync(content);
        await writer.FlushAsync();
        stream.Position = 0;

        long sizeInBytes = stream.Length;

        // Act
        var result = await _parser.ParseAsync(stream, "test.txt", sizeInBytes, CancellationToken.None);

        // Assert
        Assert.Equal(sizeInBytes, result.SizeInBytes);
    }

    #endregion

    #region Invalid Content Tests

    [Fact]
    public async Task ParseAsync_WithEmptyFile_ThrowsInvalidOperationException()
    {
        // Arrange
        using var stream = new MemoryStream();
        stream.Position = 0;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _parser.ParseAsync(stream, "empty.txt", stream.Length, CancellationToken.None));
    }

    [Fact]
    public async Task ParseAsync_WithOnlyWhitespace_ThrowsInvalidOperationException()
    {
        // Arrange
        const string content = "   \n\n\t\t   ";
        using var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        await writer.WriteAsync(content);
        await writer.FlushAsync();
        stream.Position = 0;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _parser.ParseAsync(stream, "whitespace.txt", stream.Length, CancellationToken.None));
    }

    [Fact]
    public async Task ParseAsync_WithFileSizeExceedingLimit_ThrowsInvalidOperationException()
    {
        // Arrange
        using var stream = new MemoryStream();
        long oversizedBytes = (10 * 1024 * 1024) + 1; // 10 MB + 1 byte

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _parser.ParseAsync(stream, "huge.txt", oversizedBytes, CancellationToken.None));
    }

    [Fact]
    public async Task ParseAsync_WithNullStream_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _parser.ParseAsync(null!, "test.txt", 100, CancellationToken.None));
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task ParseAsync_WithCancellationToken_ThrowsOperationCanceledException()
    {
        // Arrange
        const string content = "Test content";
        using var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        await writer.WriteAsync(content);
        await writer.FlushAsync();
        stream.Position = 0;

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        // TaskCanceledException hereda de OperationCanceledException, ThrowsAnyAsync acepta subtipos
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _parser.ParseAsync(stream, "test.txt", stream.Length, cts.Token));
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public async Task ParseAsync_WithLargeFile_ProcessesSuccessfully()
    {
        // Arrange - Crear un archivo de 5 MB
        const int fileSizeBytes = 5 * 1024 * 1024;
        using var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        
        // Escribir contenido repetido hasta alcanzar el tamaño deseado
        string line = "Esta es una línea de contenido para el archivo de prueba.\n";
        int linesNeeded = fileSizeBytes / line.Length;
        for (int i = 0; i < linesNeeded; i++)
        {
            await writer.WriteAsync(line);
        }
        await writer.FlushAsync();
        stream.Position = 0;

        // Act
        var result = await _parser.ParseAsync(stream, "large.txt", stream.Length, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
        Assert.Equal(DocumentType.Txt, result.Type);
    }

    [Fact]
    public async Task ParseAsync_WithMixedLineEndings_NormalizesAll()
    {
        // Arrange - Mezcla de CRLF, LF y CR
        const string content = "Línea 1\r\nLínea 2\nLínea 3\rLínea 4";
        using var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        await writer.WriteAsync(content);
        await writer.FlushAsync();
        stream.Position = 0;

        // Act
        var result = await _parser.ParseAsync(stream, "mixed.txt", stream.Length, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        // Todos los saltos deben ser LF
        Assert.DoesNotContain("\r\n", result.Content);
        Assert.DoesNotContain("\r", result.Content);
        Assert.Contains("\n", result.Content);
    }

    [Fact]
    public async Task ParseAsync_WithStreamPositionNotAtZero_ResetsPosition()
    {
        // Arrange
        const string content = "Contenido para prueba";
        using var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        await writer.WriteAsync(content);
        await writer.FlushAsync();
        // Posicionar el stream en medio
        stream.Position = 5;

        // Act
        var result = await _parser.ParseAsync(stream, "test.txt", stream.Length, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(content, result.Content);
    }

    #endregion
}
