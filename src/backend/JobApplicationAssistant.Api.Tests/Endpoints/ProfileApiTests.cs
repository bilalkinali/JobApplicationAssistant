using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using JobApplicationAssistant.Api.Ai;
using JobApplicationAssistant.Api.Contracts;
using JobApplicationAssistant.Api.Data;
using JobApplicationAssistant.Api.Domain;
using JobApplicationAssistant.Api.Imports;
using JobApplicationAssistant.Api.Tests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        await using var factory = new TestApplicationFactory(services =>
        {
            services.RemoveAll<IPdfTextExtractor>();
            services.AddSingleton<IPdfTextExtractor>(new StubPdfTextExtractor("Built .NET React APIs for internal workflow automation."));
        });
        using var client = factory.CreateClient();
        using var form = new MultipartFormDataContent();
        using var file = new ByteArrayContent([1, 2, 3]);
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

    [Fact]
    public async Task GetImportDraftFacts_returns_grouped_source_context_and_duplicate_indicators()
    {
        await using var factory = new TestApplicationFactory(services =>
        {
            services.RemoveAll<IPdfTextExtractor>();
            services.AddSingleton<IPdfTextExtractor>(new StubPdfTextExtractor("Project Alpha: Built .NET React APIs. Project Beta: Built .NET React APIs."));
            services.RemoveAll<IAiProvider>();
            services.AddSingleton<IAiProvider>(new StubAiProvider(new AssistedProfileImportResult(
            [
                new AssistedProfileImportFact(
                    "Project",
                    "Built workflow APIs",
                    "Built .NET React APIs for workflow automation.",
                    ["Built APIs"],
                    [".NET", "React"],
                    ["Built .NET APIs"],
                    [],
                    "Project Alpha: Built .NET React APIs."),
                new AssistedProfileImportFact(
                    "Project",
                    "Built workflow APIs",
                    "Built .NET React APIs for internal automation.",
                    ["Built APIs"],
                    [".NET", "React"],
                    ["Built React workflows"],
                    [],
                    "Project Beta: Built .NET React APIs."),
                new AssistedProfileImportFact(
                    "Education",
                    "Completed cloud course",
                    "Completed cloud architecture coursework.",
                    ["Cloud coursework"],
                    ["Azure"],
                    ["Completed cloud coursework"],
                    [],
                    "Education: cloud architecture coursework.")
            ])));
        });
        await SeedApprovedProfileFactAsync(factory);
        using var client = factory.CreateClient();

        var importResponse = await PostPdfImportAsync(client);

        Assert.Equal(HttpStatusCode.Created, importResponse.StatusCode);
        var import = await importResponse.Content.ReadFromJsonAsync<AssistedProfileImportResponse>();
        Assert.NotNull(import);

        var response = await client.GetAsync($"/api/profile/imports/{import.ImportSessionId:N}/draft-facts");

        response.EnsureSuccessStatusCode();
        var queue = await response.Content.ReadFromJsonAsync<ImportedDraftFactReviewQueueResponse>();
        Assert.NotNull(queue);
        Assert.Equal(import.ImportSessionId, queue.ImportSessionId);
        Assert.Equal("cv.pdf", queue.FileName);
        Assert.Equal(3, queue.DraftFactCount);
        Assert.Collection(
            queue.Groups,
            group =>
            {
                Assert.Equal("Education", group.Key);
                Assert.Single(group.Facts);
                Assert.Contains("cloud architecture", group.Facts[0].SourceContext);
                Assert.False(group.Facts[0].HasDuplicateIndicators);
            },
            group =>
            {
                Assert.Equal("Project", group.Key);
                Assert.Equal(2, group.DraftFactCount);
                Assert.All(group.Facts, fact =>
                {
                    Assert.Contains("Built .NET React APIs", fact.SourceContext);
                    Assert.Contains(fact.DuplicateIndicators, indicator => indicator.Scope == "ImportBatch");
                    Assert.Contains(fact.DuplicateIndicators, indicator => indicator.Scope == "ExistingProfileFact");
                    Assert.Equal("Draft", fact.ProfileFact.Status);
                    Assert.Null(fact.ProfileFact.OriginalImportedSnapshot);
                });
            });

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Contains(db.ProfileFacts, fact => fact.Status == ProfileFactStatus.Approved && fact.Title == "Existing workflow API work");
        Assert.Equal(3, db.ProfileFacts.Count(fact => fact.Status == ProfileFactStatus.Draft));
    }

    [Fact]
    public async Task PostImportedDraftFactDecision_approves_imported_fact_and_updates_review_queue()
    {
        await using var factory = new TestApplicationFactory(services =>
        {
            services.RemoveAll<IPdfTextExtractor>();
            services.AddSingleton<IPdfTextExtractor>(new StubPdfTextExtractor("Built .NET APIs."));
        });
        using var client = factory.CreateClient();
        var import = await ImportSingleFactAsync(client);
        var importedFact = Assert.Single(import.ProfileFacts);

        var response = await client.PostAsJsonAsync(
            $"/api/profile/imports/{import.ImportSessionId:N}/draft-facts/{importedFact.Id:N}/decision",
            new ImportedDraftFactDecisionRequest("approve"));

        response.EnsureSuccessStatusCode();
        var decision = await response.Content.ReadFromJsonAsync<ImportedDraftFactDecisionResponse>();
        Assert.NotNull(decision);
        Assert.Equal("Approved", decision.ProfileFact.Status);
        Assert.False(decision.ProfileFact.ManuallyEdited);
        Assert.Equal(0, decision.ReviewQueue.DraftFactCount);
        Assert.Empty(decision.ReviewQueue.Groups);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var fact = Assert.Single(db.ProfileFacts);
        Assert.Equal(ProfileFactStatus.Approved, fact.Status);
    }

    [Fact]
    public async Task PostImportedDraftFactDecision_edits_before_approving_imported_fact()
    {
        await using var factory = new TestApplicationFactory(services =>
        {
            services.RemoveAll<IPdfTextExtractor>();
            services.AddSingleton<IPdfTextExtractor>(new StubPdfTextExtractor("Built .NET APIs."));
        });
        using var client = factory.CreateClient();
        var import = await ImportSingleFactAsync(client);
        var importedFact = Assert.Single(import.ProfileFacts);

        var response = await client.PostAsJsonAsync(
            $"/api/profile/imports/{import.ImportSessionId:N}/draft-facts/{importedFact.Id:N}/decision",
            new ImportedDraftFactDecisionRequest(
                "approve",
                new ProfileFactRequest(
                    "Project",
                    "Edited imported API work",
                    "Edited summary from reviewed CV evidence.",
                    "Draft",
                    """["Edited item"]""",
                    """[".NET","React"]""",
                    """["Edited allowed claim"]""",
                    "[]")));

        response.EnsureSuccessStatusCode();
        var decision = await response.Content.ReadFromJsonAsync<ImportedDraftFactDecisionResponse>();
        Assert.NotNull(decision);
        Assert.Equal("Approved", decision.ProfileFact.Status);
        Assert.Equal("Edited imported API work", decision.ProfileFact.Title);
        Assert.True(decision.ProfileFact.ManuallyEdited);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var fact = Assert.Single(db.ProfileFacts);
        Assert.Equal(ProfileFactStatus.Approved, fact.Status);
        Assert.Equal("Edited imported API work", fact.Title);
        Assert.True(fact.ManuallyEdited);
    }

    [Fact]
    public async Task PostImportedDraftFactDecision_archives_imported_fact_without_approving()
    {
        await using var factory = new TestApplicationFactory(services =>
        {
            services.RemoveAll<IPdfTextExtractor>();
            services.AddSingleton<IPdfTextExtractor>(new StubPdfTextExtractor("Built .NET APIs."));
        });
        using var client = factory.CreateClient();
        var import = await ImportSingleFactAsync(client);
        var importedFact = Assert.Single(import.ProfileFacts);

        var response = await client.PostAsJsonAsync(
            $"/api/profile/imports/{import.ImportSessionId:N}/draft-facts/{importedFact.Id:N}/decision",
            new ImportedDraftFactDecisionRequest("archive"));

        response.EnsureSuccessStatusCode();
        var decision = await response.Content.ReadFromJsonAsync<ImportedDraftFactDecisionResponse>();
        Assert.NotNull(decision);
        Assert.Equal("Archived", decision.ProfileFact.Status);
        Assert.Equal(0, decision.ReviewQueue.DraftFactCount);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(ProfileFactStatus.Archived, Assert.Single(db.ProfileFacts).Status);
    }

    [Fact]
    public async Task PostImportedDraftFactDecision_rejects_imported_fact_without_approving()
    {
        await using var factory = new TestApplicationFactory(services =>
        {
            services.RemoveAll<IPdfTextExtractor>();
            services.AddSingleton<IPdfTextExtractor>(new StubPdfTextExtractor("Built .NET APIs."));
        });
        using var client = factory.CreateClient();
        var import = await ImportSingleFactAsync(client);
        var importedFact = Assert.Single(import.ProfileFacts);

        var response = await client.PostAsJsonAsync(
            $"/api/profile/imports/{import.ImportSessionId:N}/draft-facts/{importedFact.Id:N}/decision",
            new ImportedDraftFactDecisionRequest("reject"));

        response.EnsureSuccessStatusCode();
        var decision = await response.Content.ReadFromJsonAsync<ImportedDraftFactDecisionResponse>();
        Assert.NotNull(decision);
        Assert.Equal("Rejected", decision.ProfileFact.Status);
        Assert.Equal(0, decision.ReviewQueue.DraftFactCount);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(ProfileFactStatus.Rejected, Assert.Single(db.ProfileFacts).Status);
    }

    [Fact]
    public async Task PostImportedDraftFactMerge_combines_selected_drafts_and_archives_sources()
    {
        await using var factory = MultiFactImportFactory();
        using var client = factory.CreateClient();
        var import = await ImportSingleBatchAsync(client);
        var selectedIds = import.ProfileFacts.Take(2).Select(fact => fact.Id).ToList();

        var response = await client.PostAsJsonAsync(
            $"/api/profile/imports/{import.ImportSessionId:N}/draft-facts/merge",
            new ImportedDraftFactMergeRequest(selectedIds));

        response.EnsureSuccessStatusCode();
        var merge = await response.Content.ReadFromJsonAsync<ImportedDraftFactMergeResponse>();
        Assert.NotNull(merge);
        Assert.Equal("Draft", merge.ProfileFact.Status);
        Assert.Contains("Alpha API", merge.ProfileFact.Title);
        Assert.Contains("Beta UI", merge.ProfileFact.Title);
        Assert.Contains(".NET", merge.ProfileFact.Technologies);
        Assert.Contains("React", merge.ProfileFact.Technologies);
        Assert.Contains("Alpha source", merge.ProfileFact.OriginalImportedSnapshot);
        Assert.Contains("Beta source", merge.ProfileFact.OriginalImportedSnapshot);
        Assert.Equal(2, merge.ReviewQueue.DraftFactCount);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(1, db.ProfileFacts.Count(fact => fact.Status == ProfileFactStatus.Archived));
        Assert.Equal(2, db.ProfileFacts.Count(fact => fact.Status == ProfileFactStatus.Draft));
        Assert.Empty(db.ProfileFacts.Where(fact => fact.Status == ProfileFactStatus.Approved));
    }

    [Fact]
    public async Task PostImportedDraftFactSplit_creates_narrower_drafts_and_preserves_source_context()
    {
        await using var factory = MultiFactImportFactory();
        using var client = factory.CreateClient();
        var import = await ImportSingleBatchAsync(client);
        var importedFact = import.ProfileFacts[0];

        var response = await client.PostAsJsonAsync(
            $"/api/profile/imports/{import.ImportSessionId:N}/draft-facts/{importedFact.Id:N}/split",
            new ImportedDraftFactSplitRequest(
            [
                new ProfileFactRequest("Project", "Alpha API backend", "Built backend APIs.", "Draft", """["Built backend"]""", """[".NET"]""", """["Built backend APIs"]""", "[]"),
                new ProfileFactRequest("Project", "Alpha API delivery", "Shipped the API workflow.", "Draft", """["Shipped workflow"]""", """["Azure"]""", """["Shipped API workflow"]""", "[]")
            ]));

        response.EnsureSuccessStatusCode();
        var split = await response.Content.ReadFromJsonAsync<ImportedDraftFactSplitResponse>();
        Assert.NotNull(split);
        Assert.Equal(2, split.ProfileFacts.Count);
        Assert.All(split.ProfileFacts, fact =>
        {
            Assert.Equal("Draft", fact.Status);
            Assert.True(fact.ManuallyEdited);
            Assert.Contains(import.ImportSessionId.ToString("N"), fact.SourceDocumentIds);
            Assert.Contains("Alpha source", fact.OriginalImportedSnapshot);
        });
        Assert.Equal(4, split.ReviewQueue.DraftFactCount);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(ProfileFactStatus.Archived, db.ProfileFacts.Single(fact => fact.Id == importedFact.Id).Status);
        Assert.Equal(4, db.ProfileFacts.Count(fact => fact.Status == ProfileFactStatus.Draft));
    }

    [Fact]
    public async Task PostImportedDraftFactBulkDecision_approves_only_selected_imported_drafts()
    {
        await using var factory = MultiFactImportFactory();
        using var client = factory.CreateClient();
        var import = await ImportSingleBatchAsync(client);
        var selectedIds = import.ProfileFacts.Take(2).Select(fact => fact.Id).ToList();

        var response = await client.PostAsJsonAsync(
            $"/api/profile/imports/{import.ImportSessionId:N}/draft-facts/bulk-decision",
            new ImportedDraftFactBulkDecisionRequest("approve", selectedIds));

        response.EnsureSuccessStatusCode();
        var bulk = await response.Content.ReadFromJsonAsync<ImportedDraftFactBulkDecisionResponse>();
        Assert.NotNull(bulk);
        Assert.All(bulk.ProfileFacts, fact => Assert.Equal("Approved", fact.Status));
        Assert.Equal(1, bulk.ReviewQueue.DraftFactCount);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(2, db.ProfileFacts.Count(fact => fact.Status == ProfileFactStatus.Approved));
        Assert.Equal(1, db.ProfileFacts.Count(fact => fact.Status == ProfileFactStatus.Draft));
        Assert.Empty(db.ProfileFacts.Where(fact => fact.Status == ProfileFactStatus.Archived));
    }

    [Fact]
    public async Task PostImportedDraftFactBulkDecision_archives_selected_imported_drafts_without_approving()
    {
        await using var factory = MultiFactImportFactory();
        using var client = factory.CreateClient();
        var import = await ImportSingleBatchAsync(client);
        var selectedIds = import.ProfileFacts.Take(2).Select(fact => fact.Id).ToList();

        var response = await client.PostAsJsonAsync(
            $"/api/profile/imports/{import.ImportSessionId:N}/draft-facts/bulk-decision",
            new ImportedDraftFactBulkDecisionRequest("archive", selectedIds));

        response.EnsureSuccessStatusCode();
        var bulk = await response.Content.ReadFromJsonAsync<ImportedDraftFactBulkDecisionResponse>();
        Assert.NotNull(bulk);
        Assert.All(bulk.ProfileFacts, fact => Assert.Equal("Archived", fact.Status));
        Assert.Equal(1, bulk.ReviewQueue.DraftFactCount);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(2, db.ProfileFacts.Count(fact => fact.Status == ProfileFactStatus.Archived));
        Assert.Equal(1, db.ProfileFacts.Count(fact => fact.Status == ProfileFactStatus.Draft));
        Assert.Empty(db.ProfileFacts.Where(fact => fact.Status == ProfileFactStatus.Approved));
    }

    [Fact]
    public async Task PostImportedDraftFactBulkDecision_rejects_archived_or_untrusted_fact_ids()
    {
        await using var factory = MultiFactImportFactory();
        using var client = factory.CreateClient();
        var import = await ImportSingleBatchAsync(client);
        var archivedFact = import.ProfileFacts[0];
        var archiveResponse = await client.PostAsJsonAsync(
            $"/api/profile/imports/{import.ImportSessionId:N}/draft-facts/{archivedFact.Id:N}/decision",
            new ImportedDraftFactDecisionRequest("archive"));
        archiveResponse.EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync(
            $"/api/profile/imports/{import.ImportSessionId:N}/draft-facts/bulk-decision",
            new ImportedDraftFactBulkDecisionRequest("approve", [archivedFact.Id, import.ProfileFacts[1].Id]));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(ProfileFactStatus.Archived, db.ProfileFacts.Single(fact => fact.Id == archivedFact.Id).Status);
        Assert.Equal(ProfileFactStatus.Draft, db.ProfileFacts.Single(fact => fact.Id == import.ProfileFacts[1].Id).Status);
        Assert.Empty(db.ProfileFacts.Where(fact => fact.Status == ProfileFactStatus.Approved));
    }

    [Fact]
    public async Task Approved_imported_fact_can_support_evidence_matching()
    {
        await using var factory = new TestApplicationFactory(services =>
        {
            services.RemoveAll<IPdfTextExtractor>();
            services.AddSingleton<IPdfTextExtractor>(new StubPdfTextExtractor("Built .NET React APIs for internal workflow automation."));
        });
        using var client = factory.CreateClient();
        var import = await ImportSingleFactAsync(client);
        var importedFact = Assert.Single(import.ProfileFacts);
        var approveResponse = await client.PostAsJsonAsync(
            $"/api/profile/imports/{import.ImportSessionId:N}/draft-facts/{importedFact.Id:N}/decision",
            new ImportedDraftFactDecisionRequest("approve"));
        approveResponse.EnsureSuccessStatusCode();

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

        matchResponse.EnsureSuccessStatusCode();
        var matched = await matchResponse.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(matched);
        Assert.Contains(importedFact.Id.ToString(), matched.EvidenceMatches);
        Assert.Contains(importedFact.Title, matched.EvidenceMatches);
    }

    [Fact]
    public async Task PostPdfCvImport_returns_pdf_extraction_validation()
    {
        await using var factory = new TestApplicationFactory(services =>
        {
            services.RemoveAll<IPdfTextExtractor>();
            services.AddSingleton<IPdfTextExtractor>(new StubPdfTextExtractor(PdfTextExtractionResult.Failure("Uploaded PDF was malformed.")));
        });
        using var client = factory.CreateClient();
        using var form = new MultipartFormDataContent();
        using var file = new ByteArrayContent([1, 2, 3]);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(file, "file", "cv.pdf");

        var response = await client.PostAsync("/api/profile/imports/pdf-cv", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Equal("Uploaded PDF was malformed.", Assert.Single(error.Details!["file"]));
    }

    [Fact]
    public async Task PostPdfCvImport_rejects_empty_extracted_text_before_ai()
    {
        await using var factory = new TestApplicationFactory(services =>
        {
            services.RemoveAll<IPdfTextExtractor>();
            services.AddSingleton<IPdfTextExtractor>(new StubPdfTextExtractor("   "));
        });
        using var client = factory.CreateClient();

        var response = await PostPdfImportAsync(client);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Equal("Uploaded PDF did not contain importable text.", Assert.Single(error.Details!["file"]));

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Empty(db.ProfileFacts);
        Assert.Empty(db.AiRuns);
    }

    [Fact]
    public async Task PostPdfCvImport_records_invalid_output_and_preserves_existing_state()
    {
        var handler = new AiStatusApiTests.QueuedOpenAiCompatibleHandler(new Queue<HttpResponseMessage>(new[]
        {
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OpenAiChatCompletionContent("{ malformed") },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OpenAiChatCompletionContent("""{"facts":[]}""") }
        }));
        await using var factory = new TestApplicationFactory(services =>
        {
            services.RemoveAll<IPdfTextExtractor>();
            services.AddSingleton<IPdfTextExtractor>(new StubPdfTextExtractor("Built .NET APIs."));
        }).WithOpenAiCompatibleHandler(handler, model: "local-model", storeRawPayloads: true);
        await SeedExistingImportedFactAsync(factory);
        using var client = factory.CreateClient();

        var response = await PostPdfImportAsync(client);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Contains("OpenAI-compatible endpoint returned assisted profile import without facts.", Assert.Single(error.Details!["AiProvider"]));

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var existingFact = Assert.Single(db.ProfileFacts);
        Assert.Equal("Existing imported fact", existingFact.Title);
        Assert.Contains("existing-import-session", existingFact.OriginalImportedSnapshot);
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("AssistedProfileImport", run.Step);
        Assert.Equal("OpenAiCompatible", run.Provider);
        Assert.Equal("Failed", run.Status);
        Assert.Equal("InvalidOutput", run.ErrorCode);
        Assert.Equal(2, run.AttemptCount);
        Assert.Contains("Raw payload:", run.ErrorMessage);
    }

    [Fact]
    public async Task PostPdfCvImport_rejects_incomplete_provider_result_without_persisting_facts()
    {
        await using var factory = new TestApplicationFactory(services =>
        {
            services.RemoveAll<IPdfTextExtractor>();
            services.AddSingleton<IPdfTextExtractor>(new StubPdfTextExtractor("Built .NET APIs."));
            services.RemoveAll<IAiProvider>();
            services.AddSingleton<IAiProvider>(new StubAiProvider(new AssistedProfileImportResult(
            [
                new AssistedProfileImportFact(
                    "Project",
                    "",
                    "Built APIs.",
                    [],
                    [],
                    [],
                    [],
                    "CV project section")
            ])));
        });
        using var client = factory.CreateClient();

        var response = await PostPdfImportAsync(client);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Equal("AI provider returned structurally invalid assisted profile import facts.", Assert.Single(error.Details!["AiProvider"]));

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Empty(db.ProfileFacts);
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("Failed", run.Status);
        Assert.Equal("InvalidOutput", run.ErrorCode);
    }

    [Fact]
    public async Task PostPdfCvImport_records_non_success_response_without_raw_payload_when_disabled()
    {
        var handler = new AiStatusApiTests.QueuedOpenAiCompatibleHandler(new Queue<HttpResponseMessage>(new[]
        {
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { ReasonPhrase = "Service Unavailable" }
        }));
        await using var factory = new TestApplicationFactory(services =>
        {
            services.RemoveAll<IPdfTextExtractor>();
            services.AddSingleton<IPdfTextExtractor>(new StubPdfTextExtractor("Built .NET APIs."));
        }).WithOpenAiCompatibleHandler(handler, model: "local-model");
        using var client = factory.CreateClient();

        var response = await PostPdfImportAsync(client);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Empty(db.ProfileFacts);
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("OpenAiCompatible", run.Provider);
        Assert.Equal("ProviderUnavailable", run.ErrorCode);
        Assert.Contains("503 Service Unavailable", run.ErrorMessage);
        Assert.DoesNotContain("Raw request:", run.ErrorMessage);
    }

    [Fact]
    public async Task PostPdfCvImport_records_unreachable_provider_without_fake_fallback()
    {
        var handler = new AiStatusApiTests.QueuedOpenAiCompatibleHandler(
            new Queue<HttpResponseMessage>(),
            new HttpRequestException("No connection could be made."));
        await using var factory = new TestApplicationFactory(services =>
        {
            services.RemoveAll<IPdfTextExtractor>();
            services.AddSingleton<IPdfTextExtractor>(new StubPdfTextExtractor("Built .NET APIs."));
        }).WithOpenAiCompatibleHandler(handler, model: "local-model");
        using var client = factory.CreateClient();

        var response = await PostPdfImportAsync(client);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Empty(db.ProfileFacts);
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("OpenAiCompatible", run.Provider);
        Assert.Equal("ProviderUnavailable", run.ErrorCode);
        Assert.DoesNotContain("Fake assisted import", run.ErrorMessage);
    }

    [Fact]
    public async Task PostPdfCvImport_records_provider_timeout()
    {
        var handler = new AiStatusApiTests.QueuedOpenAiCompatibleHandler(
            new Queue<HttpResponseMessage>(),
            new TaskCanceledException("timed out"));
        await using var factory = new TestApplicationFactory(services =>
        {
            services.RemoveAll<IPdfTextExtractor>();
            services.AddSingleton<IPdfTextExtractor>(new StubPdfTextExtractor("Built .NET APIs."));
        }).WithOpenAiCompatibleHandler(handler, model: "local-model");
        using var client = factory.CreateClient();

        var response = await PostPdfImportAsync(client);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("ProviderUnavailable", run.ErrorCode);
        Assert.Contains("endpoint is unavailable", run.ErrorMessage);
    }

    [Fact]
    public async Task PostPdfCvImport_records_invalid_provider_json()
    {
        var handler = new AiStatusApiTests.QueuedOpenAiCompatibleHandler(new Queue<HttpResponseMessage>(new[]
        {
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{ invalid", System.Text.Encoding.UTF8, "application/json") }
        }));
        await using var factory = new TestApplicationFactory(services =>
        {
            services.RemoveAll<IPdfTextExtractor>();
            services.AddSingleton<IPdfTextExtractor>(new StubPdfTextExtractor("Built .NET APIs."));
        }).WithOpenAiCompatibleHandler(handler, model: "local-model");
        using var client = factory.CreateClient();

        var response = await PostPdfImportAsync(client);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("InvalidOutput", run.ErrorCode);
        Assert.Contains("malformed chat completion JSON", run.ErrorMessage);
    }

    [Fact]
    public async Task PostPdfCvImport_imports_valid_real_provider_output_and_records_success()
    {
        var handler = new AiStatusApiTests.QueuedOpenAiCompatibleHandler(new Queue<HttpResponseMessage>(new[]
        {
            new HttpResponseMessage(HttpStatusCode.OK) { Content = OpenAiChatCompletionContent(ValidAssistedProfileImportJson()) }
        }));
        await using var factory = new TestApplicationFactory(services =>
        {
            services.RemoveAll<IPdfTextExtractor>();
            services.AddSingleton<IPdfTextExtractor>(new StubPdfTextExtractor("Built .NET APIs."));
        }).WithOpenAiCompatibleHandler(handler, model: "local-model");
        using var client = factory.CreateClient();

        var response = await PostPdfImportAsync(client);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Single(db.ProfileFacts);
        var run = Assert.Single(db.AiRuns);
        Assert.Equal("Succeeded", run.Status);
        Assert.Equal("OpenAiCompatible", run.Provider);
        Assert.Equal("Imported 1 draft profile fact(s).", run.OutputSummary);
    }

    private static async Task<HttpResponseMessage> PostPdfImportAsync(HttpClient client)
    {
        using var form = new MultipartFormDataContent();
        using var file = new ByteArrayContent([1, 2, 3]);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(file, "file", "cv.pdf");
        return await client.PostAsync("/api/profile/imports/pdf-cv", form);
    }

    private static async Task<AssistedProfileImportResponse> ImportSingleFactAsync(HttpClient client)
    {
        var importResponse = await PostPdfImportAsync(client);
        importResponse.EnsureSuccessStatusCode();
        var import = await importResponse.Content.ReadFromJsonAsync<AssistedProfileImportResponse>();
        Assert.NotNull(import);
        Assert.Single(import.ProfileFacts);
        return import;
    }

    private static async Task<AssistedProfileImportResponse> ImportSingleBatchAsync(HttpClient client)
    {
        var importResponse = await PostPdfImportAsync(client);
        importResponse.EnsureSuccessStatusCode();
        var import = await importResponse.Content.ReadFromJsonAsync<AssistedProfileImportResponse>();
        Assert.NotNull(import);
        Assert.Equal(3, import.ProfileFacts.Count);
        return import;
    }

    private static TestApplicationFactory MultiFactImportFactory() =>
        new(services =>
        {
            services.RemoveAll<IPdfTextExtractor>();
            services.AddSingleton<IPdfTextExtractor>(new StubPdfTextExtractor("Alpha API. Beta UI. Gamma cloud."));
            services.RemoveAll<IAiProvider>();
            services.AddSingleton<IAiProvider>(new StubAiProvider(new AssistedProfileImportResult(
            [
                new AssistedProfileImportFact(
                    "Project",
                    "Alpha API",
                    "Built .NET APIs for workflow automation.",
                    ["Built APIs"],
                    [".NET"],
                    ["Built .NET APIs"],
                    [],
                    "Alpha source context."),
                new AssistedProfileImportFact(
                    "Project",
                    "Beta UI",
                    "Built React UI for workflow automation.",
                    ["Built UI"],
                    ["React"],
                    ["Built React UI"],
                    [],
                    "Beta source context."),
                new AssistedProfileImportFact(
                    "Project",
                    "Gamma cloud",
                    "Configured Azure deployment automation.",
                    ["Configured deployment"],
                    ["Azure"],
                    ["Configured Azure deployment"],
                    [],
                    "Gamma source context.")
            ])));
        });

    private static JsonContent OpenAiChatCompletionContent(string content) =>
        JsonContent.Create(new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        role = "assistant",
                        content
                    }
                }
            }
        });

    private static string ValidAssistedProfileImportJson() =>
        """
        {
          "facts": [
            {
              "type": "Project",
              "title": "Imported API work",
              "summary": "Built .NET APIs from imported CV evidence.",
              "factItems": ["Built APIs"],
              "technologies": [".NET"],
              "allowedClaims": ["Built .NET APIs"],
              "forbiddenClaims": ["Do not claim production ownership."],
              "sourceContext": "CV project section"
            }
          ]
        }
        """;

    private static async Task SeedExistingImportedFactAsync(WebApplicationFactory<Program> factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.ProfileFacts.Add(new ProfileFact
        {
            Id = Guid.NewGuid(),
            Type = "ImportedCv",
            Title = "Existing imported fact",
            Summary = "Existing imported session fact.",
            Status = ProfileFactStatus.Draft,
            FactItems = "[]",
            Technologies = "[]",
            AllowedClaims = "[]",
            ForbiddenClaims = "[]",
            SourceDocumentIds = """["existing-import-session"]""",
            OriginalImportedSnapshot = """{"importSessionId":"existing-import-session"}""",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedApprovedProfileFactAsync(WebApplicationFactory<Program> factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.ProfileFacts.Add(new ProfileFact
        {
            Id = Guid.NewGuid(),
            Type = "Project",
            Title = "Existing workflow API work",
            Summary = "Built .NET React APIs for workflow automation.",
            Status = ProfileFactStatus.Approved,
            FactItems = """["Built APIs"]""",
            Technologies = """[".NET","React"]""",
            AllowedClaims = """["Built .NET APIs"]""",
            ForbiddenClaims = "[]",
            SourceDocumentIds = "[]",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
    }

    private sealed class StubPdfTextExtractor : IPdfTextExtractor
    {
        private readonly PdfTextExtractionResult result;

        public StubPdfTextExtractor(string text)
            : this(PdfTextExtractionResult.Success(text))
        {
        }

        public StubPdfTextExtractor(PdfTextExtractionResult result)
        {
            this.result = result;
        }

        public Task<PdfTextExtractionResult> ExtractAsync(Stream pdfStream, CancellationToken ct) =>
            Task.FromResult(result);
    }

    private sealed class StubAiProvider(AssistedProfileImportResult importResult) : IAiProvider
    {
        public Task<AiProviderStatus> GetStatusAsync(CancellationToken ct) =>
            Task.FromResult(new AiProviderStatus("Fake", "stub", null, true, "Stub provider is available."));

        public Task<AiDiagnosticsResult> RunDiagnosticsAsync(CancellationToken ct) =>
            Task.FromResult(new AiDiagnosticsResult("Fake", "stub", null, true, "Stub diagnostics passed.", []));

        public Task<JobAnalysisResult> AnalyzeJobAsync(JobAnalysisInput input, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<EvidenceMatchResult> MatchEvidenceAsync(EvidenceMatchInput input, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<DraftGenerationResult> GenerateDraftAsync(DraftGenerationInput input, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<ClaimAuditResult> AuditClaimsAsync(ClaimAuditInput input, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<AssistedProfileImportResult> ImportProfileFactsAsync(AssistedProfileImportInput input, CancellationToken ct) =>
            Task.FromResult(importResult);
    }
}
