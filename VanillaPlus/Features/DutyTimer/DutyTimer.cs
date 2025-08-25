using System;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.DutyTimer;

public class DutyTimer : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Duty Timer",
        Description = "When completing a duty, prints the time the duty took to chat.",
        Authors = [ "MidoriKami" ],
        Type = ModificationType.GameBehavior,
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
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

    private void OnDutyStarted(object? sender, ushort e)
        => startTimestamp = DateTime.UtcNow;

    private void OnDutyCompleted(object? sender, ushort e)
        => Services.ChatGui.Print($@"Duty Completed in: {DateTime.UtcNow - startTimestamp:hh\:mm\:ss\.ffff}");

    private void OnTerritoryChanged(ushort obj)
        => startTimestamp = DateTime.UtcNow;
}
