using System.Threading.Tasks;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.PersistentRetainerGil;

public class PersistentRetainerGil : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.PersistentRetainerGil_PersistentRetainerGil,
        Description = Strings.PersistentRetainerGil_Description,
        Type = ModificationType.GameBehavior,
        Authors = ["Zeffuro"],
    };

    public override string ImageName => "PersistentRetainerGil.png";

    private int previousGil;
    private bool needsUpdate;
    private bool isProcessing;

    public override Task OnEnableAsync() {
        IAddonLifecycle.Get().RegisterListener(AddonEvent.PreReceiveEvent, "Bank", OnBankEvent);
        IAddonLifecycle.Get().RegisterListener(AddonEvent.PostRefresh, "Bank", OnBankRefreshEvent);

        return Task.CompletedTask;
    }

    public override Task OnDisableAsync() {
        IAddonLifecycle.Get().UnregisterListener(OnBankEvent, OnBankRefreshEvent);

        previousGil = 0;
        needsUpdate = false;
        isProcessing = false;

        return Task.CompletedTask;
    }

    private unsafe void OnBankRefreshEvent(AddonEvent type, AddonArgs args) {
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
        } finally {
            isProcessing = false;
        }
    }

    private void OnBankEvent(AddonEvent type, AddonArgs args) {
        if (isProcessing) return;
        if (args is not AddonReceiveEventArgs eventArgs) return;
        if ((AtkEventType)eventArgs.AtkEventType is not AtkEventType.ButtonClick) return;
        if (eventArgs.EventParam is not 3) return;

        previousGil = eventArgs.ValueSpan[4].Int;
        needsUpdate = true;
    }
}
