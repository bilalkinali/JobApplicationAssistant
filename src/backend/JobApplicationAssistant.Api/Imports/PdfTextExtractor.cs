using System.Text;
using System.Text.RegularExpressions;
using System.IO.Compression;

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

        var extractedText = ExtractPdfText(pdf, bytes);
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

    private static string ExtractPdfText(string pdf, byte[] bytes)
    {
        var decodedStreamText = ExtractDecodedStreamText(pdf, bytes);
        if (!string.IsNullOrWhiteSpace(decodedStreamText))
        {
            return decodedStreamText;
        }

        var values = Regex.Matches(pdf, @"\((?<text>(?:\\.|[^\\)])*)\)")
            .Select(match => DecodePdfLiteralString(match.Groups["text"].Value))
            .Select(SanitizeExtractedText)
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

    private static string ExtractDecodedStreamText(string pdf, byte[] bytes)
    {
        var streams = ExtractStreams(pdf, bytes);
        var cmapByObjectId = streams
            .Where(stream => stream.Text.Contains("beginbfchar", StringComparison.Ordinal) ||
                stream.Text.Contains("beginbfrange", StringComparison.Ordinal))
            .ToDictionary(stream => stream.ObjectId, stream => ParseCMap(stream.Text));
        if (cmapByObjectId.Count == 0)
        {
            return string.Empty;
        }

        var toUnicodeByFontObjectId = Regex.Matches(pdf, @"(?<font>\d+)\s+0\s+obj(?<body>.*?)endobj", RegexOptions.Singleline)
            .Cast<Match>()
            .Select(match => new
            {
                FontObjectId = int.Parse(match.Groups["font"].Value),
                ToUnicodeObjectId = Regex.Match(match.Groups["body"].Value, @"/ToUnicode\s+(?<cmap>\d+)\s+0\s+R")
            })
            .Where(match => match.ToUnicodeObjectId.Success)
            .ToDictionary(
                match => match.FontObjectId,
                match => int.Parse(match.ToUnicodeObjectId.Groups["cmap"].Value));

        var fontResourceMap = Regex.Matches(pdf, @"/(?<name>F\d+)\s+(?<font>\d+)\s+0\s+R")
            .Cast<Match>()
            .Where(match => toUnicodeByFontObjectId.ContainsKey(int.Parse(match.Groups["font"].Value)))
            .GroupBy(match => match.Groups["name"].Value)
            .ToDictionary(
                group => group.Key,
                group => toUnicodeByFontObjectId[int.Parse(group.First().Groups["font"].Value)]);

        if (fontResourceMap.Count == 0)
        {
            return string.Empty;
        }

        var values = streams
            .Where(stream => stream.Text.Contains(" BT", StringComparison.Ordinal) ||
                stream.Text.Contains("BT", StringComparison.Ordinal))
            .SelectMany(stream => DecodeTextStream(stream.Text, fontResourceMap, cmapByObjectId))
            .Select(SanitizeExtractedText)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();

        return Regex.Replace(string.Join(' ', values), @"\s+", " ").Trim();
    }

    private static IReadOnlyList<PdfStream> ExtractStreams(string pdf, byte[] bytes)
    {
        var streams = new List<PdfStream>();
        foreach (Match match in Regex.Matches(pdf, @"(?<objectId>\d+)\s+0\s+obj(?<header>(?:(?!endobj).)*?)stream\r?\n", RegexOptions.Singleline))
        {
            var dataStart = Encoding.Latin1.GetByteCount(pdf[..match.Index]) + Encoding.Latin1.GetByteCount(match.Value);
            var marker = FindBytes(bytes, Encoding.ASCII.GetBytes("endstream"), dataStart);
            if (marker < 0)
            {
                continue;
            }

            var dataEnd = marker;
            while (dataEnd > dataStart && (bytes[dataEnd - 1] == '\n' || bytes[dataEnd - 1] == '\r'))
            {
                dataEnd--;
            }

            var data = bytes[dataStart..dataEnd];
            var text = match.Groups["header"].Value.Contains("/FlateDecode", StringComparison.Ordinal)
                ? TryInflate(data)
                : Encoding.Latin1.GetString(data);
            if (text is null)
            {
                continue;
            }

            streams.Add(new PdfStream(int.Parse(match.Groups["objectId"].Value), text));
        }

        return streams;
    }

    private static string? TryInflate(byte[] data)
    {
        try
        {
            using var input = new MemoryStream(data);
            using var zlib = new ZLibStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            zlib.CopyTo(output);
            return Encoding.Latin1.GetString(output.ToArray());
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    private static int FindBytes(byte[] source, byte[] pattern, int start)
    {
        for (var index = start; index <= source.Length - pattern.Length; index++)
        {
            var matched = true;
            for (var patternIndex = 0; patternIndex < pattern.Length; patternIndex++)
            {
                if (source[index + patternIndex] != pattern[patternIndex])
                {
                    matched = false;
                    break;
                }
            }

            if (matched)
            {
                return index;
            }
        }

        return -1;
    }

    private static Dictionary<string, string> ParseCMap(string cmap)
    {
        var values = Regex.Matches(cmap, @"beginbfchar(?<body>.*?)endbfchar", RegexOptions.Singleline)
            .Cast<Match>()
            .SelectMany(match => Regex.Matches(match.Groups["body"].Value, @"<(?<from>[0-9A-Fa-f]+)>\s+<(?<to>[0-9A-Fa-f]+)>").Cast<Match>())
            .ToDictionary(
                match => match.Groups["from"].Value.ToUpperInvariant(),
                match => DecodeUnicodeHex(match.Groups["to"].Value));

        foreach (var range in Regex.Matches(cmap, @"beginbfrange(?<body>.*?)endbfrange", RegexOptions.Singleline).Cast<Match>())
        {
            foreach (Match match in Regex.Matches(range.Groups["body"].Value, @"<(?<start>[0-9A-Fa-f]+)>\s+<(?<end>[0-9A-Fa-f]+)>\s+<(?<to>[0-9A-Fa-f]+)>"))
            {
                var start = Convert.ToInt32(match.Groups["start"].Value, 16);
                var end = Convert.ToInt32(match.Groups["end"].Value, 16);
                var destination = Convert.ToInt32(match.Groups["to"].Value, 16);
                var width = match.Groups["start"].Value.Length;
                for (var value = start; value <= end; value++)
                {
                    values[value.ToString($"X{width}")] = char.ConvertFromUtf32(destination + value - start);
                }
            }
        }

        return values;
    }

    private static IEnumerable<string> DecodeTextStream(
        string stream,
        IReadOnlyDictionary<string, int> fontResourceMap,
        IReadOnlyDictionary<int, Dictionary<string, string>> cmapByObjectId)
    {
        Dictionary<string, string>? currentCMap = null;
        var output = new StringBuilder();
        foreach (Match match in Regex.Matches(stream, @"/(?<font>F\d+)\s+[\d.]+\s+Tf|<(?<hex>[0-9A-Fa-f]+)>|(?<operator>TJ|Tj)"))
        {
            if (match.Groups["font"].Success)
            {
                if (fontResourceMap.TryGetValue(match.Groups["font"].Value, out var cmapObjectId))
                {
                    cmapByObjectId.TryGetValue(cmapObjectId, out currentCMap);
                }

                continue;
            }

            if (currentCMap is null)
            {
                continue;
            }

            if (match.Groups["operator"].Success)
            {
                output.Append(' ');
                continue;
            }

            var decoded = DecodePdfHexString(match.Groups["hex"].Value, currentCMap);
            if (!string.IsNullOrWhiteSpace(decoded))
            {
                output.Append(decoded);
            }
        }

        if (output.Length > 0)
        {
            yield return output.ToString();
        }
    }

    private static string DecodePdfHexString(string hex, IReadOnlyDictionary<string, string> cmap)
    {
        var output = new StringBuilder();
        var codeWidth = cmap.Keys.Select(key => key.Length).DefaultIfEmpty(2).Max();
        for (var index = 0; index + codeWidth <= hex.Length; index += codeWidth)
        {
            var code = hex.Substring(index, codeWidth).ToUpperInvariant();
            output.Append(cmap.TryGetValue(code, out var value) ? value : " ");
        }

        return output.ToString();
    }

    private static string DecodeUnicodeHex(string hex)
    {
        var output = new StringBuilder();
        for (var index = 0; index + 4 <= hex.Length; index += 4)
        {
            output.Append(char.ConvertFromUtf32(Convert.ToInt32(hex.Substring(index, 4), 16)));
        }

        return output.ToString();
    }

    private static string SanitizeExtractedText(string value) =>
        new(value.Where(character =>
            character is '\r' or '\n' or '\t' ||
            (!char.IsControl(character) && character != '\0')).ToArray());

    private sealed record PdfStream(int ObjectId, string Text);
}
