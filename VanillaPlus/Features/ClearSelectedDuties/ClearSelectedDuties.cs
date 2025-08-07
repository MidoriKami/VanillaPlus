using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using VanillaPlus.Classes;
using VanillaPlus.Extensions;

namespace VanillaPlus.Features.ClearSelectedDuties;

public class ClearSelectedDuties : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Clear Selected Duties",
        Description = "When opening the Duty Finder, deselects any selected duties.",
        Authors = [ "MidoriKami" ],
        Type = ModificationType.GameBehavior,
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    private ClearSelectedDutiesConfig config = null!;
    private ClearSelectedDutiesConfigWindow configWindow = null!;

    public override void OnEnable() {
        config = ClearSelectedDutiesConfig.Load();
        configWindow = new ClearSelectedDutiesConfigWindow(config);
        configWindow.AddToWindowSystem();
        OpenConfigAction = configWindow.Toggle;
        
        Services.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "ContentsFinder", OnContentsFinderSetup);
    }

    public override void OnDisable() {
        configWindow.RemoveFromWindowSystem();
        Services.AddonLifecycle.UnregisterListener(OnContentsFinderSetup);
    }

    private unsafe void OnContentsFinderSetup(AddonEvent type, AddonArgs args) {
        var contentsFinder = ContentsFinder.Instance();
        var agent = AgentContentsFinder.Instance();
        var addon = args.GetAddon<AddonContentsFinder>();

        if (contentsFinder->QueueInfo.QueueState is not ContentsFinderQueueInfo.QueueStates.None)
            return;

        if (!IsRouletteTab(addon) && config.DisableWhenUnrestricted && contentsFinder->IsUnrestrictedParty) return;

        agent->AgentInterface.SendCommand(0, [ 12, 1 ]);
    }

    private unsafe bool IsRouletteTab(AddonContentsFinder* addon)
        => addon->SelectedRadioButton is 0;
}
