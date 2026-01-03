using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.PersistentRetainerGil;

public unsafe class PersistentRetainerGil : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.PersistentRetainerGil_PersistentRetainerGil,
        Description = Strings.PersistentRetainerGil_Description,
        Type = ModificationType.UserInterface,
        Authors = [ "Zeffuro" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    private int previousGil;
    private bool needsUpdate;
    private bool isProcessing;

    public override void OnEnable() {
        Services.AddonLifecycle.RegisterListener(AddonEvent.PreReceiveEvent, "Bank", OnBankEvent);
        Services.AddonLifecycle.RegisterListener(AddonEvent.PostRefresh, "Bank", OnBankRefreshEvent);
    }

    private void OnBankRefreshEvent(AddonEvent type, AddonArgs args) {
        if (isProcessing) return;
        if(!needsUpdate) return;
        if (args is not AddonRefreshArgs eventArgs) return;

        var addon = eventArgs.GetAddon<AddonBank>();
        if (addon == null) return;

        isProcessing = true;
        try {
            addon->AtkValues[4].SetInt(previousGil);

            AtkEventData eventData = default;
            eventData.InputData.InputId = previousGil;

            AtkEvent dummyEvent = default;

            addon->ReceiveEvent(AtkEventType.ValueUpdate, 0, &dummyEvent, &eventData);
            needsUpdate = false;
        }
        finally {
            isProcessing = false;
        }
    }

    private void OnBankEvent(AddonEvent type, AddonArgs args) {
        if (isProcessing) return;
        if (args is not AddonReceiveEventArgs eventArgs) return;
        if ((AtkEventType)eventArgs.AtkEventType != AtkEventType.ButtonClick || eventArgs.EventParam != 3) return;

        previousGil = eventArgs.AtkValueSpan[4].Int;
        needsUpdate = true;
    }

    public override void OnDisable() {
        Services.AddonLifecycle.UnregisterListener(AddonEvent.PreReceiveEvent, "Bank", OnBankEvent);
        Services.AddonLifecycle.UnregisterListener(AddonEvent.PostRefresh, "Bank", OnBankRefreshEvent);
    }
}
