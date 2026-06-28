using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Config;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Utilities;

namespace VanillaPlus.Features.InventoryCooldowns;

public class InventoryCooldowns : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_InventoryCooldowns,
        Description = Strings.ModificationDescription_InventoryCooldowns,
        Type = ModificationType.UserInterface,
        SubType = ModificationSubType.Inventory,
        Authors = ["Haselnussbomber"],
    };

    public override string ImageName => "InventoryCooldowns.png";

    private readonly Dictionary<string, Dictionary<int, InventoryCooldownTextNode>> addonNodeCache = [];

    private AddonController? inventoryController;

    public override async Task OnEnableAsync() {
        if (Services.ClientState.IsLoggedIn) {
            await Services.Framework.RunSafely(ReinitializeController);
        }

        Services.AddonLifecycle.RegisterListener(AddonEvent.PostReceiveEvent, ["InventoryExpansion", "InventoryLarge", "Inventory"], OnPostReceiveEvent);
        Services.GameConfig.UiConfigChanged += OnUiConfigChanged;
    }

    public override async Task OnDisableAsync() {
        Services.GameConfig.UiConfigChanged -= OnUiConfigChanged;
        Services.AddonLifecycle.UnregisterListener(OnPostReceiveEvent);

        await Services.Framework.RunSafely(() => inventoryController?.Dispose());
        inventoryController = null;
    }

    private  void OnUiConfigChanged(object? sender, ConfigChangeEvent e) {
        if (e.Option is not UiConfigOption.ItemInventryWindowSizeType) return;

        ReinitializeController();
    }

    private unsafe void ReinitializeController() {
        if (!Services.GameConfig.UiConfig.TryGet("ItemInventryWindowSizeType", out uint inventoryType)) return;

        inventoryController?.Dispose();

        inventoryController = new AddonController {
            AddonName = inventoryType switch {
                0 => "Inventory",
                1 => "InventoryLarge",
                2 => "InventoryExpansion",
                _ => throw new ArgumentOutOfRangeException(),
            },
            OnDraw = UpdateInventory,
            OnFinalize = FinalizeInventory,
        };

        inventoryController.Enable();
    }

    public void RemoveNodeFromCache(TextNode node) {
        foreach (var inventoryNodes in addonNodeCache.Values) {
            foreach (var (key, cachedNode) in inventoryNodes) {
                if (node != cachedNode) continue;
                inventoryNodes.Remove(key);
                return;
            }
        }
    }

    private unsafe void OnPostReceiveEvent(AddonEvent type, AddonArgs args) {
        if (args is not AddonReceiveEventArgs { EventType: AtkEventType.ChildAddonAttached } receiveEventArgs)
            return;

        var addon = receiveEventArgs.GetAddon();

        foreach (var childAddon in Inventory.GetInventoryAddons(addon)) {
            if (!IsAllowedChildAddon(childAddon))
                continue;

            var inventorySlots = Inventory.GetInventorySlots(childAddon);

            foreach (var index in Enumerable.Range(0, inventorySlots.Length)) {
                var inventorySlot = inventorySlots[index].Value;
                if (inventorySlot is null)
                    continue;

                if (!addonNodeCache.TryGetValue(childAddon.Value->NameString, out var inventoryNodes))
                    addonNodeCache.Add(childAddon.Value->NameString, inventoryNodes = []);

                if (inventoryNodes.ContainsKey(index))
                    continue;

                var cooldownNode = new InventoryCooldownTextNode(this) {
                    Slot = inventorySlot,

                    Position = new Vector2(2.0f, 8.0f),
                    Size = new Vector2(40.0f, 30.0f),
                    AlignmentType = AlignmentType.Center,
                    FontType = FontType.TrumpGothic,
                    FontSize = 23,
                    NodeFlags = NodeFlags.Enabled,
                    TextFlags = TextFlags.Edge,
                };

                cooldownNode.AttachNode(inventorySlot->OwnerNode);

                inventoryNodes.Add(index, cooldownNode);
            }
        }
    }

    private unsafe void UpdateInventory(AtkUnitBase* addon) {
        var inventorySorter = Inventory.GetSorterForInventory(addon);

        foreach (var childAddon in Inventory.GetInventoryAddons(addon)) {
            if (!IsAllowedChildAddon(childAddon))
                continue;

            var inventorySlots = Inventory.GetInventorySlots(childAddon);

            foreach (var index in Enumerable.Range(0, inventorySlots.Length)) {
                if (!addonNodeCache.TryGetValue(childAddon.Value->NameString, out var inventoryNodes))
                    continue;

                if (!inventoryNodes.TryGetValue(index, out var node))
                    continue;

                var adjustedPage = Inventory.GetAdjustedPage(childAddon, index);
                var adjustedIndex = Inventory.GetAdjustedIndex(childAddon, index);

                var item = Inventory.GetItemForSorter(inventorySorter, adjustedPage, adjustedIndex);
                node.Update(item);
            }
        }
    }

    private unsafe void FinalizeInventory(AtkUnitBase* addon) {
        foreach (var childAddon in Inventory.GetInventoryAddons(addon)) {
            if (!IsAllowedChildAddon(childAddon))
                continue;

            if (addonNodeCache.TryGetValue(childAddon.Value->NameString, out var inventoryNodes)) {
                foreach (var node in inventoryNodes.Values) {
                    node.Dispose();
                }

                inventoryNodes.Clear();
            }
        }
    }

    private static unsafe bool IsAllowedChildAddon(AtkUnitBase* addon)
        => !addon->Name.StartsWith("InventoryEvent"u8);
}
