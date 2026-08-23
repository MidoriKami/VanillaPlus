using System;
using System.Collections.Generic;
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
    private readonly Dictionary<uint, EmoteLogMessagePreview> emoteLogMessagePreviews = [];
    private string? fallbackTargetText;
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

        emoteLogMessagePreviews.Clear();
        fallbackTargetText = null;
    }

    private unsafe void OnEmotePostReceiveEvent(AddonEvent type, AddonArgs args) {
        if (args is not AddonReceiveEventArgs receiveEventArgs) return;

        switch ((AtkEventType)receiveEventArgs.AtkEventType) {
            case AtkEventType.ListItemRollOut:
                HideEmoteTooltip();
                return;

            case AtkEventType.ListItemRollOver:
                if (receiveEventArgs.AtkEventData is 0) return;

                var renderer = ((AtkEventData*)receiveEventArgs.AtkEventData)->ListItemData.ListItemRenderer;
                if (renderer is null) {
                    HideEmoteTooltip();
                    return;
                }

                var emoteName = GetRendererText(renderer);
                var emoteRowId = ResolveEmoteRowId(emoteName);
                ShowEmoteTooltip(args.GetAddon<AtkUnitBase>(), renderer, emoteRowId);
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
            fallbackTargetText ??= IDataManager.Get().GetExcelSheet<TextCommandParam>().GetRow(1001).Param.ToString();
            targetName = fallbackTargetText;
        }

        var untargeted = FormatEmoteLogMessage(preview.Untargeted, playerName, targetName);
        var targeted = FormatEmoteLogMessage(preview.Targeted, playerName, targetName);
        if (string.IsNullOrEmpty(untargeted) && string.IsNullOrEmpty(targeted)) {
            HideEmoteTooltip();
            return;
        }

        var tooltip = string.IsNullOrEmpty(untargeted)
            ? $"- {targeted}"
            : string.IsNullOrEmpty(targeted) || targeted == untargeted
                ? $"- {untargeted}"
                : $"- {untargeted}\n- {targeted}";

        var tooltipText = Encoding.UTF8.GetBytes(tooltip);
        AtkStage.Instance()->TooltipManager.ShowTooltip(addon->Id, (AtkResNode*)renderer->OwnerNode, tooltipText);
        emoteTooltipAddonId = addon->Id;
        emoteTooltipActive = true;
    }

    private EmoteLogMessagePreview GetEmoteLogMessagePreview(uint emoteRowId) {
        if (emoteLogMessagePreviews.TryGetValue(emoteRowId, out var preview)) return preview;

        if (IDataManager.Get().GetExcelSheet<Emote>().GetRowOrDefault(emoteRowId) is not { } emote) {
            return default;
        }

        preview = new EmoteLogMessagePreview(
            GetEmoteLogMessageTemplate(emote.LogMessageUntargeted),
            GetEmoteLogMessageTemplate(emote.LogMessageTargeted));
        emoteLogMessagePreviews[emoteRowId] = preview;
        return preview;
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

    private static unsafe string GetRendererText(AtkComponentListItemRenderer* renderer) {
        if (renderer->ButtonTextNode is not null) {
            var buttonText = renderer->ButtonTextNode->GetText().ToString();
            if (!string.IsNullOrEmpty(buttonText)) return buttonText;
        }

        if (renderer->RowTemplateNodeList is not null) {
            for (var index = 0; index < renderer->RowTemplateNodeCountByte; index++) {
                var node = renderer->RowTemplateNodeList[index];
                if (node is null || node->GetNodeType() is not NodeType.Text) continue;

                var text = ((AtkTextNode*)node)->GetText().ToString();
                if (!string.IsNullOrEmpty(text)) return text;
            }
        }

        foreach (var nodePointer in renderer->UldManager.Nodes) {
            var node = nodePointer.Value;
            if (node is null || node->GetNodeType() is not NodeType.Text) continue;

            var text = ((AtkTextNode*)node)->GetText().ToString();
            if (!string.IsNullOrEmpty(text)) return text;
        }

        return string.Empty;
    }

    private static uint ResolveEmoteRowId(string name) {
        if (string.IsNullOrEmpty(name)) return 0;

        // Duplicate display names are alternate rows with the same LogMessage references.
        return IDataManager.Get().GetExcelSheet<Emote>()
            .Where(emote => emote.Name.ToString() == name && emote.EmoteCategory.IsValid)
            .Select(emote => emote.RowId)
            .FirstOrDefault();
    }

    private readonly record struct EmoteLogMessagePreview(string Untargeted, string Targeted);
}
