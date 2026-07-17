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
        Services.GetService<IDutyState>().DutyStarted += OnDutyStarted;
        Services.GetService<IDutyState>().DutyCompleted += OnDutyCompleted;
        Services.GetService<IClientState>().TerritoryChanged += OnTerritoryChanged;

        return Task.CompletedTask;
    }

    public override Task OnDisableAsync() {
        Services.GetService<IDutyState>().DutyStarted -= OnDutyStarted;
        Services.GetService<IDutyState>().DutyCompleted -= OnDutyCompleted;
        Services.GetService<IClientState>().TerritoryChanged -= OnTerritoryChanged;

        return Task.CompletedTask;
    }

    private void OnDutyStarted(IDutyStateEventArgs args)
        => startTimestamp = DateTime.UtcNow;

    private void OnDutyCompleted(IDutyStateEventArgs args) {
        var duration = DateTime.UtcNow - startTimestamp;
        var formattedDuration = duration.ToString(@"hh\:mm\:ss\.ffff");
        Services.GetService<IChatGui>().Print(Strings.DutyTimer_CompletedMessage.Format(formattedDuration));
    }

    private void OnTerritoryChanged(uint u)
        => startTimestamp = DateTime.UtcNow;
}
