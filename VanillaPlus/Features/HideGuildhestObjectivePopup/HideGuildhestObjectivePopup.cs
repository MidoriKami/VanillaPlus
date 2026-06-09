using System.Threading.Tasks;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.HideGuildhestObjectivePopup;

public class HideGuildhestObjectivePopup : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_HideGuildhestObjectivePopup,
        Description = Strings.ModificationDescription_HideGuildhestObjectivePopup,
        Authors = ["MidoriKami"],
        Type = ModificationType.GameBehavior,
        CompatibilityModule = new SimpleTweaksCompatibilityModule("UiAdjustments@HideGuildhestObjectivePopup"),
    };

    public override string ImageName => "HideGuildhestObjective.png";

    public override Task OnEnableAsync() {
        Services.AddonLifecycle.RegisterListener(AddonEvent.PreSetup, "JournalAccept", OnJournalAcceptOpen);

        return Task.CompletedTask;
    }

    public override Task OnDisableAsync() {
        Services.AddonLifecycle.UnregisterListener(OnJournalAcceptOpen);

        return Task.CompletedTask;
    }

    private static unsafe void OnJournalAcceptOpen(AddonEvent type, AddonArgs args) {
        if (Services.DataManager.GetExcelSheet<TerritoryType>().GetRow(Services.ClientState.TerritoryType) is not { TerritoryIntendedUse.RowId: 3 }) return;

        // todo: figure out how to use the agent to do this.
        args.GetAddon<AtkUnitBase>()->Hide(false, false, 1);
    }
}
