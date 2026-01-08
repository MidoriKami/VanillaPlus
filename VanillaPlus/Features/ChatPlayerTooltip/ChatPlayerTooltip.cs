using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Config;
using Dalamud.Game.Text;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;
using System;
using System.Numerics;
using VanillaPlus.Classes;
using VanillaPlus.NativeElements.Config;

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

    private ChatPlayerTooltipConfig? config;
    private ConfigAddon? configWindow;
    private bool tooltipActive;
    private ushort activeTooltipAddonId;
    private ReadOnlySeString flower;

    public override void OnEnable() {
        config = ChatPlayerTooltipConfig.Load();
        configWindow = new ConfigAddon {
            Size = new Vector2(400.0f, 125.0f),
            InternalName = "ChatTooltipConfig",
            Title = "Chat Tooltip Config",
            Config = config,
        };

        configWindow.AddCategory(Strings.Settings)
            .AddCheckbox("Show World Name", nameof(config.showWorldName));

        OpenConfigAction = configWindow.Toggle;

        Services.AddonLifecycle.RegisterListener(AddonEvent.PreReceiveEvent, ["ChatLogPanel_0", "ChatLogPanel_1", "ChatLogPanel_2", "ChatLogPanel_3"], PreReceiveEvent);
        flower = SeIconChar.CrossWorld.ToIconString();
    }

    public override void OnDisable() {
        Services.AddonLifecycle.UnregisterListener(PreReceiveEvent);
        HideTooltip(); 
        
        configWindow?.Dispose();
        configWindow = null;

        config = null;
    }

    private unsafe void PreReceiveEvent(AddonEvent type, AddonArgs args) {
        Services.GameConfig.TryGet(UiConfigOption.LogCrossWorldName, out bool crossWorldName);
        Services.GameConfig.TryGet(UiConfigOption.LogNameType, out uint nameType);

        // name is not abbreviated
        if (nameType == 0 && crossWorldName == true) return;

        if (args is not AddonReceiveEventArgs eventArgs) return;

        switch ((AtkEventType)eventArgs.AtkEventType) {
            case AtkEventType.LinkMouseOver:
                
                if (eventArgs.AtkEventData == nint.Zero) return; 
                var linkData = ((LinkData**)eventArgs.AtkEventData)[0];

                if (linkData is null) return;
                if (linkData->LinkType is not (byte)LinkMacroPayloadType.Character) return;
                if (linkData->Payload == null) return;

                var charName = new ReadOnlySeStringSpan();

                var seString = new ReadOnlySeStringSpan(linkData->Payload);
                foreach (var payload in seString) {
                    if (payload.Type == ReadOnlySePayloadType.Macro &&
                        payload.MacroCode == MacroCode.Link &&
                        payload.TryGetExpression(out var typeExpression) &&
                        typeExpression.TryGetInt(out var linkType)) {
                        if (linkType == (int)LinkMacroPayloadType.Character &&
                            payload.TryGetExpression(out _, out _, out var worldExpression, out _, out var nameExpression) &&
                            nameExpression.TryGetString(out charName)) {
                        }
                        else if (linkType == (int)LinkMacroPayloadType.Terminator) // end of the link
                        {
                            break;
                        }
                    }
                }
                
                // IntValue2 of character link is world id
                var worldId = (uint)linkData->IntValue2;

                // Not able to find world
                if (!Services.DataManager.GetExcelSheet<World>().TryGetRow(worldId, out var world)) return;
                var worldName = flower + world.Name;
                if (worldId == (uint)0 || !config.showWorldName || Services.PlayerState.HomeWorld.RowId == worldId)
                    worldName = new ReadOnlySeString();
                var tooltipString = (charName + worldName).AsSpan();
                var addon = args.GetAddon<AtkUnitBase>();
                ShowTooltip(addon->Id, null, tooltipString.Data);
                break;
            
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
