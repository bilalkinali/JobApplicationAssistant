using System.Text.Json;

namespace JobApplicationAssistant.Api.Ai;

internal static class CandidateFitBriefJsonParser
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static CandidateFitBriefResult Parse(
        string responseText,
        CandidateFitBriefInput input,
        string providerName,
        int attemptCount)
    {
        CandidateFitBriefResponse? payload;
        try
        {
            payload = JsonSerializer.Deserialize<CandidateFitBriefResponse>(responseText, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new AiInvalidOutputException($"{providerName} returned malformed candidate fit brief JSON.", attemptCount, exception);
        }

        if (payload is null ||
            string.IsNullOrWhiteSpace(payload.CandidateSummary) ||
            payload.SkillGroups is null ||
            payload.Competencies is null ||
            payload.RelevantProjects is null ||
            payload.TransferableStrengths is null ||
            payload.RiskNotes is null)
        {
            throw new AiInvalidOutputException($"{providerName} returned structurally invalid candidate fit brief JSON.", attemptCount);
        }

        var approvedFactIds = input.ApprovedProfileFacts.Select(fact => fact.Id).ToHashSet();

        return new CandidateFitBriefResult(
            payload.CandidateSummary.Trim(),
            payload.SkillGroups.Select(group => ValidateSkillGroup(group, approvedFactIds, providerName, attemptCount)).ToList(),
            payload.Competencies.Select(item => ValidateItem(item, approvedFactIds, providerName, attemptCount, allowEmptySupport: false)).ToList(),
            payload.RelevantProjects.Select(item => ValidateItem(item, approvedFactIds, providerName, attemptCount, allowEmptySupport: false)).ToList(),
            payload.TransferableStrengths.Select(item => ValidateItem(item, approvedFactIds, providerName, attemptCount, allowEmptySupport: false)).ToList(),
            payload.RiskNotes.Select(item => ValidateItem(item, approvedFactIds, providerName, attemptCount, allowEmptySupport: true)).ToList())
        {
            AttemptCount = attemptCount
        };
    }

    private static CandidateFitSkillGroup ValidateSkillGroup(
        CandidateFitSkillGroupResponse? group,
        ISet<Guid> approvedFactIds,
        string providerName,
        int attemptCount)
    {
        if (group is null || string.IsNullOrWhiteSpace(group.Name) || group.Items is null)
        {
            throw Invalid(providerName, attemptCount);
        }

        return new CandidateFitSkillGroup(
            group.Name.Trim(),
            group.Items.Select(item => ValidateItem(item, approvedFactIds, providerName, attemptCount, allowEmptySupport: false)).ToList());
    }

    private static CandidateFitBriefItem ValidateItem(
        CandidateFitBriefItemResponse? item,
        ISet<Guid> approvedFactIds,
        string providerName,
        int attemptCount,
        bool allowEmptySupport)
    {
        if (item is null ||
            string.IsNullOrWhiteSpace(item.Title) ||
            string.IsNullOrWhiteSpace(item.Summary) ||
            item.SupportingProfileFactIds is null)
        {
            throw Invalid(providerName, attemptCount);
        }

        var supportingIds = new List<Guid>();
        foreach (var rawId in item.SupportingProfileFactIds)
        {
            if (string.IsNullOrWhiteSpace(rawId) ||
                !Guid.TryParse(rawId, out var profileFactId) ||
                !approvedFactIds.Contains(profileFactId))
            {
                throw Invalid(providerName, attemptCount);
            }

            if (!supportingIds.Contains(profileFactId))
            {
                supportingIds.Add(profileFactId);
            }
        }

        if (!allowEmptySupport && supportingIds.Count == 0)
        {
            throw Invalid(providerName, attemptCount);
        }

        return new CandidateFitBriefItem(
            item.Title.Trim(),
            item.Summary.Trim(),
            supportingIds);
    }

    private static AiInvalidOutputException Invalid(string providerName, int attemptCount) =>
        new($"{providerName} returned structurally invalid candidate fit brief JSON.", attemptCount);

    private sealed record CandidateFitBriefResponse(
        string CandidateSummary,
        IReadOnlyList<CandidateFitSkillGroupResponse> SkillGroups,
        IReadOnlyList<CandidateFitBriefItemResponse> Competencies,
        IReadOnlyList<CandidateFitBriefItemResponse> RelevantProjects,
        IReadOnlyList<CandidateFitBriefItemResponse> TransferableStrengths,
        IReadOnlyList<CandidateFitBriefItemResponse> RiskNotes);

    private sealed record CandidateFitSkillGroupResponse(
        string Name,
        IReadOnlyList<CandidateFitBriefItemResponse> Items);

    private sealed record CandidateFitBriefItemResponse(
        string Title,
        string Summary,
        IReadOnlyList<string> SupportingProfileFactIds);
}
