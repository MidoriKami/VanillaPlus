using KamiToolKit.Premade;
using Lumina.Excel.Sheets;

namespace VanillaPlus.Features.ActionHighlight;

public class ClassJobWrapper : IInfoNodeData {
    public ClassJob? ClassJob { get; }
    public bool IsRoleActions { get; }
    public bool IsGeneralSettings { get; }

    public ClassJobWrapper(ClassJob classJob) {
        ClassJob = classJob;
        IsRoleActions = false;
        IsGeneralSettings = false;
    }

    private ClassJobWrapper(bool isRoleActions, bool isGeneralSettings) {
        IsRoleActions = isRoleActions;
        IsGeneralSettings = isGeneralSettings;
    }

    public static ClassJobWrapper RoleActions 
        => new(true, false);

    public static ClassJobWrapper GeneralSettings 
        => new(false, true);

    public string GetLabel() {
        if (IsGeneralSettings) return "General Settings";
        if (IsRoleActions) return "Role Actions";
        return ClassJob!.Value.NameEnglish.ExtractText();
    }

    public string GetSubLabel() {
        if (IsGeneralSettings) return "CONFIG";
        if (IsRoleActions) return "ALL";
        return ClassJob!.Value.Abbreviation.ExtractText();
    }

    public uint? GetId()
        => null;

    public uint? GetIconId() {
        if (IsGeneralSettings) return 91178; // Config icon or similar
        if (IsRoleActions) return 62143;

        return ClassJob!.Value.IconId;
    }

    public string? GetTexturePath() 
        => null;

    public int Compare(IInfoNodeData other, string sortingMode) {
        if (other is not ClassJobWrapper otherWrapper) return 0;

        if (IsGeneralSettings) return -1;
        if (otherWrapper.IsGeneralSettings) return 1;

        if (IsRoleActions) return 1;
        if (otherWrapper.IsRoleActions) return -1;

        var priorityA = GetJobPriority(ClassJob!.Value);
        var priorityB = GetJobPriority(otherWrapper.ClassJob!.Value);

        if (priorityA != priorityB) return priorityA.CompareTo(priorityB);

        return ClassJob.Value.JobIndex.CompareTo(otherWrapper.ClassJob.Value.JobIndex);
    }

    private static int GetJobPriority(ClassJob job) {
        if (job.RowId == 36) return 6; // Blue Mage

        return job.Role switch {
            1 => 1, // Tank
            4 => 2, // Healer
            2 => 3, // Melee
            3 => job.PrimaryStat == 4 ? 5 : 4, // 3=Ranged, 4=INT(Caster), else PhysRanged
            _ => 7,
        };
    }
}

