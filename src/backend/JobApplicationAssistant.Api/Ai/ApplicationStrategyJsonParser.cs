using System.Text.Json;

namespace JobApplicationAssistant.Api.Ai;

internal static class ApplicationStrategyJsonParser
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static ApplicationStrategyResult Parse(
        string responseText,
        ApplicationStrategyInput input,
        string providerName,
        int attemptCount)
    {
        ApplicationStrategyResponse? payload;
        try
        {
            payload = JsonSerializer.Deserialize<ApplicationStrategyResponse>(ExtractJsonObject(responseText), JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new AiInvalidOutputException($"{providerName} returned malformed application strategy JSON.", attemptCount, exception);
        }

        if (payload is null ||
            payload.PrimaryAngles is null ||
            payload.SecondaryAngles is null ||
            payload.GapHandlingGuidance is null ||
            payload.ClaimsToAvoid is null ||
            string.IsNullOrWhiteSpace(payload.ToneGuidance) ||
            payload.DraftOutline is null)
        {
            throw Invalid(providerName, attemptCount);
        }

        var approvedEvidenceIds = input.ApprovedEvidence
            .Select(evidence => evidence.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var validProfileFactIds = input.ApprovedEvidence
            .Select(evidence => evidence.ProfileFactId)
            .Concat(ProfileFactIdsFromBrief(input.CandidateFitBrief))
            .ToHashSet();
        var unmatchedRequirementIds = input.UnmatchedRequirements
            .Select(requirement => requirement.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return new ApplicationStrategyResult(
            payload.PrimaryAngles.Select(angle => ValidateAngle(angle, approvedEvidenceIds, validProfileFactIds, providerName, attemptCount)).ToList(),
            payload.SecondaryAngles.Select(angle => ValidateAngle(angle, approvedEvidenceIds, validProfileFactIds, providerName, attemptCount)).ToList(),
            payload.GapHandlingGuidance.Select(gap => ValidateGapGuidance(gap, unmatchedRequirementIds, providerName, attemptCount)).ToList(),
            payload.ClaimsToAvoid.Select(claim => ValidateClaimToAvoid(claim, providerName, attemptCount)).ToList(),
            payload.ToneGuidance.Trim(),
            payload.DraftOutline.Select(item => ValidateOutlineItem(item, approvedEvidenceIds, validProfileFactIds, providerName, attemptCount)).ToList())
        {
            AttemptCount = attemptCount
        };
    }

    private static ApplicationStrategyAngle ValidateAngle(
        ApplicationStrategyAngleResponse? angle,
        ISet<string> approvedEvidenceIds,
        ISet<Guid> validProfileFactIds,
        string providerName,
        int attemptCount)
    {
        if (angle is null ||
            string.IsNullOrWhiteSpace(angle.Title) ||
            string.IsNullOrWhiteSpace(angle.Rationale) ||
            angle.EvidenceIds is null ||
            angle.ProfileFactIds is null)
        {
            throw Invalid(providerName, attemptCount);
        }

        return new ApplicationStrategyAngle(
            angle.Title.Trim(),
            angle.Rationale.Trim(),
            ValidateEvidenceIds(angle.EvidenceIds, approvedEvidenceIds, providerName, attemptCount),
            ValidateProfileFactIds(angle.ProfileFactIds, validProfileFactIds, providerName, attemptCount));
    }

    private static ApplicationStrategyGapGuidance ValidateGapGuidance(
        ApplicationStrategyGapGuidanceResponse? gap,
        ISet<string> unmatchedRequirementIds,
        string providerName,
        int attemptCount)
    {
        if (gap is null ||
            string.IsNullOrWhiteSpace(gap.UnmatchedRequirementId) ||
            string.IsNullOrWhiteSpace(gap.Guidance) ||
            !unmatchedRequirementIds.Contains(gap.UnmatchedRequirementId.Trim()))
        {
            throw Invalid(providerName, attemptCount);
        }

        return new ApplicationStrategyGapGuidance(gap.UnmatchedRequirementId.Trim(), gap.Guidance.Trim());
    }

    private static ApplicationStrategyClaimToAvoid ValidateClaimToAvoid(
        ApplicationStrategyClaimToAvoidResponse? claim,
        string providerName,
        int attemptCount)
    {
        if (claim is null ||
            string.IsNullOrWhiteSpace(claim.Claim) ||
            string.IsNullOrWhiteSpace(claim.Reason))
        {
            throw Invalid(providerName, attemptCount);
        }

        return new ApplicationStrategyClaimToAvoid(claim.Claim.Trim(), claim.Reason.Trim());
    }

    private static ApplicationStrategyOutlineItem ValidateOutlineItem(
        ApplicationStrategyOutlineItemResponse? item,
        ISet<string> approvedEvidenceIds,
        ISet<Guid> validProfileFactIds,
        string providerName,
        int attemptCount)
    {
        if (item is null ||
            string.IsNullOrWhiteSpace(item.Section) ||
            string.IsNullOrWhiteSpace(item.Guidance) ||
            item.EvidenceIds is null ||
            item.ProfileFactIds is null)
        {
            throw Invalid(providerName, attemptCount);
        }

        return new ApplicationStrategyOutlineItem(
            item.Section.Trim(),
            item.Guidance.Trim(),
            ValidateEvidenceIds(item.EvidenceIds, approvedEvidenceIds, providerName, attemptCount),
            ValidateProfileFactIds(item.ProfileFactIds, validProfileFactIds, providerName, attemptCount));
    }

    private static IReadOnlyList<string> ValidateEvidenceIds(
        IReadOnlyList<string> rawIds,
        ISet<string> approvedEvidenceIds,
        string providerName,
        int attemptCount)
    {
        var evidenceIds = new List<string>();
        foreach (var rawId in rawIds)
        {
            if (string.IsNullOrWhiteSpace(rawId) || !approvedEvidenceIds.Contains(rawId.Trim()))
            {
                throw Invalid(providerName, attemptCount);
            }

            if (!evidenceIds.Contains(rawId.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                evidenceIds.Add(rawId.Trim());
            }
        }

        return evidenceIds;
    }

    private static IReadOnlyList<Guid> ValidateProfileFactIds(
        IReadOnlyList<string> rawIds,
        ISet<Guid> validProfileFactIds,
        string providerName,
        int attemptCount)
    {
        var profileFactIds = new List<Guid>();
        foreach (var rawId in rawIds)
        {
            if (string.IsNullOrWhiteSpace(rawId) ||
                !Guid.TryParse(rawId, out var profileFactId) ||
                !validProfileFactIds.Contains(profileFactId))
            {
                throw Invalid(providerName, attemptCount);
            }

            if (!profileFactIds.Contains(profileFactId))
            {
                profileFactIds.Add(profileFactId);
            }
        }

        return profileFactIds;
    }

    private static IEnumerable<Guid> ProfileFactIdsFromBrief(CandidateFitBriefResult brief) =>
        brief.SkillGroups
            .SelectMany(group => group.Items)
            .Concat(brief.Competencies)
            .Concat(brief.RelevantProjects)
            .Concat(brief.TransferableStrengths)
            .Concat(brief.RiskNotes)
            .SelectMany(item => item.SupportingProfileFactIds);

    private static AiInvalidOutputException Invalid(string providerName, int attemptCount) =>
        new($"{providerName} returned structurally invalid application strategy JSON.", attemptCount);

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

    private sealed record ApplicationStrategyResponse(
        IReadOnlyList<ApplicationStrategyAngleResponse> PrimaryAngles,
        IReadOnlyList<ApplicationStrategyAngleResponse> SecondaryAngles,
        IReadOnlyList<ApplicationStrategyGapGuidanceResponse> GapHandlingGuidance,
        IReadOnlyList<ApplicationStrategyClaimToAvoidResponse> ClaimsToAvoid,
        string ToneGuidance,
        IReadOnlyList<ApplicationStrategyOutlineItemResponse> DraftOutline);

    private sealed record ApplicationStrategyAngleResponse(
        string Title,
        string Rationale,
        IReadOnlyList<string> EvidenceIds,
        IReadOnlyList<string> ProfileFactIds);

    private sealed record ApplicationStrategyGapGuidanceResponse(
        string UnmatchedRequirementId,
        string Guidance);

    private sealed record ApplicationStrategyClaimToAvoidResponse(
        string Claim,
        string Reason);

    private sealed record ApplicationStrategyOutlineItemResponse(
        string Section,
        string Guidance,
        IReadOnlyList<string> EvidenceIds,
        IReadOnlyList<string> ProfileFactIds);
}
