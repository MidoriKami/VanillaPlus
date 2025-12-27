using System;
using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.NativeElements.Addons;

namespace VanillaPlus.Features.ListInventory;

public class AddonListInventory : SearchableNodeListAddon {
    public required Action? OnInventoryDataChanged { get; init; }

    protected override unsafe void OnSetup(AtkUnitBase* addon) {
        base.OnSetup(addon);

        addon->SubscribeNumberArrayData(NumberArrayType.Inventory);
    }

    protected override unsafe void OnRequestedUpdate(AtkUnitBase* addon, NumberArrayData** numberArrayData, StringArrayData** stringArrayData) {
        OnInventoryDataChanged?.Invoke();
    }

    protected override unsafe void OnFinalize(AtkUnitBase* addon) {
        addon->UnsubscribeNumberArrayData(NumberArrayType.Inventory);
    }
}
