using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.Exd;
using KamiToolKit.BaseTypes;
using KamiToolKit.Controllers;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using KamiToolKit.Nodes.Simplified;
using Lumina.Excel.Sheets;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Native.Addons;

namespace VanillaPlus.Features.EnhancedLootWindow;

public class EnhancedLootWindow : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_EnhancedLootWindow,
        Description = Strings.ModificationDescription_EnhancedLootWindow,
        Type = ModificationType.UserInterface,
        Authors = ["MidoriKami"],
        CompatibilityModule = new SimpleTweaksCompatibilityModule("UiAdjustments@LootWindowDuplicateUniqueItemIndicator"),
    };

    private ConfigAddon? configWindow;
    private EnhancedLootWindowConfig? config;
    private AddonController<AddonNeedGreed>? needGreedController;

    private List<NodeBase>? attachedNodes;

    public override string ImageName => "EnhancedLootWindow.png";

    public override async Task OnEnableAsync() {
        IAddonLifecycle.Get().LogAddon("NeedGreed");

        config = await EnhancedLootWindowConfig.Load();

        configWindow = new ConfigAddon {
            InternalName = "EnhancedLootWindowConfig",
            Title = Strings.EnhancedLootWindow_ConfigTitle,
            Config = config,
        };

        configWindow.AddCategory(Strings.Settings)
            .AddCheckbox(Strings.EnhancedLootWindow_LabelMarkUnobtainable, nameof(config.MarkUnobtainableItems))
            .AddCheckbox(Strings.EnhancedLootWindow_LabelMarkAlreadyObtained, nameof(config.MarkAlreadyObtainedItems));

        OpenConfigAction = configWindow.Toggle;

        unsafe {
            needGreedController = new AddonController<AddonNeedGreed> {
                AddonName = "NeedGreed",
                OnRefresh = RefreshNeedGreed,
                OnFinalize = FinalizeNeedGreed,
            };
        }

        await needGreedController.EnableAsync();
    }

    public override async Task OnDisableAsync() {
        await needGreedController.DisposeAsyncSafe();
        needGreedController = null;

        await configWindow.DisposeAsyncSafe();
        configWindow = null;

        config = null;
    }

    private unsafe void RefreshNeedGreed(AddonNeedGreed* addon) {
        foreach (var attachedNode in attachedNodes ?? []) {
            attachedNode.Dispose();
        }
        attachedNodes?.Clear();
        attachedNodes = [];

        if (config is null) return;

        var listComponent = addon->GetComponentListById(6);
        if (listComponent is null) return;

        // For each possible item slot, get the item info
        foreach (var index in Enumerable.Range(0, addon->Items.Length)) {
            ref var itemInfo = ref addon->Items[index];
            if (itemInfo.ItemId is 0) continue;

            var itemId = ItemUtil.GetBaseId(itemInfo.ItemId);
            var itemData = IDataManager.Get().GetExcelSheet<Item>().GetRowOrDefault(itemId.ItemId);
            if (itemData is null) continue;

            var rendererComponent = listComponent->GetItemRenderer(index);
            if (rendererComponent is null) continue;

            var targetPart = rendererComponent->UldManager.SearchNodeById(12);
            if (targetPart is null) continue;

            switch (itemData) {
                // Item is unique, and has no unlock action, and is unobtainable if we have any in our inventory
                case { IsUnique: true, ItemAction.RowId: 0 } when InventoryManager.Instance()->PlayerHasItem(itemInfo.ItemId):

                // Item is unobtainable if it's a minion/mount and already unlocked

                // Check Minion Category
                case { ItemUICategory.RowId: 81 } when IsItemAlreadyUnlocked(itemInfo.ItemId):

                // Check MountCategory and MountSortCategory
                case { ItemUICategory.RowId: 63, ItemSortCategory.RowId: 175 } when IsItemAlreadyUnlocked(itemInfo.ItemId):
                    if (config.MarkUnobtainableItems) {
                        var newCrossNode = new IconImageNode {
                            Size = new Vector2(40.0f, 40.0f),
                            Origin = new Vector2(20.0f, 20.0f),
                            Scale = new Vector2(0.80f, 0.80f),
                            Position = new Vector2(12.0f, 13.0f),
                            IconId = 61502,
                            FitTexture = true,
                        };

                        attachedNodes.Add(newCrossNode);
                        newCrossNode.AttachNode(targetPart, NodePosition.AfterTarget);
                    }
                    break;

                // Item can be obtained if unlocked
                case not null when IsItemAlreadyUnlocked(itemInfo.ItemId):
                    if (config.MarkAlreadyObtainedItems) {
                        var newPadlockNode = new SimpleImageNode {
                            TexturePath = "ui/uld/ActionBar.tex",
                            TextureCoordinates = new Vector2(48.0f, 0.0f),
                            TextureSize = new Vector2(20.0f, 24.0f),
                            Size = new Vector2(20.0f, 24.0f),
                            Position = new Vector2(22.0f, 20.0f),
                            WrapMode = WrapMode.Tile,
                        };

                        attachedNodes.Add(newPadlockNode);
                        newPadlockNode.AttachNode(targetPart, NodePosition.AfterTarget);
                    }
                    break;
            }
        }
    }

    private unsafe void FinalizeNeedGreed(AddonNeedGreed* addon) {
        foreach (var attachedNode in attachedNodes ?? []) {
            attachedNode.Dispose();
        }
        attachedNodes?.Clear();
        attachedNodes = null;
    }

    private static unsafe bool IsItemAlreadyUnlocked(uint itemId) {
        var exdItem = ExdModule.GetItemRowById(itemId);
        return exdItem is null || UIState.Instance()->IsItemActionUnlocked(exdItem) is 1;
    }
}
