using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Utilities;

namespace VanillaPlus.Features.InventoryCooldowns;

public unsafe class InventoryCooldowns : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_InventoryCooldowns,
        Description = Strings.ModificationDescription_InventoryCooldowns,
        Type = ModificationType.UserInterface,
        SubType = ModificationSubType.Inventory,
        Authors = [ "Haselnussbomber" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ]
    };

    public override string ImageName => "InventoryCooldowns.png";

    private readonly Dictionary<string, Dictionary<int, InventoryCooldownTextNode>> addonNodeCache = [];

    private MultiAddonController? controller;

    public override void OnEnable() {
        controller = new MultiAddonController {
            AddonNames = ["InventoryExpansion", "InventoryLarge", "Inventory"],
            OnSetup = SetupInventory,
            OnUpdate = UpdateInventory,
            OnFinalize = FinalizeInventory,
        };
        controller.Enable();
    }

    public override void OnDisable() {
        controller?.Dispose();
        controller = null;
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

    private void SetupInventory(AtkUnitBase* addon) {
        foreach (var childAddon in Inventory.GetInventoryAddons(addon)) {
            if (!IsAllowedChildAddon(childAddon))
                continue;

            var inventorySlots = Inventory.GetInventorySlots(childAddon);

            foreach (var index in Enumerable.Range(0, inventorySlots.Length)) {
                var inventorySlot = inventorySlots[index].Value;
                if (inventorySlot is null) continue;

                if (!addonNodeCache.TryGetValue(childAddon.Value->NameString, out var inventoryNodes))
                    addonNodeCache.Add(childAddon.Value->NameString, inventoryNodes = []);

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

    private void UpdateInventory(AtkUnitBase* addon) {
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

    private void FinalizeInventory(AtkUnitBase* addon) {
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

    private static bool IsAllowedChildAddon(AtkUnitBase* addon)
        => !addon->Name.StartsWith("InventoryEvent"u8);
}
