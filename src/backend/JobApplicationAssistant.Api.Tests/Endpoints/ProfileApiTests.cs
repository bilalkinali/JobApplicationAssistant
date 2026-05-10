using System.Net;
using System.Net.Http.Json;
using JobApplicationAssistant.Api.Contracts;
using JobApplicationAssistant.Api.Tests.Support;
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
}
