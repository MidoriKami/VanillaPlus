using Lumina.Excel.Sheets;

namespace VanillaPlus.Extensions;

public static class ClassJobExtensions {
    extension(ClassJob row) {
        public bool IsGatherer => row.ClassJobCategory.RowId is 32;
        public bool IsCrafter => row.ClassJobCategory.RowId is 33;
        public bool IsNotCrafterGatherer => row is { IsGatherer: false, IsCrafter: false };
        public uint IconId => row.RowId + 62000;
    }
}
