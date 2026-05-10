using JobApplicationAssistant.Api.Ai;
using JobApplicationAssistant.Api.Domain;
using Xunit;

namespace JobApplicationAssistant.Api.Tests.Ai;

public sealed class FakeAiProviderTests
{
    [Fact]
    public async Task AnalyzeJobAsync_extracts_labeled_company_role_language_and_keyword_signals()
    {
        var provider = new FakeAiProvider();
        var input = new JobAnalysisInput(
            "Fallback Company",
            "Fallback Role",
            null,
            """
            Company: Northwind
            Role: Full-stack Developer

            We need C#, .NET, React, TypeScript, PostgreSQL, REST APIs, Docker, and Kubernetes.
            """);

        var result = await provider.AnalyzeJobAsync(input, CancellationToken.None);

        Assert.Equal("Northwind", result.CompanyName);
        Assert.Equal("Full-stack Developer", result.RoleTitle);
        Assert.Equal("English", result.DetectedLanguage);
        Assert.Equal("English", result.SelectedLanguage);
        Assert.Contains(".NET", result.JobSignals.RequiredSkills);
        Assert.Contains("C#", result.JobSignals.RequiredSkills);
        Assert.Contains("React", result.JobSignals.RequiredSkills);
        Assert.Contains("TypeScript", result.JobSignals.RequiredSkills);
        Assert.Contains("PostgreSQL", result.JobSignals.RequiredSkills);
        Assert.Contains("Docker", result.JobSignals.PreferredSkills);
        Assert.Contains("Kubernetes", result.JobSignals.PreferredSkills);
        Assert.Contains("REST APIs", result.JobSignals.Responsibilities);
    }

    [Fact]
    public async Task AnalyzeJobAsync_uses_requested_selected_language_over_detected_language()
    {
        var provider = new FakeAiProvider();
        var input = new JobAnalysisInput(
            "Contoso",
            "Backend Developer",
            "English",
            "Vi soeger en udvikler med erfaring i .NET og React i Koebenhavn.");

        var result = await provider.AnalyzeJobAsync(input, CancellationToken.None);

        Assert.Equal("Danish", result.DetectedLanguage);
        Assert.Equal("English", result.SelectedLanguage);
    }

    [Fact]
    public async Task MatchEvidenceAsync_matches_signals_against_profile_fact_text_and_json_fields()
    {
        var provider = new FakeAiProvider();
        var profileFactId = Guid.NewGuid();
        var signals = new[]
        {
            new JobSignal("dotnet", ".NET", "RequiredSkill", [".net", "asp.net", "dotnet"]),
            new JobSignal("react", "React", "RequiredSkill", ["react"]),
            new JobSignal("postgresql", "PostgreSQL", "RequiredSkill", ["postgresql", "postgres"])
        };
        var approvedFacts = new[]
        {
            new ProfileFact
            {
                Id = profileFactId,
                Type = "Project",
                Title = "Portfolio management platform",
                Summary = "Built a backend API with ASP.NET Core.",
                Status = ProfileFactStatus.Approved,
                Technologies = """["React", "PostgreSQL"]""",
                AllowedClaims = """["Delivered .NET services"]"""
            }
        };

        var result = await provider.MatchEvidenceAsync(
            new EvidenceMatchInput(signals, approvedFacts),
            CancellationToken.None);

        Assert.Empty(result.UnmatchedRequirements);
        Assert.Collection(
            result.EvidenceMatches.OrderBy(match => match.Signal).ToList(),
            match =>
            {
                Assert.Equal(".NET", match.Signal);
                Assert.Equal(profileFactId, match.ProfileFactId);
                Assert.Contains(".net", match.MatchedTerms, StringComparer.OrdinalIgnoreCase);
            },
            match =>
            {
                Assert.Equal("PostgreSQL", match.Signal);
                Assert.Equal(profileFactId, match.ProfileFactId);
                Assert.Contains("postgresql", match.MatchedTerms, StringComparer.OrdinalIgnoreCase);
            },
            match =>
            {
                Assert.Equal("React", match.Signal);
                Assert.Equal(profileFactId, match.ProfileFactId);
                Assert.Contains("react", match.MatchedTerms, StringComparer.OrdinalIgnoreCase);
            });
    }

    [Fact]
    public async Task MatchEvidenceAsync_records_unmatched_requirement_when_no_approved_fact_supports_signal()
    {
        var provider = new FakeAiProvider();
        var signals = new[]
        {
            new JobSignal("kubernetes", "Kubernetes", "PreferredSkill", ["kubernetes", "k8s"])
        };
        var approvedFacts = new[]
        {
            new ProfileFact
            {
                Id = Guid.NewGuid(),
                Type = "Experience",
                Title = "Frontend work",
                Summary = "Built React interfaces.",
                Status = ProfileFactStatus.Approved,
                Technologies = """["React", "TypeScript"]"""
            }
        };

        var result = await provider.MatchEvidenceAsync(
            new EvidenceMatchInput(signals, approvedFacts),
            CancellationToken.None);

        Assert.Empty(result.EvidenceMatches);
        var unmatched = Assert.Single(result.UnmatchedRequirements);
        Assert.Equal("unmatched-kubernetes", unmatched.Id);
        Assert.Equal("kubernetes", unmatched.SignalId);
        Assert.Equal("Kubernetes", unmatched.Requirement);
        Assert.Equal("PreferredSkill", unmatched.Category);
        Assert.Contains("interest to learn", unmatched.Recommendation);
    }

    [Fact]
    public async Task GenerateDraftAsync_creates_deterministic_text_from_approved_evidence_and_unmatched_requirements()
    {
        var provider = new FakeAiProvider();
        var approvedEvidence = new[]
        {
            new EvidenceMatch(
                "match-dotnet",
                "dotnet",
                ".NET",
                "RequiredSkill",
                Guid.NewGuid(),
                "Approved API work",
                "Built ASP.NET Core APIs backed by PostgreSQL.",
                [".net"])
        };
        var unmatchedRequirements = new[]
        {
            new UnmatchedRequirement(
                "unmatched-kubernetes",
                "kubernetes",
                "Kubernetes",
                "PreferredSkill",
                "Mention Kubernetes as an interest to learn only if the posting makes it relevant.")
        };
        var input = new DraftGenerationInput(
            "Northwind",
            "Full-stack Developer",
            "English",
            "Bilal Kinali",
            "direct and specific",
            approvedEvidence,
            unmatchedRequirements);

        var first = await provider.GenerateDraftAsync(input, CancellationToken.None);
        var second = await provider.GenerateDraftAsync(input, CancellationToken.None);

        Assert.Equal(
            System.Text.Json.JsonSerializer.Serialize(first),
            System.Text.Json.JsonSerializer.Serialize(second));
        Assert.Contains("Northwind", first.CoverLetterText);
        Assert.Contains("Full-stack Developer", first.CoverLetterText);
        Assert.Contains("direct and specific", first.CoverLetterText);
        Assert.Contains("Approved API work", first.CoverLetterText);
        Assert.Contains("Built ASP.NET Core APIs backed by PostgreSQL.", first.CoverLetterText);
        Assert.Contains("Kubernetes", first.CoverLetterText);
        Assert.Contains("area to learn", first.CoverLetterText);
        Assert.Contains("Approved API work", first.ShortMotivationText);
    }

    [Fact]
    public async Task AuditClaimsAsync_classifies_supported_unsupported_and_needs_review_claims_deterministically()
    {
        var provider = new FakeAiProvider();
        var approvedEvidence = new[]
        {
            new EvidenceMatch(
                "match-dotnet",
                "dotnet",
                ".NET",
                "RequiredSkill",
                Guid.NewGuid(),
                "Approved API work",
                "Built ASP.NET Core APIs backed by PostgreSQL.",
                [".net"])
        };
        var input = new ClaimAuditInput(
            """
            Built ASP.NET Core APIs backed by PostgreSQL.
            Led Kubernetes platform operations.
            I may be a fit for the team.
            """,
            "",
            approvedEvidence);

        var first = await provider.AuditClaimsAsync(input, CancellationToken.None);
        var second = await provider.AuditClaimsAsync(input, CancellationToken.None);

        Assert.Equal(
            System.Text.Json.JsonSerializer.Serialize(first),
            System.Text.Json.JsonSerializer.Serialize(second));
        Assert.Collection(
            first.Claims,
            claim =>
            {
                Assert.Equal("Supported", claim.Status);
                Assert.Equal("Built ASP.NET Core APIs backed by PostgreSQL.", claim.Text);
                Assert.Contains("match-dotnet", claim.EvidenceIds);
            },
            claim =>
            {
                Assert.Equal("Unsupported", claim.Status);
                Assert.Equal("Led Kubernetes platform operations.", claim.Text);
                Assert.Empty(claim.EvidenceIds);
            },
            claim =>
            {
                Assert.Equal("NeedsReview", claim.Status);
                Assert.Equal("I may be a fit for the team.", claim.Text);
                Assert.Empty(claim.EvidenceIds);
            });
    }
}
