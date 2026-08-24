using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using VanillaPlus.Extensions;

namespace VanillaPlus.Features.BetterEmoteWindow;

public class EmoteTooltipController : IAsyncDisposable {
    private bool emoteTooltipActive;
    private ushort emoteTooltipAddonId;

    public void Enable() {
        IAddonLifecycle.Get().RegisterListener(AddonEvent.PostReceiveEvent, "Emote", OnEmotePostReceiveEvent);
        IAddonLifecycle.Get().RegisterListener(AddonEvent.PreFinalize, "Emote", OnEmoteFinalize);
    }

    public async ValueTask DisposeAsync() {
        IAddonLifecycle.Get().UnregisterListener(OnEmotePostReceiveEvent);
        IAddonLifecycle.Get().UnregisterListener(OnEmoteFinalize);
        await IFramework.Get().Run(HideEmoteTooltip);
    }

    private unsafe void OnEmotePostReceiveEvent(AddonEvent type, AddonArgs args) {
        if (args is not AddonReceiveEventArgs receiveEventArgs) return;

        switch ((AtkEventType)receiveEventArgs.AtkEventType) {
            case AtkEventType.ListItemRollOut:
                HideEmoteTooltip();
                return;

            case AtkEventType.ListItemRollOver:
                var listItemData = ((AtkEventData*)receiveEventArgs.AtkEventData)->ListItemData;
                var emoteName = listItemData.ListItem->StringValues[0].ToString();
                var emoteRowId = ResolveEmoteRowId(emoteName);
                ShowEmoteTooltip(args.GetAddon<AtkUnitBase>(), listItemData.ListItemRenderer, emoteRowId);
                return;
        }
    }

    private unsafe void ShowEmoteTooltip(AtkUnitBase* addon, AtkComponentListItemRenderer* renderer, uint emoteRowId) {
        if (addon is null || renderer->OwnerNode is null || emoteRowId is 0) {
            HideEmoteTooltip();
            return;
        }

        var playerName = IObjectTable.Get().LocalPlayer?.Name.ToString();
        if (string.IsNullOrWhiteSpace(playerName)) {
            HideEmoteTooltip();
            return;
        }

        var preview = GetEmoteLogMessagePreview(emoteRowId);
        var targetName = ITargetManager.Get().Target?.Name.ToString();
        if (string.IsNullOrEmpty(targetName)) {
            targetName = IDataManager.Get().GetExcelSheet<TextCommandParam>().GetRow(1001).Param.ToString();
        }

        var untargeted = FormatEmoteLogMessage(preview.Untargeted, playerName, targetName);
        var targeted = FormatEmoteLogMessage(preview.Targeted, playerName, targetName);
        if (string.IsNullOrEmpty(untargeted) && string.IsNullOrEmpty(targeted)) {
            HideEmoteTooltip();
            return;
        }

        string tooltip;
        if (string.IsNullOrEmpty(untargeted)) {
            tooltip = $"- {targeted}";
        }
        else if (string.IsNullOrEmpty(targeted) || targeted == untargeted) {
            tooltip = $"- {untargeted}";
        }
        else {
            tooltip = $"- {untargeted}\n- {targeted}";
        }

        var tooltipText = Encoding.UTF8.GetBytes(tooltip);
        AtkStage.Instance()->TooltipManager.ShowTooltip(addon->Id, (AtkResNode*)renderer->OwnerNode, tooltipText);
        emoteTooltipAddonId = addon->Id;
        emoteTooltipActive = true;
    }

    private EmoteLogMessagePreview GetEmoteLogMessagePreview(uint emoteRowId) {
        if (IDataManager.Get().GetExcelSheet<Emote>().GetRowOrDefault(emoteRowId) is not { } emote) {
            return default;
        }

        return new EmoteLogMessagePreview(
            GetEmoteLogMessageTemplate(emote.LogMessageUntargeted),
            GetEmoteLogMessageTemplate(emote.LogMessageTargeted));
    }

    private static string GetEmoteLogMessageTemplate(RowRef<LogMessage> logMessage) {
        if (logMessage.RowId is 0 || !logMessage.IsValid) return string.Empty;

        return logMessage.Value.Text.ToMacroString();
    }

    private static string FormatEmoteLogMessage(string template, string playerName, string targetName)
        => template
            .Replace("<if(gnum7,<sheet(ObjStr,gnum7,0)>,gstr2)>", playerName, StringComparison.Ordinal)
            .Replace("<if(gnum8,<sheet(ObjStr,gnum8,0)>,gstr3)>", targetName, StringComparison.Ordinal);

    private unsafe void HideEmoteTooltip() {
        if (!emoteTooltipActive) return;

        AtkStage.Instance()->TooltipManager.HideTooltip(emoteTooltipAddonId);
        emoteTooltipActive = false;
        emoteTooltipAddonId = 0;
    }

    private unsafe void OnEmoteFinalize(AddonEvent type, AddonArgs args) => HideEmoteTooltip();

    private static uint ResolveEmoteRowId(string name) {
        if (string.IsNullOrEmpty(name)) return 0;

        // Duplicate display names are alternate rows with the same LogMessage references.
        return IDataManager.Get().GetExcelSheet<Emote>()
            .Where(emote => emote.Name.ToString() == name && emote.EmoteCategory.IsValid)
            .Select(emote => emote.RowId)
            .FirstOrDefault();
    }
}
