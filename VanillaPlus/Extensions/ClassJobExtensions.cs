using Lumina.Excel.Sheets;

namespace VanillaPlus.Extensions;

public static class ClassJobExtensions {
    public static bool IsGatherer(this ClassJob row)
        => row.ClassJobCategory.RowId is 32;

    public static bool IsCrafter(this ClassJob row)
        => row.ClassJobCategory.RowId is 33;

    public static bool IsNotCrafterGatherer(this ClassJob row)
        => !row.IsGatherer() && !row.IsCrafter();
}
