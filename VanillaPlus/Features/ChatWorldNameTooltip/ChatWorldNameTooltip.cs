using System;
using System.Runtime.Remoting.Channels;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.SampleGameModification;

// Template GameModification for more easily creating your own, can copy this entire folder and rename it.
public class SampleGameModification : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_SampleGameModification,
        Description = Strings.ModificationDescription_SampleGameModification,
        Type = ModificationType.Hidden,
        Authors = [ "anqied" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    // public override string ImageName => "SampleGameModification.png";

    // public override bool IsExperimental => true;
    private bool tooltipActive = false;
    private ushort id = 0;

    public override void OnEnable() {
        Services.AddonLifecycle.RegisterListener(AddonEvent.PreReceiveEvent, ["ChatLogPanel_0", "ChatLogPanel_1", "ChatLogPanel_2", "ChatLogPanel_3"], PreReceiveEvent);
    }

    public override void OnDisable() {
        HideTooltip()
        Services.AddonLifecycle.UnregisterListener(AddonEvent.PreReceiveEvent, ["ChatLogPanel_0", "ChatLogPanel_1", "ChatLogPanel_2", "ChatLogPanel_3"], PreReceiveEvent);
    }

    private unsafe void PreReceiveEvent(AddonEvent type, AddonArgs args) {
        if (!Services.GameConfig.TryGet(UiConfigOption.LogCrossWorldName, out bool value)) //settings check failed
            return;
        if (value) //setting set to show world names
            return;
        if (args is not AddonReceiveEventArgs eventArgs) //wrong event type
            return;

        ushort tempid = eventArgs.Addon.Id;
        if (tempid == 0) //null pointer
            return;
        id = tempid;

        if (eventArgs.AtkEventType == (int)AtkEventType.LinkMouseOver) { // started hovering over something
            if (eventArgs.AtkEventData == IntPtr.Zero) //no info
                return; 
            var linkData = ((LinkData**)eventArgs.AtkEventData)[0]; //get link data
            if (linkData == null || linkData->LinkType != (byte)LinkMacroPayloadType.Character) //hovering a character name
                return;
            uint worldId = (uint)linkData->IntValue2 // IntValue2 of character link is world id
            var world = Services.DataManager.Excel.GetSheet<World>().GetRowOrDefault(worldId);
            if (world == null) 
                return;
            if (Services.PlayerState.HomeWorld.RowId == worldId) //world same as homeworld
                return;

            AtkUnitBase* ptr = (AtkUnitBase*)eventArgs.Addon.Address; //node event came from

            ShowTooltip(ptr->CursorTarget, world?.Name.ToString());
        }

        else if (eventArgs.AtkEventType == (int)AtkEventType.LinkMouseOut) { // stopped hovering over something
            HideTooltip();
        }
    }
    private unsafe void ShowTooltip( AtkResNode* node, string world) {
        AtkStage.Instance()->TooltipManager.ShowTooltip(id, node, world);
        tooltipActive = true;
    }
    private unsafe void HideTooltip() {
        AtkStage.Instance()->TooltipManager.HideTooltip(id);
        tooltipActive = false;
    }
}
