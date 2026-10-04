using System;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using VanillaPlus.Features.BetterEmoteWindow.Classes;

namespace VanillaPlus.Features.BetterEmoteWindow.Controllers;

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

        var eventType = (AtkEventType)receiveEventArgs.AtkEventType;
        var eventData = (AtkEventData*)receiveEventArgs.AtkEventData;
        var addon = args.GetAddon<AtkUnitBase>();

        switch (eventType) {
            case AtkEventType.ListItemRollOut:
                HideEmoteTooltip();
                return;

            case AtkEventType.ListItemRollOver:
                var listItemData = eventData->ListItemData;

                var itemRenderer = listItemData.ListItemRenderer;
                if (itemRenderer is null) return;

                var emoteName = GetRendererText(itemRenderer);
                var emoteRowId = ResolveEmoteRowId(emoteName);
                ShowEmoteTooltip(addon, itemRenderer, emoteRowId);
                return;
        }
    }

    private unsafe void ShowEmoteTooltip(AtkUnitBase* addon, AtkComponentListItemRenderer* renderer, uint emoteRowId) {
        if (addon is null || renderer->OwnerNode is null || emoteRowId is 0) {
            HideEmoteTooltip();
            return;
        }

        var player = IObjectTable.Get().LocalPlayer;
        if (player is null) {
            HideEmoteTooltip();
            return;
        }

        var target = ITargetManager.Get().Target;
        var targetName = target?.Name.ToString();
        byte targetSex = 0;
        if (target is ICharacter targetCharacter) targetSex = targetCharacter.CustomizeData.Sex;

        if (string.IsNullOrEmpty(targetName)) {
            targetName = IDataManager.Get().GetExcelSheet<TextCommandParam>().GetRow(1001).Param.ToString();
        }

        if (IDataManager.Get().GetExcelSheet<Emote>().GetRowOrDefault(emoteRowId) is not { } emote) {
            HideEmoteTooltip();
            return;
        }

        var preview = EmoteLogMessageFormatter.Format(emote, player.Name.ToString(), player.CustomizeData.Sex, targetName, targetSex);
        if (preview.Untargeted.IsEmpty && preview.Targeted.IsEmpty) {
            HideEmoteTooltip();
            return;
        }

        using var rentedStringBuilder = new RentedSeStringBuilder();
        var stringBuilder = rentedStringBuilder.Builder;

        if (preview.Untargeted.IsEmpty) {
            stringBuilder
                .Append("- ")
                .Append(preview.Targeted);
        }
        else if (preview.Targeted.IsEmpty || preview.Targeted == preview.Untargeted) {
            stringBuilder
                .Append("- ")
                .Append(preview.Untargeted);
        }
        else {
            stringBuilder
                .Append("- ").Append(preview.Untargeted)
                .AppendNewLine()
                .Append("- ").Append(preview.Targeted);
        }

        AtkStage.Instance()->TooltipManager.ShowTooltip(addon->Id, (AtkResNode*)renderer->OwnerNode, stringBuilder.GetViewAsSpan());
        emoteTooltipAddonId = addon->Id;
        emoteTooltipActive = true;
    }

    private unsafe void HideEmoteTooltip() {
        if (!emoteTooltipActive) return;

        AtkStage.Instance()->TooltipManager.HideTooltip(emoteTooltipAddonId);
        emoteTooltipActive = false;
        emoteTooltipAddonId = 0;
    }

    private void OnEmoteFinalize(AddonEvent type, AddonArgs args)
        => HideEmoteTooltip();

    private static unsafe string GetRendererText(AtkComponentListItemRenderer* renderer) {
        if (renderer is null) return string.Empty;

        if (renderer->ButtonTextNode is not null) {
            var buttonText = renderer->ButtonTextNode->GetText().ToString();
            if (!string.IsNullOrEmpty(buttonText)) {
                return buttonText;
            }
        }

        if (renderer->RowTemplateNodeList is not null) {
            if (renderer->RowTemplateNodeCountByte is 1) {
                var node = renderer->RowTemplateNode;
                if (node is not null && node->GetNodeType() is not NodeType.Text) {
                    var textNode = (AtkTextNode*)node;
                    var text = textNode->GetText().ToString();

                    if (!string.IsNullOrEmpty(text)) {
                        return text;
                    }
                }
            }
            else {
                for (var index = 0; index < renderer->RowTemplateNodeCountByte; index++) {
                    var node = renderer->RowTemplateNodeList[index];
                    if (node is null || node->GetNodeType() is not NodeType.Text) continue;

                    var textNode = (AtkTextNode*)node;
                    var text = textNode->GetText().ToString();

                    if (!string.IsNullOrEmpty(text)) {
                        return text;
                    }
                }
            }
        }

        foreach (var nodePointer in renderer->UldManager.Nodes) {
            if (nodePointer.IsNull) continue;
            if (nodePointer.Value->GetNodeType() is not NodeType.Text) continue;

            var textNode = (AtkTextNode*)nodePointer.Value;
            var text = textNode->GetText().ToString();

            if (!string.IsNullOrEmpty(text)) {
                return text;
            }
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
}
