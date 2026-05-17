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
            payload = JsonSerializer.Deserialize<CandidateFitBriefResponse>(ExtractJsonObject(responseText), JsonOptions);
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
        var riskNotes = payload.RiskNotes
            .Select(item => ValidateItem(item, approvedFactIds, providerName, attemptCount, allowEmptySupport: true))
            .ToList();
        var skillGroups = payload.SkillGroups
            .Select(group => ValidateSkillGroup(group, approvedFactIds, providerName, attemptCount, riskNotes))
            .Where(group => group.Items.Count > 0)
            .ToList();

        return new CandidateFitBriefResult(
            payload.CandidateSummary.Trim(),
            skillGroups,
            ValidateSupportedItems(payload.Competencies, approvedFactIds, providerName, attemptCount, riskNotes),
            ValidateSupportedItems(payload.RelevantProjects, approvedFactIds, providerName, attemptCount, riskNotes),
            ValidateSupportedItems(payload.TransferableStrengths, approvedFactIds, providerName, attemptCount, riskNotes),
            riskNotes)
        {
            AttemptCount = attemptCount
        };
    }

    private static CandidateFitSkillGroup ValidateSkillGroup(
        CandidateFitSkillGroupResponse? group,
        ISet<Guid> approvedFactIds,
        string providerName,
        int attemptCount,
        List<CandidateFitBriefItem> riskNotes)
    {
        if (group is null || string.IsNullOrWhiteSpace(group.Name) || group.Items is null)
        {
            throw Invalid(providerName, attemptCount);
        }

        return new CandidateFitSkillGroup(
            group.Name.Trim(),
            ValidateSupportedItems(group.Items, approvedFactIds, providerName, attemptCount, riskNotes));
    }

    private static List<CandidateFitBriefItem> ValidateSupportedItems(
        IReadOnlyList<CandidateFitBriefItemResponse> items,
        ISet<Guid> approvedFactIds,
        string providerName,
        int attemptCount,
        List<CandidateFitBriefItem> riskNotes)
    {
        var supportedItems = new List<CandidateFitBriefItem>();
        foreach (var item in items)
        {
            var validated = ValidateItem(item, approvedFactIds, providerName, attemptCount, allowEmptySupport: true);
            if (validated.SupportingProfileFactIds.Count == 0)
            {
                riskNotes.Add(validated);
            }
            else
            {
                supportedItems.Add(validated);
            }
        }

        return supportedItems;
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

    private static string ExtractJsonObject(string responseText)
    {
        var trimmed = responseText.Trim();
        if (trimmed.StartsWith('{') && trimmed.EndsWith('}'))
        {
            return trimmed;
        }

        var start = trimmed.IndexOf('{');
        if (start < 0)
        {
            return trimmed;
        }

        var depth = 0;
        var inString = false;
        var escaped = false;
        for (var index = start; index < trimmed.Length; index++)
        {
            var current = trimmed[index];

            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (current == '\\' && inString)
            {
                escaped = true;
                continue;
            }

            if (current == '"')
            {
                inString = !inString;
                continue;
            }

            if (inString)
            {
                continue;
            }

            if (current == '{')
            {
                depth++;
            }
            else if (current == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return trimmed[start..(index + 1)];
                }
            }
        }

        return trimmed;
    }

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
