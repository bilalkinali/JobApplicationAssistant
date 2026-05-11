using JobApplicationAssistant.Api.Domain;
using System.IO.Compression;
using System.Security;
using System.Text;

namespace JobApplicationAssistant.Api.Exports;

public static class CoverLetterDocxExporter
{
    public static byte[] Export(JobApplication application, Profile? profile)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(
                archive,
                "[Content_Types].xml",
                """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                  <Default Extension="xml" ContentType="application/xml"/>
                  <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
                </Types>
                """);
            AddEntry(
                archive,
                "_rels/.rels",
                """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
                </Relationships>
                """);
            AddEntry(archive, "word/document.xml", BuildDocumentXml(application, profile));
        }

        return stream.ToArray();
    }

    private static string BuildDocumentXml(JobApplication application, Profile? profile)
    {
        var paragraphs = new List<string>();

        if (profile is not null)
        {
            AddOptional(paragraphs, profile.FullName);
            AddOptional(paragraphs, profile.Email);
            AddOptional(paragraphs, profile.Phone);
            AddOptional(paragraphs, profile.Location);
            AddOptional(paragraphs, profile.LinkedInUrl);
            AddOptional(paragraphs, profile.GitHubUrl);
            AddOptional(paragraphs, profile.PortfolioUrl);
        }

        AddOptional(paragraphs, application.CompanyName);
        AddOptional(paragraphs, application.RoleTitle);

        foreach (var line in application.GeneratedDraft!.CoverLetterText.Replace("\r\n", "\n").Split('\n'))
        {
            AddOptional(paragraphs, line);
        }

        var body = string.Join(Environment.NewLine, paragraphs.Select(ToParagraphXml));
        return $$"""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
              <w:body>
            {{body}}
                <w:sectPr/>
              </w:body>
            </w:document>
            """;
    }

    private static void AddOptional(List<string> paragraphs, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            paragraphs.Add(value.Trim());
        }
    }

    private static string ToParagraphXml(string text) =>
        $"    <w:p><w:r><w:t>{SecurityElement.Escape(text)}</w:t></w:r></w:p>";

    private static void AddEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(content);
    }
}
