using System.Numerics;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.NativeElements.Config;

namespace VanillaPlus.Features.ClearSelectedDuties;

public class ClearSelectedDuties : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_ClearSelectedDuties,
        Description = Strings.ModificationDescription_ClearSelectedDuties,
        Authors = [ "MidoriKami" ],
        Type = ModificationType.GameBehavior,
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    private ClearSelectedDutiesConfig? config;
    private ConfigAddon? configWindow;

    public override void OnEnable() {
        config = ClearSelectedDutiesConfig.Load();
        configWindow = new ConfigAddon {
            Size = new Vector2(300.0f, 135.0f),
            InternalName = "ClearSelectedConfig",
            Title = Strings.ClearSelectedDuties_ConfigTitle,
            Config = config,
        };

        configWindow.AddCategory(Strings.Settings)
            .AddCheckbox(Strings.ClearSelectedDuties_DisableWhenUnrestricted, nameof(config.DisableWhenUnrestricted));
        
        OpenConfigAction = configWindow.Toggle;
        
        Services.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "ContentsFinder", OnContentsFinderSetup);
    }

    public override void OnDisable() {
        Services.AddonLifecycle.UnregisterListener(OnContentsFinderSetup);
       
        configWindow?.Dispose();
        configWindow = null;
        
        config = null;
    }

    private unsafe void OnContentsFinderSetup(AddonEvent type, AddonArgs args) {
        if (config is null) return;
        
        var contentsFinder = ContentsFinder.Instance();
        var agent = AgentContentsFinder.Instance();
        var addon = args.GetAddon<AddonContentsFinder>();

        if (contentsFinder->QueueInfo.QueueState is not ContentsFinderQueueInfo.QueueStates.None)
            return;

        if (!IsRouletteTab(addon) && config.DisableWhenUnrestricted && contentsFinder->IsUnrestrictedParty) return;

        agent->AgentInterface.SendCommand(0, [ 12, 1 ]);
    }

    private static unsafe bool IsRouletteTab(AddonContentsFinder* addon)
        => addon->SelectedRadioButton is 0;
}
