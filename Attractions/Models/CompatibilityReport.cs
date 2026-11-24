using System.Text;

namespace Attractions.Models;

public enum RequirementEvaluationStatus
{
    Met,
    Unmet,
    Warning
}

public class RequirementCheckResult
{
    public int FeatureId { get; }
    public string FeatureName { get; }
    public RequirementEvaluationStatus Status { get; }
    public string RequiredValueText { get; }
    public string ParticipantValueText { get; }
    public string SourceElement { get; }
    public string Message { get; }

    public RequirementCheckResult(
        int featureId,
        string featureName,
        RequirementEvaluationStatus status,
        string requiredValueText,
        string participantValueText,
        string message,
        string sourceElement = "")
    {
        FeatureId = featureId;
        FeatureName = featureName ?? string.Empty;
        Status = status;
        RequiredValueText = requiredValueText ?? string.Empty;
        ParticipantValueText = participantValueText ?? string.Empty;
        Message = message ?? string.Empty;
        SourceElement = sourceElement ?? string.Empty;
    }

    public string FormattedMessage =>
        string.IsNullOrWhiteSpace(SourceElement)
            ? Message
            : $"[{SourceElement}] {Message}";
}

public class CompatibilityReport
{
    public string TargetName { get; }
    public string TargetTypeLabel { get; }
    public List<RequirementCheckResult> MetRequirements { get; }
    public List<RequirementCheckResult> UnmetRequirements { get; }
    public List<RequirementCheckResult> Warnings { get; }

    public CompatibilityReport(
        string targetName,
        string targetTypeLabel = "OFERTY",
        IEnumerable<RequirementCheckResult>? metRequirements = null,
        IEnumerable<RequirementCheckResult>? unmetRequirements = null,
        IEnumerable<RequirementCheckResult>? warnings = null)
    {
        TargetName = targetName ?? string.Empty;
        TargetTypeLabel = targetTypeLabel ?? "OFERTY";
        MetRequirements = metRequirements is not null ? new List<RequirementCheckResult>(metRequirements) : new List<RequirementCheckResult>();
        UnmetRequirements = unmetRequirements is not null ? new List<RequirementCheckResult>(unmetRequirements) : new List<RequirementCheckResult>();
        Warnings = warnings is not null ? new List<RequirementCheckResult>(warnings) : new List<RequirementCheckResult>();
    }

    public bool IsCompatible => UnmetRequirements.Count == 0;

    public IReadOnlyList<string> IncompatibilityReasons =>
        UnmetRequirements
            .Select(r => r.FormattedMessage)
            .ToList();

    public string FormatSummary(bool includeMetRequirements = true)
    {
        var sb = new StringBuilder();

        if (IsCompatible)
        {
            sb.AppendLine($"MOŻNA SKORZYSTAĆ Z {TargetTypeLabel}: {TargetName}");
        }
        else
        {
            sb.AppendLine($"NIE MOŻNA SKORZYSTAĆ Z {TargetTypeLabel}: {TargetName}");
            AppendSection(sb, "Powody:", UnmetRequirements);
        }

        if (Warnings.Count > 0)
        {
            AppendSection(sb, "Uwagi / wymagania do weryfikacji:", Warnings);
        }

        if (includeMetRequirements && MetRequirements.Count > 0)
        {
            AppendSection(sb, "Spełnione wymagania:", MetRequirements);
        }

        return sb.ToString();
    }

    private static void AppendSection(StringBuilder sb, string heading, IReadOnlyCollection<RequirementCheckResult> items)
    {
        sb.AppendLine();
        sb.AppendLine(heading);
        foreach (var item in items)
        {
            sb.AppendLine($"- {item.FormattedMessage}");
        }
    }
}
