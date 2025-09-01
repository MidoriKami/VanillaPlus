using System.Globalization;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Arrays;
using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.Classes;
using VanillaPlus.Extensions;

namespace VanillaPlus.Features.ResourceBarPercentages;

public unsafe class ResourceBarPercentages : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Show Resource Bars as Percentages",
        Description = "Displays HP, MP, GP and CP bars as percentages instead of raw values.",
        Type = ModificationType.UserInterface,
        Authors = [ "Zeffuro" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Changelog"),
        ],
    };

    private ResourceBarPercentagesConfig? config;
    private ResourceBarPercentagesConfigWindow? configWindow;

    private bool parameterWidgetEnabled;
    private bool partyListEnabled;

    public override void OnEnable() {
        config = ResourceBarPercentagesConfig.Load();
        configWindow = new ResourceBarPercentagesConfigWindow(config, OnConfigChanged);
        configWindow.AddToWindowSystem();
        OpenConfigAction = configWindow.Toggle;

        parameterWidgetEnabled = config.ParameterWidgetEnabled;
        partyListEnabled = config.PartyListEnabled;

        if (config.ParameterWidgetEnabled) OnParameterEnable();
        if (config.PartyListEnabled) OnPartyListEnable();
    }

    public void OnConfigChanged() {
        if (config is null) return;

        if (config.ParameterWidgetEnabled != parameterWidgetEnabled) {
            if (config.ParameterWidgetEnabled)
                OnParameterEnable();
            else
                OnParameterDisable();
            parameterWidgetEnabled = config.ParameterWidgetEnabled;
        }

        if (config.PartyListEnabled != partyListEnabled) {
            if (config.PartyListEnabled)
                OnPartyListEnable();
            else
                OnPartyListDisable();
            partyListEnabled = config.PartyListEnabled;
        }
    }

    public override void OnDisable() {
        if (config is null) return;

        OnParameterDisable();
        OnPartyListDisable();

        configWindow?.RemoveFromWindowSystem();
        configWindow = null;

        config = null;
    }

    private void OnParameterDraw(AddonEvent type, AddonArgs args) {
        if (config is null) return;
        var addon = args.GetAddon<AddonParameterWidget>();
        if (Services.ClientState.LocalPlayer is not { } localPlayer) return;

        addon->HealthAmount->SetText(FormatPercentage(localPlayer.CurrentHp, localPlayer.MaxHp, config));

        var activeResource = GetActiveResource(localPlayer);
        if (activeResource.Max > 0)
            addon->ManaAmount->SetText(FormatPercentage(activeResource.Current, activeResource.Max, config));
    }

    private void OnParameterEnable() {
        Services.AddonLifecycle.RegisterListener(AddonEvent.PostDraw, "_ParameterWidget", OnParameterDraw);
        Services.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "_ParameterWidget", OnParameterDraw);
    }

    private void OnParameterDisable() {
        if (config is null) return;
        var addon = (AddonParameterWidget*)Services.GameGui.GetAddonByName("_ParameterWidget").Address;
        if (Services.ClientState.LocalPlayer is not { } localPlayer) return;

        addon->HealthAmount->SetText(localPlayer.CurrentHp.ToString());
        addon->ManaAmount->SetText(localPlayer.CurrentMp.ToString());

        Services.AddonLifecycle.UnregisterListener(OnParameterDraw);
    }

    private void OnPartyListDraw(AddonEvent type, AddonArgs args) {
        if (config is null) return;
        var addon = args.GetAddon<AddonPartyList>();

        foreach (var hudMember in AgentHUD.Instance()->PartyMembers) {
            var hudPartyMember = PartyListNumberArray.Instance()->PartyMembers[hudMember.Index];

            var hpGaugeTextNode = addon->PartyMembers[hudMember.Index].HPGaugeComponent->UldManager.SearchNodeById<AtkTextNode>(2);
            if (hpGaugeTextNode is not null) {
                var isSelf = hudMember.Index == 0;
                if ((isSelf && config.PartyListSelf) || (!isSelf && config.PartyListOtherMembers)) {
                    hpGaugeTextNode->SetText(FormatPercentage((uint)hudPartyMember.CurrentHealth, (uint)hudPartyMember.MaxHealth, config));
                } else {
                    hpGaugeTextNode->SetText(hudPartyMember.CurrentHealth.ToString());
                }
            }
        }
    }

    private void OnPartyListEnable() {
        Services.AddonLifecycle.RegisterListener(AddonEvent.PostDraw, "_PartyList", OnPartyListDraw);
        Services.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "_PartyList", OnPartyListDraw);
    }

    private void OnPartyListDisable() {
        if (config is null) return;
        var addon = (AddonPartyList*)Services.GameGui.GetAddonByName("_PartyList").Address;

        foreach (var hudMember in AgentHUD.Instance()->PartyMembers) {
            var hudPartyMember = PartyListNumberArray.Instance()->PartyMembers[hudMember.Index];

            var hpGaugeTextNode = addon->PartyMembers[hudMember.Index].HPGaugeComponent->UldManager.SearchNodeById<AtkTextNode>(2);
            if(hpGaugeTextNode is not null)
                hpGaugeTextNode->SetText(hudPartyMember.CurrentHealth.ToString());
        }

        Services.AddonLifecycle.UnregisterListener(OnPartyListDraw);
    }

    private string FormatPercentage(uint current, uint max, ResourceBarPercentagesConfig resourceBarConfig)
    {
        if (max == 0) return "0" + (resourceBarConfig.PercentageSignEnabled ? "%" : "");
        var percentage = current / (float)max * 100f;
        var percentSign = resourceBarConfig.PercentageSignEnabled ? "%" : "";
        return percentage.ToString($"F{resourceBarConfig.DecimalPlaces}", CultureInfo.InvariantCulture) + percentSign;
    }

    private ActiveResource GetActiveResource(IPlayerCharacter player) {
        if (player.MaxMp > 0)
            return new ActiveResource(player.CurrentMp, player.MaxMp);
        if (player.MaxGp > 0)
            return new ActiveResource (player.CurrentGp, player.MaxGp);
        if (player.MaxCp > 0)
            return new ActiveResource (player.CurrentCp, player.MaxCp);
        return new ActiveResource(0,0);
    }

    private record ActiveResource(uint Current, uint Max);
}
