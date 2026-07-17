using System;
using System.Linq;
using Dalamud.Game.ClientState.Fates;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.BaseTypes;
using KamiToolKit.Nodes;

namespace VanillaPlus.Features.FateListWindow;

public class FateListAddon : NativeAddon {
    protected override unsafe void OnSetup(AtkUnitBase* addon, Span<AtkValue> atkValueSpan) {
        base.OnSetup(addon, atkValueSpan);

        fateListNode = new ListNode<IFate, FateListItemNode> {
            Position = ContentStartPosition,
            Size = ContentSize,
            AutoResetScroll = false,
            ItemSpacing = 3.0f,
            OptionsList = [],
        };
        fateListNode.AttachNode(this);
    }

    protected override unsafe void OnUpdate(AtkUnitBase* addon) {
        base.OnUpdate(addon);

        fateListNode?.OptionsList = IFateTable.Get()
            .Where(fate => fate is { State: FateState.Running or FateState.Preparing })
            .OrderBy(fate => fate.TimeRemaining)
            .ToList();
    }

    private ListNode<IFate, FateListItemNode>? fateListNode;
}
