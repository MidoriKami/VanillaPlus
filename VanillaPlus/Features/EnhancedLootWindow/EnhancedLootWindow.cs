using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.Exd;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using Lumina.Excel.Sheets;
using VanillaPlus.Classes;
using VanillaPlus.Extensions;

namespace VanillaPlus.EnhancedLootWindow;

public unsafe class EnhancedLootWindow : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Enhanced Loot Window",
        Description = "Adds indicators to loot window items to indicate if you have unlocked that item before, or if the item is obtainable.",
        Type = ModificationType.UserInterface,
        Authors = ["MidoriKami"],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
        CompatabilityModule = new SimpleTweaksCompatabilityModule("UiAdjustments@LootWindowDuplicateUniqueItemIndicator"),
    };

    private EnhancedLootWindowConfigWindow configWindow = null!;
    private EnhancedLootWindowConfig config = null!;
    private AddonController<AddonNeedGreed> needGreedController = null!;

    private readonly List<ImageNode> crossNodes = [];
    private readonly List<ImageNode> padlockNodes = [];

    public override string ImageName => "EnhancedLootWindow.png";

    public override void OnEnable() {
        config = EnhancedLootWindowConfig.Load();
        configWindow = new EnhancedLootWindowConfigWindow(config);
        configWindow.AddToWindowSystem();
        OpenConfigAction = configWindow.Toggle;

        needGreedController = new AddonController<AddonNeedGreed>("NeedGreed");
        needGreedController.OnAttach += AttachNodes;
        needGreedController.OnDetach += DetachNodes;
        needGreedController.OnRefresh += RefreshNodes;
        needGreedController.Enable();
    }

    public override void OnDisable() {
        needGreedController.Dispose();
        configWindow.RemoveFromWindowSystem();
    }

    private void AttachNodes(AddonNeedGreed* addon) {
        var listComponentNode = (AtkComponentNode*) addon->GetNodeById(6);
        if (listComponentNode is null) return;
        
        var listComponent = (AtkComponentList*) listComponentNode->Component;
        if (listComponent is null) return;

        foreach(uint nodeId in Enumerable.Range(21001, 31).Prepend(2)) {
            var listItemRenderer = listComponent->UldManager.SearchNodeById<AtkComponentNode>(nodeId);
            if (listItemRenderer is null) continue;
            
            var listItemRendererComponent =  (AtkComponentListItemRenderer*) listItemRenderer->Component;
            if (listItemRendererComponent is null) continue;

            var targetPart = listItemRendererComponent->UldManager.SearchNodeById(12);
            if (targetPart is null) continue;
            
            var newCrossNode = new IconImageNode {
                Size = new Vector2(40.0f, 40.0f),
                Origin = new Vector2(20.0f, 20.0f),
                Scale = new Vector2(0.80f, 0.80f),
                Position = new Vector2(12.0f, 13.0f),
                IconId = 61502,
                WrapMode = 2,
                ImageNodeFlags = 0,
            };
            
            crossNodes.Add(newCrossNode);
            System.NativeController.AttachNode(newCrossNode, targetPart, NodePosition.AfterTarget);

            var newPadlockNode = new SimpleImageNode {
                TexturePath = "ui/uld/ActionBar.tex",
                TextureCoordinates = new Vector2(48.0f, 0.0f),
                TextureSize = new Vector2(20.0f, 24.0f),
                Size = new Vector2(20.0f, 24.0f),
                Position = new Vector2(22.0f, 20.0f),
                Alpha = 175,
                WrapMode = 1,
                ImageNodeFlags = 0,
            };
            
            padlockNodes.Add(newPadlockNode);
            System.NativeController.AttachNode(newPadlockNode, targetPart, NodePosition.AfterTarget);
        }
    }
    
    private void DetachNodes(AddonNeedGreed* addon) {
        foreach (var node in crossNodes) {
            System.NativeController.DetachNode(node, () => {
                node.Dispose();
            });
        }
        crossNodes.Clear();
        
        foreach (var node in padlockNodes) {
            System.NativeController.DetachNode(node, () => {
                node.Dispose();
            });
        }
        padlockNodes.Clear();
    }
    
    private const int MinionCategory = 81;
    private const int MountCategory = 63;
    private const int MountSubCategory = 175;

    private void RefreshNodes(AddonNeedGreed* addon) {
        // For each possible item slot, get the item info
        for (var index = 0; index < addon->Items.Length; index++) {
            ref var itemInfo = ref addon->Items[index];
            if (itemInfo.ItemId is 0) continue;
            
            var adjustedItemId = itemInfo.ItemId > 1_000_000 ? itemInfo.ItemId - 1_000_000 : itemInfo.ItemId;
            
            // If we can't match the item in lumina, skip.
            var itemData = Services.DataManager.GetExcelSheet<Item>().GetRowOrDefault(adjustedItemId);
            if (itemData is null) continue;

            var crossNode = crossNodes[index];
            var padlockNode = padlockNodes[index];
                
            switch (itemData) {
                // Item is unique, and has no unlock action, and is unobtainable if we have any in our inventory
                case { IsUnique: true, ItemAction.RowId: 0 } when InventoryManager.Instance()->PlayerHasItem(itemInfo.ItemId):
                        
                // Item is unobtainable if it's a minion/mount and already unlocked
                case { ItemUICategory.RowId: MinionCategory } when IsItemAlreadyUnlocked(itemInfo.ItemId):
                case { ItemUICategory.RowId: MountCategory, ItemSortCategory.RowId: MountSubCategory } when IsItemAlreadyUnlocked(itemInfo.ItemId):
                    crossNode.IsVisible = config?.MarkUnobtainableItems ?? false;
                    padlockNode.IsVisible = false;
                    break;

                // Item can be obtained if unlocked
                case not null when IsItemAlreadyUnlocked(itemInfo.ItemId):
                    padlockNode.IsVisible = config?.MarkAlreadyObtainedItems ?? false;
                    crossNode.IsVisible = false;
                    break;
                    
                // Item can be obtained normally
                default:
                    crossNode.IsVisible = false;
                    padlockNode.IsVisible = false;
                    break;
            }
        }
    }

    private bool IsItemAlreadyUnlocked(uint itemId) {
        var exdItem = ExdModule.GetItemRowById(itemId);
        return exdItem is null || UIState.Instance()->IsItemActionUnlocked(exdItem) is 1;
    }
}
