using System;
using Dalamud.Game.DutyState;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.DutyTimer;

public class DutyTimer : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_DutyTimer, 
        Description = Strings.ModificationDescription_DutyTimer,
        Authors = [ "MidoriKami" ],
        Type = ModificationType.GameBehavior,
        CompatibilityModule = new SimpleTweaksCompatibilityModule("DutyTimer"),
    };

    private DateTime startTimestamp;

    public override void OnEnable() {
        Services.DutyState.DutyStarted += OnDutyStarted;
        Services.DutyState.DutyCompleted += OnDutyCompleted;
        Services.ClientState.TerritoryChanged += OnTerritoryChanged;
    }

    public override void OnDisable() {
        Services.DutyState.DutyStarted -= OnDutyStarted;
        Services.DutyState.DutyCompleted -= OnDutyCompleted;
        Services.ClientState.TerritoryChanged -= OnTerritoryChanged;
    }

    private void OnDutyStarted(IDutyStateEventArgs args)
        => startTimestamp = DateTime.UtcNow;

    private void OnDutyCompleted(IDutyStateEventArgs args) {
        var duration = DateTime.UtcNow - startTimestamp;
        var formattedDuration = duration.ToString(@"hh\:mm\:ss\.ffff");
        Services.ChatGui.Print(Strings.DutyTimer_CompletedMessage.Format(formattedDuration));
    }

    private void OnTerritoryChanged(uint _)
        => startTimestamp = DateTime.UtcNow;
}
