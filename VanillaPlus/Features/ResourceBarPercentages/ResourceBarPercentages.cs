using System.Globalization;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Arrays;
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

    public override void OnEnable() {
        config = ResourceBarPercentagesConfig.Load();
        configWindow = new ResourceBarPercentagesConfigWindow(config, OnConfigChanged);
        configWindow.AddToWindowSystem();
        OpenConfigAction = configWindow.Toggle;

        Services.AddonLifecycle.RegisterListener(AddonEvent.PostDraw, "_ParameterWidget", OnParameterDraw);
        Services.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "_ParameterWidget", OnParameterDraw);
        Services.AddonLifecycle.RegisterListener(AddonEvent.PostDraw, "_PartyList", OnPartyListDraw);
        Services.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "_PartyList", OnPartyListDraw);
    }

    private void OnConfigChanged() {
        if (config is null) return;

        if (!config.ParameterWidgetEnabled) {
            OnParameterDisable();
        }

        if (!config.PartyListEnabled) {
            OnPartyListDisable();
        }
    }

    public override void OnDisable() {
        Services.AddonLifecycle.UnregisterListener(OnParameterDraw, OnPartyListDraw);
       
        OnParameterDisable();
        OnPartyListDisable();

        configWindow?.RemoveFromWindowSystem();
        configWindow = null;

        config = null;
    }

    private void OnParameterDraw(AddonEvent type, AddonArgs args) {
        if (config is null) return;
        if (!config.ParameterWidgetEnabled) return;

        var addon = args.GetAddon<AddonParameterWidget>();
        if (Services.ClientState.LocalPlayer is not { } localPlayer) return;

        addon->HealthAmount->SetText(GetCorrectText(localPlayer.CurrentHp, localPlayer.MaxHp, config.ParameterHpEnabled));

        var activeResource = GetActiveResource(localPlayer);
        addon->ManaAmount->SetText(GetCorrectText(activeResource.Current, activeResource.Max, activeResource.Enabled));
    }

    private void OnPartyListDraw(AddonEvent type, AddonArgs args) {
        if (config is null) return;
        if (!config.PartyListEnabled) return;
        
        var addon = args.GetAddon<AddonPartyList>();
        if (Services.ClientState.LocalPlayer is null) return;

        foreach (var hudMember in AgentHUD.Instance()->GetSizedHudMemberSpan()) {
            ref var hudPartyMember = ref PartyListNumberArray.Instance()->PartyMembers[hudMember.Index];
            ref var partyMember = ref addon->PartyMembers[hudMember.Index];

            var hpGaugeTextNode = partyMember.HPGaugeComponent->GetTextNodeById(2);
            if (hpGaugeTextNode is not null) {
                var isSelf = hudMember.Index == 0;
                if (isSelf && config.PartyListSelf || !isSelf && config.PartyListOtherMembers) {
                    hpGaugeTextNode->SetText(GetCorrectText((uint)hudPartyMember.CurrentHealth, (uint)hudPartyMember.MaxHealth));
                } else {
                    hpGaugeTextNode->SetText(hudPartyMember.CurrentHealth.ToString());
                }
            }
        }
    }

    private static void OnPartyListDisable() {
        var addon = Services.GameGui.GetAddonByName<AddonPartyList>("_PartyList");
        if (addon is null) return;
        if (Services.ClientState.LocalPlayer is null) return;

        foreach (var hudMember in AgentHUD.Instance()->GetSizedHudMemberSpan()) {
            ref var hudPartyMember = ref PartyListNumberArray.Instance()->PartyMembers[hudMember.Index];
            ref var partyMember = ref addon->PartyMembers[hudMember.Index];

            var hpGaugeTextNode = partyMember.HPGaugeComponent->GetTextNodeById(2);
            if (hpGaugeTextNode is not null) {
                hpGaugeTextNode->SetText(hudPartyMember.CurrentHealth.ToString());
            }
        }
    }

    private void OnParameterDisable() {
        var addon = Services.GameGui.GetAddonByName<AddonParameterWidget>("_ParameterWidget");
        if (addon is null) return;
        if (Services.ClientState.LocalPlayer is not { } localPlayer) return;

        addon->HealthAmount->SetText(localPlayer.CurrentHp.ToString());
        var activeResource = GetActiveResource(localPlayer);
        addon->ManaAmount->SetText(GetCorrectText(activeResource.Current, activeResource.Max, false));
    }

    private string GetCorrectText(uint current, uint max, bool enabled = true)
        => !enabled ? current.ToString() : FormatPercentage(current, max);

    private string FormatPercentage(uint current, uint max) {
        if (config is null) return current.ToString();
        if (max == 0) return "0" + (config.PercentageSignEnabled ? "%" : "");
        var percentage = current / (float)max * 100f;
        var percentSign = config.PercentageSignEnabled ? "%" : "";
        return percentage.ToString($"F{config.DecimalPlaces}", CultureInfo.InvariantCulture) + percentSign;
    }

    private ActiveResource GetActiveResource(IPlayerCharacter player) {
        var defaultResource = new ActiveResource(0, 0, false);
        if (config is null) return defaultResource;

        if (player.MaxMp > 0)
            return new ActiveResource(player.CurrentMp, player.MaxMp, config.ParameterMpEnabled);
        
        if (player.MaxGp > 0)
            return new ActiveResource (player.CurrentGp, player.MaxGp, config.ParameterGpEnabled);
        
        if (player.MaxCp > 0)
            return new ActiveResource (player.CurrentCp, player.MaxCp, config.ParameterCpEnabled);

        return defaultResource;
    }

    private record ActiveResource(uint Current, uint Max, bool Enabled);
}
