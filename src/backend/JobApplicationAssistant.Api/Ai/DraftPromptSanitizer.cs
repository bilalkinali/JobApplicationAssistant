namespace JobApplicationAssistant.Api.Ai;

internal static class DraftPromptSanitizer
{
    private static readonly string[] ProfileFactIdPropertyNames = ["profileFactIds", "supportingProfileFactIds"];

    public static DraftApplicationStrategyContext? SanitizeApplicationStrategy(ApplicationStrategyResult? strategy)
    {
        if (strategy is null)
        {
            return null;
        }

        return new DraftApplicationStrategyContext(
            strategy.PrimaryAngles.Select(SanitizeAngle).ToList(),
            strategy.SecondaryAngles.Select(SanitizeAngle).ToList(),
            strategy.GapHandlingGuidance,
            strategy.ClaimsToAvoid,
            strategy.ToneGuidance,
            strategy.DraftOutline.Select(SanitizeOutlineItem).ToList());
    }

    private static DraftApplicationStrategyAngle SanitizeAngle(ApplicationStrategyAngle angle) =>
        new(angle.Title, angle.Rationale, angle.EvidenceIds);

    private static DraftApplicationStrategyOutlineItem SanitizeOutlineItem(ApplicationStrategyOutlineItem item) =>
        new(item.Section, item.Guidance, item.EvidenceIds);

    public static string RemoveProfileFactIdProperties(string prompt)
    {
        foreach (var propertyName in ProfileFactIdPropertyNames)
        {
            prompt = System.Text.RegularExpressions.Regex.Replace(
                prompt,
                $"""\"{propertyName}\"\s*:\s*\[[^\]]*\]\s*,""",
                string.Empty,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            prompt = System.Text.RegularExpressions.Regex.Replace(
                prompt,
                $"""\s*,\s*\"{propertyName}\"\s*:\s*\[[^\]]*\]""",
                string.Empty,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            prompt = System.Text.RegularExpressions.Regex.Replace(
                prompt,
                $"""\\u0022{propertyName}\\u0022\s*:\s*\[[^\]]*\]\s*,""",
                string.Empty,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            prompt = System.Text.RegularExpressions.Regex.Replace(
                prompt,
                $"""\s*,\s*\\u0022{propertyName}\\u0022\s*:\s*\[[^\]]*\]""",
                string.Empty,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            prompt = System.Text.RegularExpressions.Regex.Replace(
                prompt,
                $"""\\\\u0022{propertyName}\\\\u0022\s*:\s*\[[^\]]*\]\s*,""",
                string.Empty,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            prompt = System.Text.RegularExpressions.Regex.Replace(
                prompt,
                $"""\s*,\s*\\\\u0022{propertyName}\\\\u0022\s*:\s*\[[^\]]*\]""",
                string.Empty,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return prompt;
    }
}

internal sealed record DraftApplicationStrategyContext(
    IReadOnlyList<DraftApplicationStrategyAngle> PrimaryAngles,
    IReadOnlyList<DraftApplicationStrategyAngle> SecondaryAngles,
    IReadOnlyList<ApplicationStrategyGapGuidance> GapHandlingGuidance,
    IReadOnlyList<ApplicationStrategyClaimToAvoid> ClaimsToAvoid,
    string ToneGuidance,
    IReadOnlyList<DraftApplicationStrategyOutlineItem> DraftOutline);

internal sealed record DraftApplicationStrategyAngle(
    string Title,
    string Rationale,
    IReadOnlyList<string> EvidenceIds);

internal sealed record DraftApplicationStrategyOutlineItem(
    string Section,
    string Guidance,
    IReadOnlyList<string> EvidenceIds);
