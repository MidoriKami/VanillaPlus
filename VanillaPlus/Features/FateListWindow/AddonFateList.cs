using System;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Fates;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Addon;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.FateListWindow;

public class AddonFateList(AddonConfig config) : NativeAddon {

    private ScrollingAreaNode<OrderedVerticalListNode<FateEntryNode, long>>? scrollingAreaNode;
    
    private OrderedVerticalListNode<FateEntryNode, long> FateListNode => scrollingAreaNode?.ContentNode ?? throw new Exception("Invalid List Node");

    protected override unsafe void OnSetup(AtkUnitBase* addon) {
        scrollingAreaNode = new ScrollingAreaNode<OrderedVerticalListNode<FateEntryNode, long>> {
            Position = ContentStartPosition,
            Size = ContentSize,
            IsVisible = true,
            ContentHeight = 100,
        };
        scrollingAreaNode.ContentNode.FitContents = true;
        scrollingAreaNode.ContentNode.OrderSelector = node => node.Fate.TimeRemaining;
        AttachNode(scrollingAreaNode);
    }

    protected override unsafe void OnUpdate(AtkUnitBase* addon) {
        if (scrollingAreaNode is null) return;
        
        var validFates = Services.FateTable.Where(fate => fate is { State: FateState.Running }).ToList();
        FateListNode.SyncWithListData(validFates, node => node.Fate, data => new FateEntryNode {
            Size = new Vector2(FateListNode.Width, 53.0f),
            IsVisible = true,
            Fate = data,
        });

        foreach (var fateNode in FateListNode.GetNodes<FateEntryNode>()) {
            fateNode.Update();
        }
        
        scrollingAreaNode.ContentHeight = FateListNode.Nodes.Sum(node => node.IsVisible ? node.Height : 0.0f);
    }

    protected override unsafe void OnHide(AtkUnitBase* addon) {
        config.WindowPosition = Position;
        config.WindowSize = Size;
        config.Save();
    }

    protected override unsafe void OnFinalize(AtkUnitBase* addon)
        => System.NativeController.DisposeNode(ref scrollingAreaNode);
}
