using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Premade.SearchResultNodes;
using Lumina.Excel.Sheets;
using VanillaPlus.NativeElements.Addons;

namespace VanillaPlus.Features.ListInventory;

public class AddonListInventory : SearchableNodeListAddon<Item, ItemListItemNode> {
    protected override unsafe void OnSetup(AtkUnitBase* addon) {
        base.OnSetup(addon);

        addon->SubscribeNumberArrayData(NumberArrayType.Inventory);
    }

    protected override unsafe void OnRequestedUpdate(AtkUnitBase* addon, NumberArrayData** numberArrayData, StringArrayData** stringArrayData) {
        // todo, fix this
    }

    protected override unsafe void OnFinalize(AtkUnitBase* addon) {
        addon->UnsubscribeNumberArrayData(NumberArrayType.Inventory);
    }
}
