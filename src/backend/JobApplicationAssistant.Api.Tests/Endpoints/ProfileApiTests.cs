using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using JobApplicationAssistant.Api.Contracts;
using JobApplicationAssistant.Api.Data;
using JobApplicationAssistant.Api.Domain;
using JobApplicationAssistant.Api.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JobApplicationAssistant.Api.Tests.Endpoints;

public sealed class ProfileApiTests
{
    [Fact]
    public async Task PutProfile_rejects_missing_required_fields_and_invalid_urls()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var request = new ProfileRequest(
            "",
            "invalid-email",
            null,
            null,
            "linkedin",
            "github",
            "portfolio",
            "",
            null,
            null);

        var response = await client.PutAsJsonAsync("/api/profile", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Contains(nameof(ProfileRequest.FullName), error.Details.Keys);
        Assert.Contains(nameof(ProfileRequest.Email), error.Details.Keys);
        Assert.Contains(nameof(ProfileRequest.DefaultLanguage), error.Details.Keys);
        Assert.Contains(nameof(ProfileRequest.LinkedInUrl), error.Details.Keys);
        Assert.Contains(nameof(ProfileRequest.GitHubUrl), error.Details.Keys);
        Assert.Contains(nameof(ProfileRequest.PortfolioUrl), error.Details.Keys);
    }

    [Fact]
    public async Task PutProfile_saves_minimum_valid_profile()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var request = new ProfileRequest(
            "Ada Lovelace",
            "ada@example.com",
            null,
            null,
            null,
            null,
            null,
            "English",
            null,
            null);

        var response = await client.PutAsJsonAsync("/api/profile", request);

        response.EnsureSuccessStatusCode();
        var profile = await response.Content.ReadFromJsonAsync<ProfileResponse>();
        Assert.NotNull(profile);
        Assert.Equal("Ada Lovelace", profile.FullName);
        Assert.Equal("ada@example.com", profile.Email);
        Assert.Equal("English", profile.DefaultLanguage);
    }

    [Fact]
    public async Task PostProfileFact_rejects_missing_required_fields_status_and_non_array_json()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var request = new ProfileFactRequest(
            "",
            "",
            "",
            "Published",
            "{}",
            "not-json",
            """["valid"]""",
            """{"claim":"no"}""");

        var response = await client.PostAsJsonAsync("/api/profile/facts", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Contains(nameof(ProfileFactRequest.Type), error.Details.Keys);
        Assert.Contains(nameof(ProfileFactRequest.Title), error.Details.Keys);
        Assert.Contains(nameof(ProfileFactRequest.Summary), error.Details.Keys);
        Assert.Contains(nameof(ProfileFactRequest.Status), error.Details.Keys);
        Assert.Contains(nameof(ProfileFactRequest.FactItems), error.Details.Keys);
        Assert.Contains(nameof(ProfileFactRequest.Technologies), error.Details.Keys);
        Assert.DoesNotContain(nameof(ProfileFactRequest.AllowedClaims), error.Details.Keys);
        Assert.Contains(nameof(ProfileFactRequest.ForbiddenClaims), error.Details.Keys);
    }

    [Fact]
    public async Task PostProfileFact_saves_minimum_valid_profile_fact()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var request = new ProfileFactRequest(
            "Project",
            "Portfolio assistant",
            "Built workflow software.",
            "Approved",
            null,
            """[".NET", "React"]""",
            """["Built APIs"]""",
            null);

        var response = await client.PostAsJsonAsync("/api/profile/facts", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var fact = await response.Content.ReadFromJsonAsync<ProfileFactResponse>();
        Assert.NotNull(fact);
        Assert.Equal("Project", fact.Type);
        Assert.Equal("Portfolio assistant", fact.Title);
        Assert.Equal("Approved", fact.Status);
        Assert.Equal("[]", fact.FactItems);
        Assert.Equal("""[".NET", "React"]""", fact.Technologies);
    }

    [Fact]
    public async Task PostPdfCvImport_rejects_non_pdf_upload()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        using var form = new MultipartFormDataContent();
        using var file = new StringContent("not a pdf");
        file.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        form.Add(file, "file", "cv.txt");

        var response = await client.PostAsync("/api/profile/imports/pdf-cv", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Contains("file", error.Details!.Keys);
    }

    [Fact]
    public async Task PostPdfCvImport_creates_draft_imported_facts_that_do_not_support_matching()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        using var form = new MultipartFormDataContent();
        using var file = new ByteArrayContent(BuildPdf("Built .NET React APIs for internal workflow automation."));
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(file, "file", "cv.pdf");

        var importResponse = await client.PostAsync("/api/profile/imports/pdf-cv", form);

        Assert.Equal(HttpStatusCode.Created, importResponse.StatusCode);
        var import = await importResponse.Content.ReadFromJsonAsync<AssistedProfileImportResponse>();
        Assert.NotNull(import);
        Assert.Equal("cv.pdf", import.FileName);
        var importedFact = Assert.Single(import.ProfileFacts);
        Assert.Equal("Draft", importedFact.Status);
        Assert.False(importedFact.ManuallyEdited);
        Assert.Contains(".NET", importedFact.Technologies);
        Assert.Contains(import.ImportSessionId.ToString("N"), importedFact.SourceDocumentIds);
        Assert.Contains("sourceContext", importedFact.OriginalImportedSnapshot);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var fact = Assert.Single(db.ProfileFacts);
            Assert.Equal(ProfileFactStatus.Draft, fact.Status);
            Assert.False(fact.ManuallyEdited);
        }

        var applicationResponse = await client.PostAsJsonAsync(
            "/api/applications",
            new ApplicationRequest(
                "ExampleCo",
                ".NET Developer",
                null,
                null,
                "Draft",
                "Role requires .NET and React APIs.",
                null,
                null));
        applicationResponse.EnsureSuccessStatusCode();
        var application = await applicationResponse.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(application);

        var analysisResponse = await client.PostAsync($"/api/applications/{application.Id}/analyze-job", null);
        analysisResponse.EnsureSuccessStatusCode();

        var matchResponse = await client.PostAsync($"/api/applications/{application.Id}/match-evidence", null);

        Assert.Equal(HttpStatusCode.BadRequest, matchResponse.StatusCode);
        var error = await matchResponse.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Contains("ProfileFacts", error.Details!.Keys);
    }

    private static byte[] BuildPdf(string text) =>
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
        %%EOF
        """);
}
