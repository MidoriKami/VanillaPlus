using System.Linq;
using Dalamud.Game.ClientState.Fates;
using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.NativeElements.Addons;

namespace VanillaPlus.Features.FateListWindow;

public class FateListAddon : NodeListAddon<IFate, FateListItemNode> {
    private int? lastFateCount;

    protected override unsafe void OnUpdate(AtkUnitBase* addon) {
        base.OnUpdate(addon);

        if (lastFateCount != Services.FateTable.Count) {
            ListNode?.OptionsList = Services.FateTable
                .Where(fate => fate is { State: FateState.Running or FateState.Preparation })
                .OrderBy(fate => fate.TimeRemaining)
                .ToList();
            
            lastFateCount = Services.FateTable.Count;
        }
    }

    protected override unsafe void OnFinalize(AtkUnitBase* addon) {
        base.OnFinalize(addon);

        lastFateCount = null;
    }
}
