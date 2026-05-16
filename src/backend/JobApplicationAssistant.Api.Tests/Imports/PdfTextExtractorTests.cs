using System.Text;
using JobApplicationAssistant.Api.Imports;
using Xunit;

namespace JobApplicationAssistant.Api.Tests.Imports;

public sealed class PdfTextExtractorTests
{
    [Fact]
    public async Task ExtractAsync_returns_text_from_pdf_literal_strings()
    {
        var extractor = new PdfTextExtractor();
        using var stream = new MemoryStream(BuildPdf("Built .NET React APIs."));

        var result = await extractor.ExtractAsync(stream, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Built .NET React APIs.", result.Text);
    }

    [Fact]
    public async Task ExtractAsync_returns_text_from_compressed_tounicode_pdf_fixture()
    {
        var extractor = new PdfTextExtractor();
        await using var stream = File.OpenRead(FindRepoFile("docs", "testing", "fake CV.pdf"));

        var result = await extractor.ExtractAsync(stream, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Text);
        var text = result.Text;
        Assert.Contains("Phone:", text);
        Assert.Contains("mail", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain('\0', text);
        Assert.DoesNotContain("endstream", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExtractAsync_returns_page_text_from_cv_with_object_streams()
    {
        var extractor = new PdfTextExtractor();
        await using var stream = File.OpenRead(FindRepoFile("docs", "testing", "CV-en.pdf"));

        var result = await extractor.ExtractAsync(stream, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Text);
        var text = result.Text;
        Assert.Contains("Bilal Kinali", text);
        Assert.Contains("Software Developer", text);
        Assert.Contains("C#", text);
        Assert.DoesNotContain("endstream", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/Type /Catalog", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExtractAsync_reports_unreadable_pdf()
    {
        var extractor = new PdfTextExtractor();
        await using var stream = new ThrowingReadStream();

        var result = await extractor.ExtractAsync(stream, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("Uploaded PDF could not be read.", result.Error);
    }

    [Fact]
    public async Task ExtractAsync_reports_empty_pdf()
    {
        var extractor = new PdfTextExtractor();
        using var stream = new MemoryStream();

        var result = await extractor.ExtractAsync(stream, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("Uploaded PDF was empty.", result.Error);
    }

    [Fact]
    public async Task ExtractAsync_reports_encrypted_pdf()
    {
        var extractor = new PdfTextExtractor();
        using var stream = new MemoryStream(BuildPdf("secret", "<< /Encrypt 3 0 R >>"));

        var result = await extractor.ExtractAsync(stream, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("Uploaded PDF is encrypted and cannot be imported.", result.Error);
    }

    [Fact]
    public async Task ExtractAsync_reports_malformed_pdf()
    {
        var extractor = new PdfTextExtractor();
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes("%PDF-1.4\nnot complete"));

        var result = await extractor.ExtractAsync(stream, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("Uploaded PDF was malformed.", result.Error);
    }

    [Fact]
    public async Task ExtractAsync_reports_pdf_without_extractable_text()
    {
        var extractor = new PdfTextExtractor();
        using var stream = new MemoryStream(BuildPdf(""));

        var result = await extractor.ExtractAsync(stream, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("Uploaded PDF did not contain extractable CV text.", result.Error);
    }

    private static byte[] BuildPdf(string text, string trailer = "") =>
        Encoding.ASCII.GetBytes($"""
        %PDF-1.4
        1 0 obj
        << /Type /Page /Contents 2 0 R >>
        endobj
        2 0 obj
        << /Length 80 >>
        stream
        BT
        /F1 12 Tf
        72 720 Td
        ({text}) Tj
        ET
        endstream
        endobj
        {trailer}
        %%EOF
        """);

    private static string FindRepoFile(params string[] relativePathParts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var path = Path.Combine([directory.FullName, .. relativePathParts]);
            if (File.Exists(path))
            {
                return path;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not locate repository fixture.", Path.Combine(relativePathParts));
    }

    private sealed class ThrowingReadStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) =>
            throw new IOException("Cannot read stream.");

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            throw new IOException("Cannot read stream.");

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        public override void SetLength(long value) =>
            throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();
    }
}
