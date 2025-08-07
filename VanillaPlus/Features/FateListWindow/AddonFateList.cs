using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Fates;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Addon;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.FateListWindow;

public class AddonFateList(AddonConfig config) : NativeAddon {

    private ScrollingAreaNode<OrderedVerticalListNode<FateEntryNode, long>> scrollingAreaNode = null!;

    private readonly List<FateEntryNode> fateNodes = [];

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
        var validFates = Services.FateTable.Where(fate => fate is { State: FateState.Running }).ToList();
        
        var toRemove =  fateNodes.Where(node => !validFates.Any(fate => node.Fate.FateId == fate.FateId)).ToList();
        foreach (var node in toRemove) {
            scrollingAreaNode.ContentAreaNode.RemoveNode(node);
            fateNodes.Remove(node);
            node.Dispose();
        }
        
        var toAdd = validFates.Where(fate => !fateNodes.Any(node => node.Fate.FateId == fate.FateId)).ToList();
        foreach (var fate in toAdd) {
            var newNode = new FateEntryNode {
                Size = new Vector2(scrollingAreaNode.ContentAreaNode.Width, 53.0f),
                IsVisible = true,
                Fate = fate,
            };
            
            scrollingAreaNode.ContentAreaNode.AddNode(newNode);
            fateNodes.Add(newNode);
        }

        foreach (var fateNode in fateNodes) {
            fateNode.Update();
        }
        
        scrollingAreaNode.ContentHeight = fateNodes.Sum(node => node.IsVisible ? node.Height : 0.0f);
    }

    protected override unsafe void OnHide(AtkUnitBase* addon) {
        config.WindowPosition = Position;
        config.Save();
    }

    protected override unsafe void OnFinalize(AtkUnitBase* addon) {
        foreach (var node in fateNodes.ToList()) {
            scrollingAreaNode.ContentAreaNode.RemoveNode(node);
            fateNodes.Remove(node);
            System.NativeController.DetachNode(node, () => {
                node.Dispose();
            });
        }
        
        System.NativeController.DetachNode(scrollingAreaNode, () => {
            scrollingAreaNode.Dispose();
        });
    }
}
