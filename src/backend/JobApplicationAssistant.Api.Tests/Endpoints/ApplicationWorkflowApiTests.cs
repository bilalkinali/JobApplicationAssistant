using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using JobApplicationAssistant.Api.Ai;
using JobApplicationAssistant.Api.Contracts;
using JobApplicationAssistant.Api.Data;
using JobApplicationAssistant.Api.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JobApplicationAssistant.Api.Tests.Endpoints;

public sealed class ApplicationWorkflowApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task PostApplication_rejects_missing_required_fields_invalid_url_and_unknown_status()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var request = new ApplicationRequest(
            "",
            " ",
            "not-a-url",
            null,
            "Unknown",
            "",
            null,
            null);

        var response = await client.PostAsJsonAsync("/api/applications", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Contains(nameof(ApplicationRequest.CompanyName), error.Details.Keys);
        Assert.Contains(nameof(ApplicationRequest.RoleTitle), error.Details.Keys);
        Assert.Contains(nameof(ApplicationRequest.ApplicationUrl), error.Details.Keys);
        Assert.Contains(nameof(ApplicationRequest.Status), error.Details.Keys);
    }

    [Fact]
    public async Task AnalyzeJob_rejects_application_without_job_posting_text()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, jobPostingText: "");

        var response = await client.PostAsync($"/api/applications/{application.Id}/analyze-job", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Contains(nameof(ApplicationRequest.JobPostingText), error.Details.Keys);
    }

    [Fact]
    public async Task AnalyzeJob_updates_application_with_extracted_metadata_and_signals()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(
            client,
            """
            Company: Northwind
            Role: Full-stack Developer

            We need C#, .NET, React, TypeScript, PostgreSQL, REST APIs, Docker, and Kubernetes.
            """);

        var response = await client.PostAsync($"/api/applications/{application.Id}/analyze-job", null);

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var analyzed = await response.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(analyzed);
        Assert.Equal("Northwind", analyzed.CompanyName);
        Assert.Equal("Full-stack Developer", analyzed.RoleTitle);
        Assert.Equal("English", analyzed.DetectedLanguage);
        Assert.Equal("English", analyzed.SelectedLanguage);
        Assert.Equal("PostingCaptured", analyzed.Status);

        var signals = JsonSerializer.Deserialize<JobSignalsDocument>(analyzed.JobSignals, JsonOptions);
        Assert.NotNull(signals);
        Assert.Contains(".NET", signals.RequiredSkills);
        Assert.Contains("React", signals.RequiredSkills);
        Assert.Contains("Kubernetes", signals.PreferredSkills);
        Assert.Contains("REST APIs", signals.Responsibilities);
    }

    [Fact]
    public async Task MatchEvidence_rejects_application_without_analyzed_job_signals()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET.");

        var response = await client.PostAsync($"/api/applications/{application.Id}/match-evidence", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Contains("JobSignals", error.Details.Keys);
    }

    [Fact]
    public async Task MatchEvidence_rejects_when_no_approved_profile_facts_exist()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET and Kubernetes.");
        await client.PostAsync($"/api/applications/{application.Id}/analyze-job", null);

        var response = await client.PostAsync($"/api/applications/{application.Id}/match-evidence", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Contains("ProfileFacts", error.Details.Keys);
    }

    [Fact]
    public async Task MatchEvidence_uses_only_approved_profile_facts_and_persists_unmatched_requirements()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        await CreateProfileFactAsync(client, "Approved API work", "Approved", """[".NET"]""");
        await CreateProfileFactAsync(client, "Draft Kubernetes work", "Draft", """["Kubernetes"]""");
        await CreateProfileFactAsync(client, "Archived React work", "Archived", """["React"]""");
        var application = await CreateApplicationAsync(client, "We need .NET, React, and Kubernetes.");
        await client.PostAsync($"/api/applications/{application.Id}/analyze-job", null);

        var response = await client.PostAsync($"/api/applications/{application.Id}/match-evidence", null);

        response.EnsureSuccessStatusCode();
        var matched = await response.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(matched);
        Assert.Equal("ReadyForReview", matched.Status);

        var evidenceMatches = JsonSerializer.Deserialize<List<EvidenceMatch>>(matched.EvidenceMatches, JsonOptions);
        var unmatchedRequirements = JsonSerializer.Deserialize<List<UnmatchedRequirement>>(matched.UnmatchedRequirements, JsonOptions);
        Assert.NotNull(evidenceMatches);
        Assert.NotNull(unmatchedRequirements);

        var match = Assert.Single(evidenceMatches);
        Assert.Equal(".NET", match.Signal);
        Assert.Equal("Approved API work", match.ProfileFactTitle);
        Assert.Contains(unmatchedRequirements, requirement => requirement.Requirement == "React");
        Assert.Contains(unmatchedRequirements, requirement => requirement.Requirement == "Kubernetes");
    }

    [Fact]
    public async Task PutApprovedEvidence_persists_only_submitted_current_matches()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        await CreateProfileFactAsync(client, "Approved API work", "Approved", """[".NET", "React"]""");
        var application = await CreateApplicationAsync(client, "We need .NET and React.");
        await client.PostAsync($"/api/applications/{application.Id}/analyze-job", null);
        var matchResponse = await client.PostAsync($"/api/applications/{application.Id}/match-evidence", null);
        var matched = await matchResponse.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(matched);
        var evidenceMatches = JsonSerializer.Deserialize<List<EvidenceMatch>>(matched.EvidenceMatches, JsonOptions);
        Assert.NotNull(evidenceMatches);
        var selected = Assert.Single(evidenceMatches.Where(match => match.Signal == ".NET"));

        var response = await client.PutAsJsonAsync(
            $"/api/applications/{application.Id}/approved-evidence",
            new ApprovedEvidenceRequest(JsonSerializer.Serialize(new[] { new { selected.Id } }, JsonOptions)));

        response.EnsureSuccessStatusCode();
        var reviewed = await response.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(reviewed);
        Assert.NotEqual("{}", reviewed.JobSignals);
        Assert.NotEqual("[]", reviewed.EvidenceMatches);
        var approvedEvidence = JsonSerializer.Deserialize<List<EvidenceMatch>>(reviewed.ApprovedEvidence, JsonOptions);
        var approved = Assert.Single(approvedEvidence);
        Assert.Equal(selected.Id, approved.Id);
        Assert.Equal(selected.ProfileFactId, approved.ProfileFactId);
    }

    [Fact]
    public async Task PutApprovedEvidence_rejects_unknown_or_malformed_review_state()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET.");

        var unknownIdResponse = await client.PutAsJsonAsync(
            $"/api/applications/{application.Id}/approved-evidence",
            new ApprovedEvidenceRequest("""[{"id":"missing"}]"""));
        var malformedResponse = await client.PutAsJsonAsync(
            $"/api/applications/{application.Id}/approved-evidence",
            new ApprovedEvidenceRequest("""{"id":"missing"}"""));

        Assert.Equal(HttpStatusCode.BadRequest, unknownIdResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, malformedResponse.StatusCode);
    }

    [Fact]
    public async Task GenerateDraft_creates_current_draft_from_approved_evidence()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        await CreateProfileFactAsync(client, "Approved API work", "Approved", """[".NET", "React"]""");
        var application = await CreateApplicationAsync(
            client,
            """
            Company: Northwind
            Role: Full-stack Developer

            We need .NET, React, and Kubernetes.
            """,
            "English");
        Assert.NotEqual(Guid.Empty, application.Id);
        var analysisResponse = await client.PostAsync($"/api/applications/{application.Id}/analyze-job", null);
        Assert.True(analysisResponse.IsSuccessStatusCode, await analysisResponse.Content.ReadAsStringAsync());
        var matchResponse = await client.PostAsync($"/api/applications/{application.Id}/match-evidence", null);
        Assert.True(matchResponse.IsSuccessStatusCode, await matchResponse.Content.ReadAsStringAsync());
        var matched = await matchResponse.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(matched);
        var evidenceMatches = JsonSerializer.Deserialize<List<EvidenceMatch>>(matched.EvidenceMatches, JsonOptions);
        Assert.NotNull(evidenceMatches);
        await client.PutAsJsonAsync(
            $"/api/applications/{application.Id}/approved-evidence",
            new ApprovedEvidenceRequest(JsonSerializer.Serialize(evidenceMatches, JsonOptions)));

        var response = await client.PostAsync($"/api/applications/{application.Id}/generate-draft", null);

        response.EnsureSuccessStatusCode();
        var draft = await response.Content.ReadFromJsonAsync<GeneratedDraftResponse>();
        Assert.NotNull(draft);
        Assert.Equal(application.Id, draft.JobApplicationId);
        Assert.Contains("Northwind", draft.CoverLetterText);
        Assert.Contains("Full-stack Developer", draft.CoverLetterText);
        Assert.Contains("Approved API work", draft.CoverLetterText);
        Assert.Contains("Kubernetes", draft.CoverLetterText);
        Assert.Contains("Northwind", draft.ShortMotivationText);
        Assert.Contains("Approved API work", draft.ShortMotivationText);
        Assert.Null(draft.LastEditedAt);
        Assert.Null(draft.AuditUpdatedAt);
    }

    [Fact]
    public async Task GenerateDraft_rejects_application_without_job_posting_text()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, jobPostingText: "");

        var response = await client.PostAsync($"/api/applications/{application.Id}/generate-draft", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Contains(nameof(ApplicationRequest.JobPostingText), error.Details.Keys);
    }

    [Fact]
    public async Task GenerateDraft_rejects_application_without_approved_evidence()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET.");

        var response = await client.PostAsync($"/api/applications/{application.Id}/generate-draft", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Contains(nameof(ApplicationResponse.ApprovedEvidence), error.Details.Keys);
    }

    [Fact]
    public async Task GenerateDraft_updates_the_current_generated_draft_as_latest_state()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        await CreateProfileFactAsync(client, "Approved API work", "Approved", """[".NET"]""");
        var application = await CreateApplicationAsync(client, "We need .NET.");
        await client.PostAsync($"/api/applications/{application.Id}/analyze-job", null);
        var matchResponse = await client.PostAsync($"/api/applications/{application.Id}/match-evidence", null);
        Assert.True(matchResponse.IsSuccessStatusCode, await matchResponse.Content.ReadAsStringAsync());
        var matched = await matchResponse.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(matched);
        await client.PutAsJsonAsync(
            $"/api/applications/{application.Id}/approved-evidence",
            new ApprovedEvidenceRequest(matched.EvidenceMatches));
        var firstResponse = await client.PostAsync($"/api/applications/{application.Id}/generate-draft", null);
        firstResponse.EnsureSuccessStatusCode();
        var first = await firstResponse.Content.ReadFromJsonAsync<GeneratedDraftResponse>();
        Assert.NotNull(first);

        var secondResponse = await client.PostAsync($"/api/applications/{application.Id}/generate-draft", null);

        secondResponse.EnsureSuccessStatusCode();
        var second = await secondResponse.Content.ReadFromJsonAsync<GeneratedDraftResponse>();
        Assert.NotNull(second);
        Assert.Equal(first.Id, second.Id);
        Assert.Equal(first.JobApplicationId, second.JobApplicationId);
        Assert.Equal(first.CreatedAt, second.CreatedAt);
        Assert.True(second.GeneratedAt >= first.GeneratedAt);
    }

    [Fact]
    public async Task GetApplication_includes_current_generated_draft_when_one_exists()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var draft = await CreateGeneratedDraftAsync(client);

        var response = await client.GetAsync($"/api/applications/{draft.JobApplicationId}");

        response.EnsureSuccessStatusCode();
        var application = await response.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(application);
        Assert.NotNull(application.GeneratedDraft);
        Assert.Equal(draft.Id, application.GeneratedDraft.Id);
        Assert.Equal(draft.CoverLetterText, application.GeneratedDraft.CoverLetterText);
        Assert.Equal(draft.ShortMotivationText, application.GeneratedDraft.ShortMotivationText);
    }

    [Fact]
    public async Task PutGeneratedDraft_saves_manual_edits_preserves_generation_metadata_and_marks_audit_stale()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var draft = await CreateGeneratedDraftAsync(client);
        await MarkDraftAuditedAsync(factory, draft.Id);

        var response = await client.PutAsJsonAsync(
            $"/api/applications/{draft.JobApplicationId}/generated-draft",
            new GeneratedDraftEditRequest(
                "Edited cover letter text.",
                "Edited short motivation text."));

        response.EnsureSuccessStatusCode();
        var edited = await response.Content.ReadFromJsonAsync<GeneratedDraftResponse>();
        Assert.NotNull(edited);
        Assert.Equal(draft.Id, edited.Id);
        Assert.Equal("Edited cover letter text.", edited.CoverLetterText);
        Assert.Equal("Edited short motivation text.", edited.ShortMotivationText);
        Assert.Equal(draft.GeneratedAt, edited.GeneratedAt);
        Assert.Equal(draft.CreatedAt, edited.CreatedAt);
        Assert.NotNull(edited.LastEditedAt);
        Assert.Equal(edited.LastEditedAt, edited.UpdatedAt);
        Assert.NotNull(edited.AuditUpdatedAt);
        Assert.True(edited.IsClaimAuditStale);

        var reopenedResponse = await client.GetAsync($"/api/applications/{draft.JobApplicationId}");
        reopenedResponse.EnsureSuccessStatusCode();
        var reopened = await reopenedResponse.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(reopened?.GeneratedDraft);
        Assert.Equal("Edited cover letter text.", reopened.GeneratedDraft.CoverLetterText);
        Assert.Equal("Edited short motivation text.", reopened.GeneratedDraft.ShortMotivationText);
        Assert.True(reopened.GeneratedDraft.IsClaimAuditStale);
    }

    [Fact]
    public async Task PutGeneratedDraft_rejects_application_without_generated_draft()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET.");

        var response = await client.PutAsJsonAsync(
            $"/api/applications/{application.Id}/generated-draft",
            new GeneratedDraftEditRequest("Cover letter.", "Short motivation."));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Contains(nameof(ApplicationResponse.GeneratedDraft), error.Details.Keys);
    }

    [Fact]
    public async Task AuditClaims_rejects_application_without_generated_draft()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var application = await CreateApplicationAsync(client, "We need .NET.");

        var response = await client.PostAsync($"/api/applications/{application.Id}/audit-claims", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Contains(nameof(ApplicationResponse.GeneratedDraft), error.Details.Keys);
    }

    [Fact]
    public async Task AuditClaims_persists_structured_results_updates_timestamp_and_clears_stale_state()
    {
        await using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient();
        var draft = await CreateGeneratedDraftAsync(client);
        var firstAuditResponse = await client.PostAsync($"/api/applications/{draft.JobApplicationId}/audit-claims", null);
        firstAuditResponse.EnsureSuccessStatusCode();
        var firstAudit = await firstAuditResponse.Content.ReadFromJsonAsync<GeneratedDraftResponse>();
        Assert.NotNull(firstAudit);
        Assert.NotNull(firstAudit.AuditUpdatedAt);
        await client.PutAsJsonAsync(
            $"/api/applications/{draft.JobApplicationId}/generated-draft",
            new GeneratedDraftEditRequest(
                """
                Evidence for Approved API work.
                Led Kubernetes platform operations.
                I may be a fit for the team.
                """,
                ""));

        var response = await client.PostAsync($"/api/applications/{draft.JobApplicationId}/audit-claims", null);

        response.EnsureSuccessStatusCode();
        var audited = await response.Content.ReadFromJsonAsync<GeneratedDraftResponse>();
        Assert.NotNull(audited);
        Assert.NotNull(audited.AuditUpdatedAt);
        Assert.True(audited.AuditUpdatedAt >= firstAudit.AuditUpdatedAt);
        Assert.False(audited.IsClaimAuditStale);

        var audit = JsonSerializer.Deserialize<ClaimAuditResult>(audited.ClaimAudit, JsonOptions);
        Assert.NotNull(audit);
        Assert.Contains(
            audit.Claims,
            claim => claim.Status == "Supported" && claim.EvidenceIds.Any(id => id.StartsWith("match-dotnet", StringComparison.Ordinal)));
        Assert.Contains(audit.Claims, claim => claim.Status == "Unsupported" && claim.Text == "Led Kubernetes platform operations.");
        Assert.Contains(audit.Claims, claim => claim.Status == "NeedsReview" && claim.Text == "I may be a fit for the team.");

        var reopenedResponse = await client.GetAsync($"/api/applications/{draft.JobApplicationId}");
        reopenedResponse.EnsureSuccessStatusCode();
        var reopened = await reopenedResponse.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(reopened?.GeneratedDraft);
        Assert.Equal(audited.ClaimAudit, reopened.GeneratedDraft.ClaimAudit);
        Assert.False(reopened.GeneratedDraft.IsClaimAuditStale);
    }

    private static async Task<ApplicationResponse> CreateApplicationAsync(
        HttpClient client,
        string jobPostingText,
        string selectedLanguage = "")
    {
        var response = await client.PostAsJsonAsync(
            "/api/applications",
            new ApplicationRequest(
                "Fallback Company",
                "Fallback Role",
                null,
                null,
                "Draft",
                jobPostingText,
                null,
                selectedLanguage));

        response.EnsureSuccessStatusCode();
        var application = await response.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(application);
        Assert.NotEqual(Guid.Empty, application.Id);
        return application;
    }

    private static async Task<GeneratedDraftResponse> CreateGeneratedDraftAsync(HttpClient client)
    {
        await CreateProfileFactAsync(client, "Approved API work", "Approved", """[".NET"]""");
        var application = await CreateApplicationAsync(client, "We need .NET.");
        await client.PostAsync($"/api/applications/{application.Id}/analyze-job", null);
        var matchResponse = await client.PostAsync($"/api/applications/{application.Id}/match-evidence", null);
        Assert.True(matchResponse.IsSuccessStatusCode, await matchResponse.Content.ReadAsStringAsync());
        var matched = await matchResponse.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.NotNull(matched);
        await client.PutAsJsonAsync(
            $"/api/applications/{application.Id}/approved-evidence",
            new ApprovedEvidenceRequest(matched.EvidenceMatches));
        var draftResponse = await client.PostAsync($"/api/applications/{application.Id}/generate-draft", null);
        draftResponse.EnsureSuccessStatusCode();
        var draft = await draftResponse.Content.ReadFromJsonAsync<GeneratedDraftResponse>();
        Assert.NotNull(draft);
        return draft;
    }

    private static async Task MarkDraftAuditedAsync(TestApplicationFactory factory, Guid draftId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var draft = await db.GeneratedDrafts.FindAsync(draftId);
        Assert.NotNull(draft);
        draft.ClaimAudit = """{"status":"current"}""";
        draft.AuditUpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
    }

    private static async Task<ProfileFactResponse> CreateProfileFactAsync(
        HttpClient client,
        string title,
        string status,
        string technologies)
    {
        var response = await client.PostAsJsonAsync(
            "/api/profile/facts",
            new ProfileFactRequest(
                "Project",
                title,
                $"Evidence for {title}.",
                status,
                null,
                technologies,
                null,
                null));

        response.EnsureSuccessStatusCode();
        var fact = await response.Content.ReadFromJsonAsync<ProfileFactResponse>();
        Assert.NotNull(fact);
        return fact;
    }
}
