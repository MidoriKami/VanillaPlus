using Lumina.Excel.Sheets;

namespace VanillaPlus.Features.ActionHighlight;

public record ActionCategory(ActionCategoryType Type, ClassJob? Job = null) {
    public string Name => Type switch {
        ActionCategoryType.General => "General Settings",
        ActionCategoryType.Role => "Role Actions",
        _ => Job?.NameEnglish.ExtractText() ?? "Unknown",
    };

    public string SubLabel => Type switch {
        ActionCategoryType.General => "CONFIG",
        ActionCategoryType.Role => "ALL",
        _ => Job?.Abbreviation.ExtractText() ?? string.Empty,
    };

    public uint IconId => Type switch {
        ActionCategoryType.General => 91178,
        ActionCategoryType.Role => 62143,
        _ => Job?.IconId ?? 0,
    };

    public static int Compare(ActionCategory left, ActionCategory right) {
        if (left.Type != right.Type) return left.Type.CompareTo(right.Type);

        if (left.Job.HasValue && right.Job.HasValue) {
            var priorityA = GetJobPriority(left.Job.Value);
            var priorityB = GetJobPriority(right.Job.Value);

            if (priorityA != priorityB) return priorityA.CompareTo(priorityB);
            return left.Job.Value.JobIndex.CompareTo(right.Job.Value.JobIndex);
        }

        return 0;
    }

    private static int GetJobPriority(ClassJob job) {
        if (job.RowId == 36) return 6; // Blue Mage
        return job.Role switch {
            1 => 1, // Tank
            4 => 2, // Healer
            2 => 3, // Melee
            3 => job.PrimaryStat == 4 ? 5 : 4, // Caster vs Phys Range
            _ => 7,
        };
    }
}
