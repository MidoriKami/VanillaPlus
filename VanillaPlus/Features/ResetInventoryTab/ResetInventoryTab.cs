using System;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.Classes;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace VanillaPlus.Features.ResetInventoryTab;

public unsafe class ResetInventoryTab : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Reset Inventory Tab",
        Description = "Automatically resets the inventory to the first tab when opened.",
        Type = ModificationType.GameBehavior,
        Authors = ["Haselnussbomber"],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
        CompatibilityModule = new HaselTweaksCompatibilityModule("FixInventoryOpenTab"),
    };

    public override void OnEnable()
        => Services.AddonLifecycle.RegisterListener(AddonEvent.PreRefresh, ["Inventory", "InventoryLarge", "InventoryExpansion"], OnPreRefresh);

    public override void OnDisable()
        => Services.AddonLifecycle.UnregisterListener(OnPreRefresh);

    private void OnPreRefresh(AddonEvent type, AddonArgs args) {
        if (args is not AddonRefreshArgs refreshArgs || refreshArgs.AtkValues is 0 || refreshArgs.AtkValueCount is 0)
            return;

        var addon = (AtkUnitBase*)args.Addon.Address;
        if (addon->IsVisible)
            return; // Skipping: Addon is visible (using games logic)

        if (GetTabIndex(addon) is 0)
            return; // Skipping: TabIndex already 0 (nothing to do)

        var values = new Span<AtkValue>((void*)refreshArgs.AtkValues, (int)refreshArgs.AtkValueCount);
        if (values[0].Type is not ValueType.Int)
            return; // Skipping: value[0] is not int (invalid)

        if (values[0].Int is 6)
            return; // Skipping: value[0] is 6 (means it requested to open key items)

        ResetTabIndex(addon);
    }
    
    private int GetTabIndex(AtkUnitBase* addon)
        => addon->NameString switch {
            "Inventory" => ((AddonInventory*)addon)->TabIndex,
            "InventoryLarge" => ((AddonInventoryLarge*)addon)->TabIndex,
            "InventoryExpansion" => ((AddonInventoryExpansion*)addon)->TabIndex,
            _ => 0,
        };

    private void ResetTabIndex(AtkUnitBase* addon) {
        switch (addon->NameString) {
            case "Inventory": ((AddonInventory*)addon)->SetTab(0); break;
            case "InventoryLarge": ((AddonInventoryLarge*)addon)->SetTab(0); break;
            case "InventoryExpansion": ((AddonInventoryExpansion*)addon)->SetTab(0, false); break;
        }
    }
}
