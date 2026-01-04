using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.PersistentRetainerGil;

public unsafe class PersistentRetainerGil : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.PersistentRetainerGil_PersistentRetainerGil,
        Description = Strings.PersistentRetainerGil_Description,
        Type = ModificationType.GameBehavior,
        Authors = [ "Zeffuro" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    public override string ImageName => "PersistentRetainerGil.png";

    private int previousGil;
    private bool needsUpdate;
    private bool isProcessing;

    public override void OnEnable() {
        Services.AddonLifecycle.RegisterListener(AddonEvent.PreReceiveEvent, "Bank", OnBankEvent);
        Services.AddonLifecycle.RegisterListener(AddonEvent.PostRefresh, "Bank", OnBankRefreshEvent);
    }

    public override void OnDisable() {
        Services.AddonLifecycle.UnregisterListener(OnBankEvent, OnBankRefreshEvent);
        
        previousGil = 0;
        needsUpdate = false;
        isProcessing = false;
    }

    private void OnBankRefreshEvent(AddonEvent type, AddonArgs args) {
        if (isProcessing) return;
        if (!needsUpdate) return;
        if (args is not AddonRefreshArgs eventArgs) return;

        var addon = eventArgs.GetAddon<AddonBank>();

        isProcessing = true;
        try {
            var componentNode = addon->GetComponentNodeById(32);
            if (componentNode is null) return;

            var component = componentNode->GetAsAtkComponentNumericInput();
            if (component is null) return;
            
            component->SetValue(previousGil);
            needsUpdate = false;
        }
        finally {
            isProcessing = false;
        }
    }

    private void OnBankEvent(AddonEvent type, AddonArgs args) {
        if (isProcessing) return;
        if (args is not AddonReceiveEventArgs eventArgs) return;
        if ((AtkEventType)eventArgs.AtkEventType is not AtkEventType.ButtonClick) return;
        if (eventArgs.EventParam is not 3) return;

        previousGil = eventArgs.AtkValueSpan[4].Int;
        needsUpdate = true;
    }
}
