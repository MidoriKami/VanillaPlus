using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VanillaPlus.Enums;

namespace VanillaPlus.Classes;

public class ModificationInfo {
    public required string DisplayName { get; init; }
    public required string Description { get; init; }
    public required string[] Authors { get; init; }
    public required ModificationType Type { get; init; }
    public ModificationSubType? SubType { get; init; }
    public List<string> Tags { get; init; } = [];
    public string? DisabledReason { get; init; }

    /// <summary>
    /// Compatibility Module prevents loading this GameModification if the
    /// associated plugin has the equivalent module enabled.
    /// </summary>
    public CompatibilityModule? CompatibilityModule { get; init; }

    public bool IsMatch(Regex searchRegex) {
        if (searchRegex.IsMatch(DisplayName)) return true;
        if (Authors.Any(searchRegex.IsMatch)) return true;
        if (searchRegex.IsMatch(Type.Description)) return true;
        if (SubType is not null && searchRegex.IsMatch(SubType.Description)) return true;
        if (Tags.Any(searchRegex.IsMatch)) return true;

        return false;
    }
}
