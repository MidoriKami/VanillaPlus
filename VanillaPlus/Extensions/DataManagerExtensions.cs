using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;

namespace VanillaPlus.Extensions;

public static class DataManagerExtensions {
    public static ReadOnlySeString GetAddonText(this IDataManager dataManager, uint id)
        => dataManager.GetExcelSheet<Addon>().GetRow(id).Text;

    public static IEnumerable<uint> GetManaUsingClassJobs(this IDataManager dataManager) {
        return dataManager.GetExcelSheet<Action>()
            .Where(action => action.ClassJob.RowId is not (0 or uint.MaxValue))
            .GroupBy(action => action.ClassJob.RowId)
            .ToDictionary(group => group.Key, group => group.Any(action => action.PrimaryCostType is 3 or 96))
            .Where(group => group.Value)
            .Select(group => group.Key);
    }
}
