using System.Linq;
using Dalamud.Game.ClientState.Fates;
using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.NativeElements.Addons;

namespace VanillaPlus.Features.FateListWindow;

public class FateListAddon : NodeListAddon<IFate, FateListItemNode> {
    protected override unsafe void OnUpdate(AtkUnitBase* addon) {
        base.OnUpdate(addon);

        ListNode?.OptionsList = Services.FateTable
            .Where(fate => fate is { State: FateState.Running or FateState.Preparation })
            .OrderBy(fate => fate.TimeRemaining)
            .ToList();
    }
}
