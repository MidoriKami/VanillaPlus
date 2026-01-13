using Lumina.Excel.Sheets;

namespace VanillaPlus.Extensions;

public static class ActionExtensions {
    extension(Action action) {
        /// <summary>
        /// Checks if the provided ClassJob is permitted to use this action based on the ClassJobCategory bitmask.
        /// </summary>
        public bool IsUsableByJob(ClassJob job) {
            var category = action.ClassJobCategory.Value;

            // Job flags start at offset 4 in the ClassJobCategory sheet.
            // RowId matches the bit position, so offset + 4 + job.RowId is the specific job flag.
            return category.ExcelPage.ReadBool(category.RowOffset + 4 + job.RowId);
        }
    }
}
