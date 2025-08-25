using System.Linq;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.Classes;
using VanillaPlus.Extensions;

namespace VanillaPlus.Features.SelectNextLootItem;

public unsafe class SelectNextLootItem : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Automatically Select Next Loot Item",
        Description = "Automatically advance to the next loot item after clicking Need, Greed, or Pass.\n\n" +
                      "Note: this modification does not automatically roll on loot.",
        Type = ModificationType.GameBehavior,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
        CompatibilityModule = new SimpleTweaksCompatibilityModule("UiAdjustments@LootWindowSelectNext"),
    };
    
    public override void OnEnable() {
        Services.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "NeedGreed", OnNeedGreedSetup);
        Services.AddonLifecycle.RegisterListener(AddonEvent.PostReceiveEvent, "NeedGreed", OnNeedGreedEvent);
    }
    
    public override void OnDisable()
        => Services.AddonLifecycle.UnregisterListener(OnNeedGreedSetup, OnNeedGreedEvent);

    private void OnNeedGreedSetup(AddonEvent type, AddonArgs args) {
        // Find first item that hasn't been rolled on, and select it.
        var addonNeedGreed = args.GetAddon<AddonNeedGreed>();
        foreach (var index in Enumerable.Range(0, addonNeedGreed->NumItems)) {
            if (addonNeedGreed->Items[index] is { Roll: 0, ItemId: not 0 }) {
                SelectItem(addonNeedGreed, index);
                break;
            }
        }
    }
    
    private void OnNeedGreedEvent(AddonEvent type, AddonArgs args) {
        if (args is not AddonReceiveEventArgs eventArgs) return;
        
        var eventType = (AtkEventType) eventArgs.AtkEventType;
        var buttonType = (ButtonType) eventArgs.EventParam;
        var addon = eventArgs.GetAddon<AddonNeedGreed>();
        
        if (eventType is not AtkEventType.ButtonClick) return;
        
        switch (buttonType) {
            // Fall through unconditionally
            case ButtonType.Need:
            case ButtonType.Greed:
            
            // Don't select next item if we are passing on an item that we already rolled on
            case ButtonType.Pass when addon->Items[addon->SelectedItemIndex] is { Roll: 0, ItemId: not 0 }: 
                var currentItemCount = addon->NumItems;
                var nextIndex = addon->SelectedItemIndex + 1;

                if (nextIndex < currentItemCount) {
                    SelectItem(addon, nextIndex);
                }
                break;
        }
    }
    
    private static void SelectItem(AddonNeedGreed* addon, int index) {
        var eventData = new AtkEventData();
        eventData.ListItemData.SelectedIndex = index;
        addon->ReceiveEvent(AtkEventType.ListItemClick, 0, null, &eventData);
    }
    
    // There are other button types such as "Greed Only" and "Loot Recipient"
    private enum ButtonType : uint {
        Need = 0,
        Greed = 1,
        Pass = 2,
    }
}
