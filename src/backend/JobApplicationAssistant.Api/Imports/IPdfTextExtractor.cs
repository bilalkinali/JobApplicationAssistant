namespace JobApplicationAssistant.Api.Imports;

public interface IPdfTextExtractor
{
    Task<PdfTextExtractionResult> ExtractAsync(Stream pdfStream, CancellationToken ct);
}

public sealed record PdfTextExtractionResult(
    bool Succeeded,
    string? Text,
    string? Error)
{
    public static PdfTextExtractionResult Success(string text) =>
        new(true, text, null);

    public static PdfTextExtractionResult Failure(string error) =>
        new(false, null, error);
}
