using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.Classes;
using VanillaPlus.NativeElements.Config;

namespace VanillaPlus.Features.SuppressDialogAdvance;

public unsafe class SuppressDialogueAdvance : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Suppress Dialogue Advance",
        Description = "Prevents advancing a cutscene dialogue, unless you click on the dialogue box itself.",
        Type = ModificationType.GameBehavior,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
            new ChangeLogInfo(2, "Added option to only apply while in a cutscene, enabled by default."),
        ],
    };

    private SuppressDialogAdvanceConfig? config;
    private ConfigAddon? configWindow;

    public override void OnEnable() {
        config = SuppressDialogAdvanceConfig.Load();

        configWindow = new ConfigAddon {
            InternalName = "SuppressDialogAdvanceConfig",
            Title = "Suppress Dialog Advance Config",
            Config = config,
        };
        
        configWindow.AddCategory("General")
            .AddCheckbox("Apply only in Cutscenes", nameof(config.ApplyOnlyInCutscenes));

        OpenConfigAction = configWindow.Toggle;
        
        Services.AddonLifecycle.RegisterListener(AddonEvent.PreReceiveEvent, "Talk", OnTalkReceiveEvent);
    }

    public override void OnDisable() {
        Services.AddonLifecycle.UnregisterListener(OnTalkReceiveEvent);

        configWindow?.Dispose();
        configWindow = null;
        
        config = null;
    }

    private void OnTalkReceiveEvent(AddonEvent type, AddonArgs args) {
        if (args is not AddonReceiveEventArgs eventArgs) return;
        if ((config?.ApplyOnlyInCutscenes ?? false) && !Services.Condition.IsInCutscene()) return;

        if ((AtkEventType)eventArgs.AtkEventType is AtkEventType.MouseClick) {
            var addon = args.GetAddon<AddonTalk>();

            if (!addon->RootNode->CheckCollisionAtCoords(args.GetMouseClickPosition())) {
                eventArgs.AtkEventType = 0;
            }
        }
    }
}
