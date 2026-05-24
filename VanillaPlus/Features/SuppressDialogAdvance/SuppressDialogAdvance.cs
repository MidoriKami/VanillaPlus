using System.Threading.Tasks;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.NativeElements.Config;

namespace VanillaPlus.Features.SuppressDialogAdvance;

public class SuppressDialogueAdvance : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_SuppressDialogAdvance,
        Description = Strings.ModificationDescription_SuppressDialogAdvance,
        Type = ModificationType.GameBehavior,
        Authors = ["MidoriKami"],
    };

    private SuppressDialogAdvanceConfig? config;
    private ConfigAddon? configWindow;

    public override async Task OnEnableAsync() {
        config = await SuppressDialogAdvanceConfig.Load();

        configWindow = new ConfigAddon {
            InternalName = "SuppressDialogAdvanceConfig",
            Title = Strings.SuppressDialogAdvance_ConfigTitle,
            Config = config,
        };

        configWindow.AddCategory(Strings.SuppressDialogAdvance_CategoryGeneral)
            .AddCheckbox(Strings.SuppressDialogAdvance_ApplyOnlyInCutscenes, nameof(config.ApplyOnlyInCutscenes));

        OpenConfigAction = configWindow.Toggle;

        await Services.Framework.Run(() => {
            Services.AddonLifecycle.RegisterListener(AddonEvent.PreReceiveEvent, "Talk", OnTalkReceiveEvent);
        });
    }

    public override async Task OnDisableAsync() {
        await Services.Framework.Run(() => {
            Services.AddonLifecycle.UnregisterListener(OnTalkReceiveEvent);
        });


        configWindow?.Dispose();
        configWindow = null;

        config = null;
    }

    private unsafe void OnTalkReceiveEvent(AddonEvent type, AddonArgs args) {
        if (args is not AddonReceiveEventArgs eventArgs) return;
        if ((config?.ApplyOnlyInCutscenes ?? false) && !Services.Condition.IsInCutscene) return;

        if ((AtkEventType)eventArgs.AtkEventType is AtkEventType.MouseClick) {
            var addon = args.GetAddon<AddonTalk>();

            if (!addon->RootNode->CheckCollision(args.ClickPosition)) {
                eventArgs.AtkEventType = 0;
            }
        }
    }
}
