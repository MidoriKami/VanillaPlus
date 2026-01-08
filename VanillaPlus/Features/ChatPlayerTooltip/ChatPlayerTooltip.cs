using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;
using System;
using Dalamud.Game.Text.SeStringHandling;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.ChatPlayerTooltip;

public class ChatPlayerTooltip : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Chat Player Tooltip",
        Description = "When mousing over abbreviated player names in chat, shows their full name as a tooltip.",
        Type = ModificationType.UserInterface,
        Authors = [ "anqied" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
            new ChangeLogInfo(2, "Add additional features, change name"),
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
        if (args is not AddonReceiveEventArgs eventArgs) return;

        switch ((AtkEventType)eventArgs.AtkEventType) {
            case AtkEventType.LinkMouseOver: {

                if (eventArgs.AtkEventData == nint.Zero) return;

                var eventData = (AtkEventData*)eventArgs.AtkEventData;
                var linkData = eventData->LinkData;
                if (linkData is null) return;

                if ((LinkMacroPayloadType)linkData->LinkType is not LinkMacroPayloadType.Character) return;
                if (linkData->Payload is null) return;

                var payloadStringSpan = new ReadOnlySeStringSpan(linkData->Payload);
                var enumerator = payloadStringSpan.GetEnumerator();
                enumerator.MoveNext();
                var payload = enumerator.Current;

                if (!payload.TryGetExpression(out _, out _, out var worldExpression, out _, out var nameExpression)) return;
                if (!worldExpression.TryGetUInt(out var worldId)) return;
                if (!nameExpression.TryGetString(out var playerName)) return;
                if (!Services.DataManager.GetExcelSheet<World>().TryGetRow(worldId, out var worldData)) return;

                var addon = args.GetAddon<AtkUnitBase>();
                using var rentedStringBuilder = new RentedSeStringBuilder();
                
                var tooltipString = rentedStringBuilder.Builder
                    .Append(playerName)
                    .AppendIcon((uint)BitmapFontIcon.CrossWorld)
                    .Append(worldData.Name)
                    .ToReadOnlySeString();
                
                ShowTooltip(addon->Id, null, tooltipString);
                break;
            }
            
            case AtkEventType.LinkMouseOut:
                HideTooltip();
                break;
        }
    }

    private unsafe void ShowTooltip(ushort addonId, AtkResNode* node, ReadOnlySpan<byte> tooltipString) {
        AtkStage.Instance()->TooltipManager.ShowTooltip(addonId, node, tooltipString);
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
