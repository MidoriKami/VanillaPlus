using Lumina.Excel.Sheets;
using ExcelPage = Lumina.Excel.ExcelPage;

namespace VanillaPlus.Extensions;

public static unsafe class ClassJobCategoryExtensions {
    extension(ClassJobCategory row) {
        public Lumina.Excel.Collection<bool> ClassesJobs
            => new(row.ExcelPage, parentOffset: row.RowOffset, offset: row.RowOffset, &ClassJobCtor, size: row.ExcelPage.Module.GetSheet<ClassJob>().Count);
    }

    private static bool ClassJobCtor(ExcelPage page, uint parentOffset, uint offset, uint i)
        => page.ReadBool(offset + i + 4);
}
