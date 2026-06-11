using System;
using System.Linq;
using Dalamud.Game.Gui;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.BaseTypes;
using KamiToolKit.Nodes;
using Lumina.Data.Parsing.Uld;
using Lumina.Text.ReadOnly;
using VanillaPlus.Classes;
using VanillaPlus.Features.ListInventory.Nodes;
using VanillaPlus.Utilities;

namespace VanillaPlus.Features.ListInventory.Addons;

public class InventoryListAddon : NativeAddon {

    protected override unsafe void OnSetup(AtkUnitBase* addon, Span<AtkValue> atkValueSpan) {
        base.OnSetup(addon, atkValueSpan);

        new VerticalListNode {
            Position = ContentStartPosition,
            Size = ContentSize,
            FitWidth = true,
            ItemSpacing = 4.0f,
            InitialNodes = [
                new TextInputNode {
                    Height = 26.0f,
                    PlaceholderStringId = 325, // "Search"
                    SheetType = NodeData.SheetType.Addon,
                    OnInputReceived = OnSearchUpdated,
                },
                listNode = new ListNode<ItemStack, InventoryItemNode> {
                    Height = ContentSize.Y - 26.0f - 4.0f,
                    AutoResetScroll = false,
                    OptionsList = [],
                },
            ],

        }.AttachNode(this);

        UpdateItemsList();

        Services.GameGui.AgentUpdate += OnAgentUpdate;
    }

    protected override unsafe void OnFinalize(AtkUnitBase* addon) {
        base.OnFinalize(addon);

        Services.GameGui.AgentUpdate -= OnAgentUpdate;

        listNode = null;
    }

    private void OnSearchUpdated(ReadOnlySeString searchString) {
        lastSearchString = searchString.ToString();

        UpdateItemsList();
        listNode?.ResetScroll();
    }

    private void OnAgentUpdate(AgentUpdateFlag updateFlags) {
        if (!updateFlags.HasFlag(AgentUpdateFlag.InventoryUpdate)) return;

        UpdateItemsList();
    }

    private void UpdateItemsList()
        => listNode?.OptionsList = Inventory.GetInventoryStacks()
               .Where(item => ItemStack.IsMatch(item, lastSearchString))
               .OrderBy(item => item.ItemName)
               .ToList();

    private ListNode<ItemStack, InventoryItemNode>? listNode;
    private string lastSearchString = string.Empty;

}
