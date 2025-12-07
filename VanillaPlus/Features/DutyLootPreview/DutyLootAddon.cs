using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Nodes;

namespace VanillaPlus.Features.DutyLootPreview;

/// <summary>
/// The window that shows loot for a duty.
/// </summary>
public unsafe class DutyLootPreviewAddon : NativeAddon {
    public static DutyLootPreviewAddon Create() {
        return new DutyLootPreviewAddon {
            InternalName = "DutyLootPreview",
            Title = "Duty Loot Preview",
            Size = new Vector2(250.0f, 350.0f)
        };
    }

    private ScrollingAreaNode<VerticalListNode>? ScrollingAreaNode;

    private bool updateRequested = true;
    private List<DutyLootItem> Items = new();

    public void SetItems(IEnumerable<DutyLootItem> items) {
        Items = items.ToList();
        updateRequested = true;
    }

    public void Clear() {
        Items = new();
        updateRequested = true;
    }

    protected override unsafe void OnSetup(AtkUnitBase* addon) {
        ScrollingAreaNode = new ScrollingAreaNode<VerticalListNode> {
            Position = ContentStartPosition,
            Size = ContentSize,
            ContentHeight = 100,
        };
        ScrollingAreaNode.ContentNode.FitContents = true;
        ScrollingAreaNode.AttachNode(this);
        UpdateList(true);
    }

    protected override void OnUpdate(AtkUnitBase* addon) => UpdateList();

    public void UpdateList(bool isOpening = false) {
        if (ScrollingAreaNode is null) return;
        if (!updateRequested && !isOpening) return;
        updateRequested = false;

        var list = ScrollingAreaNode.ContentNode;
        var listUpdated = list.SyncWithListData(
            Items,
            (DutyLootNode node) => node.Item,
            (DutyLootItem data) => new DutyLootNode {
                Size = new Vector2(list.Width, 36.0f),
                Item = data,
            }
        );

        if (listUpdated) {
            ScrollingAreaNode.ContentHeight = ScrollingAreaNode.ContentNode.Nodes.Sum(node => node.IsVisible ? node.Height : 0.0f);
        }
    }
}
