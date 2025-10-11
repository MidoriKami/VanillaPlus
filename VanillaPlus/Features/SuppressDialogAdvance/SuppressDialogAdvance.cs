using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.SuppressDialogAdvance;

public unsafe class SuppressDialogueAdvance : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Suppress Dialogue Advance",
        Description = "Prevents advancing a cutscene dialogue, unless you click on the dialogue box itself.",
        Type = ModificationType.GameBehavior,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "InitialChangelog"),
        ],
    };

    public override void OnEnable()
        => Services.AddonLifecycle.RegisterListener(AddonEvent.PreReceiveEvent, "Talk", OnTalkReceiveEvent);

    public override void OnDisable()
        => Services.AddonLifecycle.UnregisterListener(OnTalkReceiveEvent);

    private static void OnTalkReceiveEvent(AddonEvent type, AddonArgs args) {
        if (args is not AddonReceiveEventArgs eventArgs) return;

        if ((AtkEventType)eventArgs.AtkEventType is AtkEventType.MouseClick) {
            var addon = args.GetAddon<AddonTalk>();
            var inputData = (AtkEventData*)eventArgs.Data;
            var mouseData = inputData->MouseData;

            if (!addon->RootNode->CheckCollisionAtCoords(mouseData.PosX, mouseData.PosY, true)) {
                eventArgs.AtkEventType = 0;
            }
        }
    }
}
