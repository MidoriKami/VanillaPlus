using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Config;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using Lumina.Text.Payloads;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.ChatWorldNameTooltip;

public class ChatWorldNameTooltip : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Chat World Name Tooltip",
        Description = "When mousing over player names in chat, if the setting to show world names has been turned off, shows the world name of players from other worlds as a tooltip.",
        Type = ModificationType.UserInterface,
        Authors = [ "anqied" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };
    private bool tooltipActive;
    private ushort activeTooltipAddonId;

    public override void OnEnable()
        => Services.AddonLifecycle.RegisterListener(AddonEvent.PreReceiveEvent, ["ChatLogPanel_0", "ChatLogPanel_1", "ChatLogPanel_2", "ChatLogPanel_3"], PreReceiveEvent);

    public override void OnDisable() {
        Services.AddonLifecycle.UnregisterListener(PreReceiveEvent);
        HideTooltip();
    }

    private unsafe void PreReceiveEvent(AddonEvent type, AddonArgs args) {
        if (!Services.GameConfig.TryGet(UiConfigOption.LogCrossWorldName, out bool value) || !value) return;
        if (args is not AddonReceiveEventArgs eventArgs) return;

        switch ((AtkEventType)eventArgs.AtkEventType) {
            case AtkEventType.LinkMouseOver:
                
                if (eventArgs.AtkEventData == nint.Zero) return; 
                var linkData = ((LinkData**)eventArgs.AtkEventData)[0];

                if (linkData is null) return;
                if (linkData->LinkType is not (byte)LinkMacroPayloadType.Character) return;
                
                // IntValue2 of character link is world id
                var worldId = (uint)linkData->IntValue2;

                if (!Services.DataManager.GetExcelSheet<World>().TryGetRow(worldId, out var world)) return;
                
                // If world same as homeworld
                if (Services.PlayerState.HomeWorld.RowId == worldId) return;

                var addon = args.GetAddon<AtkUnitBase>(); 

                ShowTooltip(addon->Id, null, world.Name.ToString());
                break;
            
            case AtkEventType.LinkMouseOut:
                HideTooltip();
                break;
        }
    }

    private unsafe void ShowTooltip(ushort addonId, AtkResNode* node, string world) {
        AtkStage.Instance()->TooltipManager.ShowTooltip(addonId, node, world);
        activeTooltipAddonId = addonId;
        tooltipActive = true;
    }

    private unsafe void HideTooltip() {
        if (tooltipActive) {
            AtkStage.Instance()->TooltipManager.HideTooltip(activeTooltipAddonId);
            tooltipActive = false;
            activeTooltipAddonId = 0;
        }
    }
}
