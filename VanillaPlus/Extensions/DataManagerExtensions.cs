using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;

namespace VanillaPlus.Extensions;

public static class DataManagerExtensions {
    extension(IDataManager dataManager) {
        public ReadOnlySeString GetAddonText(uint id)
            => dataManager.GetExcelSheet<Addon>().GetRow(id).Text;

        public IEnumerable<uint> GetManaUsingClassJobs() {
            return dataManager.GetExcelSheet<Action>()
                .Where(action => action.ClassJob.RowId is not (0 or uint.MaxValue))
                .GroupBy(action => action.ClassJob.RowId)
                .ToDictionary(group => group.Key, group => group.Any(action => action.PrimaryCostType is 3 or 96))
                .Where(group => group.Value)
                .Select(group => group.Key);
        }

        public IEnumerable<Action> GetRoleActions() {
            return dataManager.GetExcelSheet<Action>()!
                .Where(a => a.IsRoleAction && a.ClassJobLevel != 0);
        }

        public ClassJob GetClassJobById(uint id)
            => dataManager.GetExcelSheet<ClassJob>().GetRow(id);

        public Item GetItem(uint id)
            => dataManager.GetExcelSheet<Item>().GetRow(id);

        public IEnumerable<Item> GetCurrencyItems()
            => dataManager.GetExcelSheet<Item>()
                .Where(item => item is { Name.IsEmpty: false, ItemUICategory.RowId: 100 } or { RowId: >= 1 and < 100, Name.IsEmpty: false });
    }
}
