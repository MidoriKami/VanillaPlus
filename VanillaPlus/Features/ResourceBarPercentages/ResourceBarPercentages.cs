using System.Globalization;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Arrays;
using Lumina.Excel.Sheets;
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
            new ChangeLogInfo(2, "Added option to change resource on Party Members and added Trust support"),
        ],
        Tags = [ "Party List", "Parameter Bars" ],
    };

    private ResourceBarPercentagesConfig? config;
    private ResourceBarPercentagesConfigWindow? configWindow;

    private const short MpDisabledXOffset = -17;
    private const short MpEnabledXOffset = 4;

    public override void OnEnable() {
        config = ResourceBarPercentagesConfig.Load();
        configWindow = new ResourceBarPercentagesConfigWindow(config, OnConfigChanged);
        configWindow.AddToWindowSystem();
        OpenConfigAction = configWindow.Toggle;

        Services.AddonLifecycle.RegisterListener(AddonEvent.PostDraw, "_ParameterWidget", OnParameterDraw);
        Services.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "_ParameterWidget", OnParameterDraw);
        Services.AddonLifecycle.RegisterListener(AddonEvent.PostDraw, "_PartyList", OnPartyListDraw);
        Services.AddonLifecycle.RegisterListener(AddonEvent.PostDraw, "_PartyList", OnTrustListDraw);
        Services.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "_PartyList", OnPartyListDraw);
        Services.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "_PartyList", OnTrustListDraw);
    }

    private void OnConfigChanged() {
        if (config is null) return;

        if (!config.ParameterWidgetEnabled) {
            OnParameterDisable();
        }

        if (!config.PartyListEnabled) {
            OnPartyListDisable();
            OnTrustListDisable();
        }

        if (!config.PartyListTrustMembers) {
            OnTrustListDisable();
        }

        if (!config.PartyListPlayerMembers) {
            OnPartyListDisable();
        }
    }

    public override void OnDisable() {
        Services.AddonLifecycle.UnregisterListener(OnParameterDraw, OnPartyListDraw, OnTrustListDraw);

        OnParameterDisable();
        OnPartyListDisable();
        OnTrustListDisable();

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
        if (Services.ClientState.LocalPlayer is not { EntityId: var playerId } ) return;

        var isTrustParty = addon->TrustCount > 0;

        foreach (var hudMember in AgentHUD.Instance()->GetSizedHudMemberSpan()) {
            if (hudMember.EntityId == 0) continue;
            var isSelf = hudMember.EntityId == playerId;
            var isTrustMember = !isSelf && isTrustParty;

            if (isTrustMember || (!isSelf && !config.PartyListPlayerMembers)) continue;

            ref var partyMember = ref addon->PartyMembers[hudMember.Index];
            HandlePartyMember(ref partyMember, hudMember, isSelf, isTrustMember);
        }
    }

    private void OnTrustListDraw(AddonEvent type, AddonArgs args) {
        if (config is null) return;
        if (!config.PartyListEnabled || !config.PartyListTrustMembers) return;

        var addon = args.GetAddon<AddonPartyList>();
        var isTrustParty = addon->TrustCount > 0;
        if (!isTrustParty) return;

        if (Services.ClientState.LocalPlayer is not { EntityId: var playerId } ) return;

        var index = 0;
        foreach (var hudMember in AgentHUD.Instance()->GetSizedHudMemberSpan()) {
            if (hudMember.EntityId == 0) continue;
            var isSelf = hudMember.EntityId == playerId;
            var isTrustMember = !isSelf && isTrustParty;

            if (!isTrustMember) continue;

            ref var partyMember = ref addon->TrustMembers[index];
            index++;
            HandlePartyMember(ref partyMember, hudMember, isSelf, isTrustMember);
        }
    }

    private void OnPartyListDisable() {
        var addon = Services.GameGui.GetAddonByName<AddonPartyList>("_PartyList");
        if (addon is null) return;
        if (Services.ClientState.LocalPlayer is not { EntityId: var playerId } ) return;

        var isTrustParty = addon->TrustCount > 0;

        foreach (var hudMember in AgentHUD.Instance()->GetSizedHudMemberSpan()) {
            if (hudMember.EntityId == 0) continue;
            var isSelf = hudMember.EntityId == playerId;
            var isTrustMember = !isSelf && isTrustParty;

            if (isTrustMember) continue;
            ref var partyMember = ref addon->PartyMembers[hudMember.Index];
            HandlePartyMember(ref partyMember, hudMember, isSelf, isTrustMember, revertToDefault: true);
        }
    }

    private void OnTrustListDisable() {
        var addon = Services.GameGui.GetAddonByName<AddonPartyList>("_PartyList");
        if (addon is null) return;
        var isTrustParty = addon->TrustCount > 0;
        if (!isTrustParty) return;

        if (Services.ClientState.LocalPlayer is not { EntityId: var playerId } ) return;

        var index = 0;
        foreach (var hudMember in AgentHUD.Instance()->GetSizedHudMemberSpan()) {
            if (hudMember.EntityId == 0) continue;
            var isSelf = hudMember.EntityId == playerId;
            var isTrustMember = !isSelf && isTrustParty;

            if (!isTrustMember) continue;

            ref var partyMember = ref addon->TrustMembers[index];
            index++;
            HandlePartyMember(ref partyMember, hudMember, isSelf, isTrustMember, revertToDefault: true);
        }
    }

    private void HandlePartyMember(ref AddonPartyList.PartyListMemberStruct partyMember, HudPartyMember hudMember, bool isSelf, bool isTrustMember, bool revertToDefault = false) {
        if (config is null) return;
        ref var hudPartyMember = ref PartyListNumberArray.Instance()->PartyMembers[hudMember.Index];
        var health = hudMember.GetHealth();
        var classJob = hudMember.GetClassJob();

        var hpGaugeTextNode = partyMember.HPGaugeComponent->GetTextNodeById(2);
        if (hpGaugeTextNode is not null) {
            if (((isSelf && config.PartyListSelf) || !isSelf) && !revertToDefault) {
                hpGaugeTextNode->SetText(GetCorrectText((uint)health.Current, (uint)health.Max, config.PartyListHpEnabled));
            } else {
                hpGaugeTextNode->SetText(health.Current.ToString());
            }
        }

        var resourceGaugeNode = partyMember.MPGaugeBar;
        if (resourceGaugeNode is not null && !isTrustMember) {
            if (!isSelf && !classJob.IsNotCrafterGatherer()) return;

            var shouldRevertResource = ShouldRevertResource(isSelf, isTrustMember, config, revertToDefault);
            var isMpDisabled = IsMpDisabled(classJob, config, shouldRevertResource);
            var resourceGaugeTextNode = resourceGaugeNode->GetTextNodeById(2);
            var resourceGaugeTextSubNode = resourceGaugeNode->GetTextNodeById(3);

            resourceGaugeTextNode->SetXShort(isMpDisabled ? MpDisabledXOffset : MpEnabledXOffset);
            resourceGaugeTextNode->SetText(GetCorrectPartyResourceText((uint)hudPartyMember.CurrentMana, (uint)hudPartyMember.MaxMana, classJob, IsResourcePercentageEnabled(config, classJob), shouldRevertResource));
            resourceGaugeTextSubNode->ToggleVisibility(isMpDisabled);
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

    private string GetCorrectPartyResourceText(uint current, uint max, ClassJob classJob, bool enabled = true, bool revertToDefault = false) {
        if (revertToDefault || (classJob.IsNotCrafterGatherer() && !enabled)) {
            if (classJob.IsNotCrafterGatherer())
                current /= 100;
            return current.ToString();
        }
        return GetCorrectText(current, max, enabled);
    }

    private string FormatPercentage(uint current, uint max) {
        if (config is null) return current.ToString();
        if (max == 0) return "0" + (config.PercentageSignEnabled ? "%" : "");
        var percentage = current / (float)max * 100f;
        var percentSign = config.PercentageSignEnabled ? "%" : "";
        var format = config.ShowDecimalsBelowHundredOnly && percentage >= 100f ? "F0" : $"F{config.DecimalPlaces}";
        return percentage.ToString(format, CultureInfo.InvariantCulture) + percentSign;
    }

    private bool IsResourcePercentageEnabled(ResourceBarPercentagesConfig resourceConfig, ClassJob classJob) {
        if (classJob.IsCrafter())
            return resourceConfig.PartyListCpEnabled;
        if (classJob.IsGatherer())
            return resourceConfig.PartyListGpEnabled;
        return resourceConfig.PartyListMpEnabled;
    }

    private bool IsMpDisabled(ClassJob classJob, ResourceBarPercentagesConfig resourceConfig, bool shouldRevertResource) {
        return (!resourceConfig.PartyListMpEnabled || shouldRevertResource) && classJob.IsNotCrafterGatherer();
    }

    private bool ShouldRevertResource(bool isSelf, bool isTrustMember, ResourceBarPercentagesConfig resourceConfig, bool revertToDefault) {
        if (isTrustMember) return revertToDefault;
        if (isSelf) return !resourceConfig.PartyListSelf || revertToDefault;
        return revertToDefault;
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
