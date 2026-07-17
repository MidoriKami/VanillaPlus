using System;
using System.Threading.Tasks;
using Dalamud.Game.DutyState;
using Dalamud.Plugin.Services;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.DutyTimer;

public class DutyTimer : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_DutyTimer,
        Description = Strings.ModificationDescription_DutyTimer,
        Authors = ["MidoriKami"],
        Type = ModificationType.GameBehavior,
        CompatibilityModule = new SimpleTweaksCompatibilityModule("DutyTimer"),
    };

    private DateTime startTimestamp;

    public override Task OnEnableAsync() {
        Service<IDutyState>.Get().DutyStarted += OnDutyStarted;
        Service<IDutyState>.Get().DutyCompleted += OnDutyCompleted;
        Service<IClientState>.Get().TerritoryChanged += OnTerritoryChanged;

        return Task.CompletedTask;
    }

    public override Task OnDisableAsync() {
        Service<IDutyState>.Get().DutyStarted -= OnDutyStarted;
        Service<IDutyState>.Get().DutyCompleted -= OnDutyCompleted;
        Service<IClientState>.Get().TerritoryChanged -= OnTerritoryChanged;

        return Task.CompletedTask;
    }

    private void OnDutyStarted(IDutyStateEventArgs args)
        => startTimestamp = DateTime.UtcNow;

    private void OnDutyCompleted(IDutyStateEventArgs args) {
        var duration = DateTime.UtcNow - startTimestamp;
        var formattedDuration = duration.ToString(@"hh\:mm\:ss\.ffff");
        Service<IChatGui>.Get().Print(Strings.DutyTimer_CompletedMessage.Format(formattedDuration));
    }

    private void OnTerritoryChanged(uint u)
        => startTimestamp = DateTime.UtcNow;
}
