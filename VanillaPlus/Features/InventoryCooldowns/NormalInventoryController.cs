using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using VanillaPlus.Utilities;

namespace VanillaPlus.Features.InventoryCooldowns;

public class NormalInventoryController : IDisposable {

    private readonly Dictionary<string, ControllerNodeset> addonControllers = [];

    public unsafe NormalInventoryController() {
        foreach (var addonName in (List<string>)[ "InventoryGrid" ]) {
            addonControllers.Add(addonName, new ControllerNodeset {
                Controller = new AddonController<AddonInventoryGrid> {
                    AddonName = addonName,
                    OnSetup = SetupInventoryGrid,
                    OnDraw = DrawInventoryGrid,
                    OnFinalize = FinalizeInventoryGrid,
                },
            });
        }
    }

    public void Enable() {
        foreach (var addonController in addonControllers) {
            addonController.Value.Controller.Enable();
        }
    }

    public void Dispose() {
        foreach (var (_, controllerSet) in addonControllers) {
            controllerSet.Controller.Dispose();
            controllerSet.Nodes.Clear();
        }
    }

    private unsafe void SetupInventoryGrid(AddonInventoryGrid* addon) {
        var inventorySlots = Inventory.GetInventorySlots(&addon->AtkUnitBase);
        foreach (var index in Enumerable.Range(0, inventorySlots.Length)) {
            var inventorySlot = inventorySlots[index].Value;
            if (inventorySlot is null)
                continue;

            var cooldownNode = new InventoryCooldownTextNode {
                Slot = inventorySlot,
                SlotIndex = index,

                Position = new Vector2(2.0f, 8.0f),
                Size = new Vector2(40.0f, 30.0f),
                AlignmentType = AlignmentType.Center,
                FontType = FontType.TrumpGothic,
                FontSize = 23,
                NodeFlags = NodeFlags.Enabled,
                TextFlags = TextFlags.Edge,
            };

            cooldownNode.AttachNode(inventorySlot->OwnerNode);

            addonControllers[addon->NameString].Nodes.Add(cooldownNode);
        }
    }

    private unsafe void DrawInventoryGrid(AddonInventoryGrid* addon) {
        var parentAddonId = addon->ParentId;
        if (parentAddonId is 0) return;

        var parentAddon = RaptureAtkUnitManager.Instance()->GetAddonById(parentAddonId);
        if (parentAddon is null) return;

        var inventorySorter = Inventory.GetSorterForInventory(parentAddon);

        foreach (var node in addonControllers[addon->NameString].Nodes) {
            var index = node.SlotIndex;

            var adjustedPage = Inventory.GetAdjustedPage(&addon->AtkUnitBase, index);
            var adjustedIndex = Inventory.GetAdjustedIndex(&addon->AtkUnitBase, index);

            var item = Inventory.GetItemForSorter(inventorySorter, adjustedPage, adjustedIndex);
            node.Update(item);
        }
    }

    private unsafe void FinalizeInventoryGrid(AddonInventoryGrid* addon) {
        foreach (var node in addonControllers[addon->NameString].Nodes) {
            node.Dispose();
        }

        addonControllers[addon->NameString].Nodes.Clear();
    }
}
