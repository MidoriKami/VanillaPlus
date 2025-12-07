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
            Size = new Vector2(250.0f, 350.0f),
        };
    }

    private ScrollingAreaNode<VerticalListNode>? scrollingAreaNode;

    private bool updateRequested = true;
    private List<DutyLootItem> items = [];

    public void SetItems(IEnumerable<DutyLootItem> itemsEnumerable) {
        items = itemsEnumerable.ToList();
        updateRequested = true;
    }

    public void Clear() {
        items = [];
        updateRequested = true;
    }

    protected override void OnSetup(AtkUnitBase* addon) {
        scrollingAreaNode = new ScrollingAreaNode<VerticalListNode> {
            Position = ContentStartPosition,
            Size = ContentSize,
            ContentHeight = 100,
        };
        scrollingAreaNode.ContentNode.FitContents = true;
        scrollingAreaNode.AttachNode(this);
        UpdateList(true);
    }

    protected override void OnUpdate(AtkUnitBase* addon) => UpdateList();

    private void UpdateList(bool isOpening = false) {
        if (scrollingAreaNode is null) return;
        if (!updateRequested && !isOpening) return;
        updateRequested = false;

        var list = scrollingAreaNode.ContentNode;
        var listUpdated = list.SyncWithListData(
            items,
            node => node.Item,
            data => new DutyLootNode {
                Size = new Vector2(list.Width, 36.0f),
                Item = data,
            }
        );

        if (listUpdated) {
            scrollingAreaNode.ContentHeight = scrollingAreaNode.ContentNode.Nodes.Sum(node => node.IsVisible ? node.Height : 0.0f);
        }
    }
}
