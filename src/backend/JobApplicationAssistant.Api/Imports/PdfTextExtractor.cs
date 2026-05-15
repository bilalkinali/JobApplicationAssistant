using System.Text;
using System.Text.RegularExpressions;

namespace JobApplicationAssistant.Api.Imports;

public sealed class PdfTextExtractor : IPdfTextExtractor
{
    public async Task<PdfTextExtractionResult> ExtractAsync(Stream pdfStream, CancellationToken ct)
    {
        byte[] bytes;
        try
        {
            using var memory = new MemoryStream();
            await pdfStream.CopyToAsync(memory, ct);
            bytes = memory.ToArray();
        }
        catch (Exception exception) when (exception is IOException or NotSupportedException)
        {
            return PdfTextExtractionResult.Failure("Uploaded PDF could not be read.");
        }

        if (bytes.Length == 0)
        {
            return PdfTextExtractionResult.Failure("Uploaded PDF was empty.");
        }

        if (!HasPdfHeader(bytes) || !HasPdfTrailer(bytes))
        {
            return PdfTextExtractionResult.Failure("Uploaded PDF was malformed.");
        }

        var pdf = Encoding.Latin1.GetString(bytes);
        if (Regex.IsMatch(pdf, @"/Encrypt\b", RegexOptions.IgnoreCase))
        {
            return PdfTextExtractionResult.Failure("Uploaded PDF is encrypted and cannot be imported.");
        }

        var extractedText = ExtractPdfText(pdf);
        if (string.IsNullOrWhiteSpace(extractedText))
        {
            return PdfTextExtractionResult.Failure("Uploaded PDF did not contain extractable CV text.");
        }

        return PdfTextExtractionResult.Success(extractedText);
    }

    private static bool HasPdfHeader(byte[] bytes) =>
        bytes.Length >= 5 && Encoding.ASCII.GetString(bytes, 0, 5) == "%PDF-";

    private static bool HasPdfTrailer(byte[] bytes)
    {
        var text = Encoding.Latin1.GetString(bytes);
        return text.Contains("%%EOF", StringComparison.Ordinal);
    }

    private static string ExtractPdfText(string pdf)
    {
        var values = Regex.Matches(pdf, @"\((?<text>(?:\\.|[^\\)])*)\)")
            .Select(match => DecodePdfLiteralString(match.Groups["text"].Value))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();

        return Regex.Replace(string.Join(' ', values), @"\s+", " ").Trim();
    }

    private static string DecodePdfLiteralString(string value) =>
        Regex.Replace(value, @"\\([nrtbf\\()])", match => match.Groups[1].Value switch
        {
            "n" => "\n",
            "r" => "\r",
            "t" => "\t",
            "b" => "\b",
            "f" => "\f",
            "\\" => "\\",
            "(" => "(",
            ")" => ")",
            _ => match.Value
        });
}
