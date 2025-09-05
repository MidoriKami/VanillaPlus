using System.Globalization;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.UI;
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
        foreach (var member in addon->GetHudMembers()) {
            var resetSelf = member.IsSelf() && !config.PartyListSelf;
            var resetOthers = !member.IsSelf() && !config.PartyListMembers;

            ApplyModification(member, resetSelf || resetOthers);
        }
    }

    private void OnPartyListDisable() {
        var addon = Services.GameGui.GetAddonByName<AddonPartyList>("_PartyList");
        if (addon is null) return;
        
        foreach (var member in addon->GetHudMembers()) {
            ApplyModification(member, true);
        }
    }

    private void ApplyModification(PartyListHudData hudData, bool revertToDefault = false) {
        if (config is null) return;

        ModifyPartyListHp(hudData, revertToDefault);
        ModifyPartyListParameter(hudData, revertToDefault);
    }

    private void ModifyPartyListHp(PartyListHudData hudData, bool revertToDefault) {
        if (config is null) return;

        var health = hudData.HudMember->GetHealth();
        if (health is null) return;
        
        var hpGaugeTextNode = hudData.PartyListMember->HPGaugeComponent->GetTextNodeById(2);
        if (hpGaugeTextNode is null) return;

        if ((hudData.IsSelf() && config.PartyListSelf || (!hudData.IsSelf() && !revertToDefault))) {
            hpGaugeTextNode->SetText(GetCorrectText((uint)health.Current, (uint)health.Max, config.PartyListHpEnabled));
        }
        else {
            hpGaugeTextNode->SetText(health.Current.ToString());
        }
    }

    private void ModifyPartyListParameter(PartyListHudData hudData, bool revertToDefault) {
        if (config is null) return;

        if (hudData.HudMember->GetClassJob() is not { } classJob) return;

        var resourceGaugeNode = hudData.PartyListMember->MPGaugeBar;
        if (resourceGaugeNode is null) return;

        if (!hudData.IsSelf() && !classJob.IsNotCrafterGatherer()) return;

        var shouldRevertResource = hudData.IsSelf() && !config.PartyListSelf || revertToDefault;
        var isMpDisabled = (!config.PartyListMpEnabled || shouldRevertResource) && classJob.IsNotCrafterGatherer();

        var resourceGaugeTextNode = resourceGaugeNode->GetTextNodeById(2);
        var resourceGaugeTextSubNode = resourceGaugeNode->GetTextNodeById(3);

        resourceGaugeTextNode->SetXShort(isMpDisabled ? MpDisabledXOffset : MpEnabledXOffset);

        var isPercentageEnabled = IsResourcePercentageEnabled(config, classJob);
        var newResourceText = GetCorrectPartyResourceText(hudData, isPercentageEnabled, shouldRevertResource);
        
        resourceGaugeTextNode->SetText(newResourceText);
        resourceGaugeTextSubNode->ToggleVisibility(isMpDisabled);
    }

    private void OnParameterDisable() {
        var addon = Services.GameGui.GetAddonByName<AddonParameterWidget>("_ParameterWidget");
        if (addon is null) return;
        if (Services.ClientState.LocalPlayer is not { } localPlayer) return;

        var activeResource = GetActiveResource(localPlayer);

        addon->HealthAmount->SetText(localPlayer.CurrentHp.ToString());
        addon->ManaAmount->SetText(GetCorrectText(activeResource.Current, activeResource.Max, false));
    }

    private string GetCorrectText(uint current, uint max, bool enabled = true)
        => !enabled ? current.ToString() : FormatPercentage(current, max);

    private string GetCorrectPartyResourceText(PartyListHudData hudData, bool enabled = true, bool revertToDefault = false) {
        if (hudData.HudMember->GetClassJob() is not { } classJob) return string.Empty;
        
        var currentMana = (uint)hudData.NumberArrayData->CurrentMana;
        var maxMana = (uint)hudData.NumberArrayData->MaxMana;
        
        if (revertToDefault || classJob.IsNotCrafterGatherer() && !enabled) {
            if (classJob.IsNotCrafterGatherer()) {
                currentMana /= 100;
            }

            return currentMana.ToString();
        }

        return GetCorrectText(currentMana, maxMana, enabled);
    }

    private string FormatPercentage(uint current, uint max) {
        if (config is null) return current.ToString();

        if (max == 0) return "0" + (config.PercentageSignEnabled ? "%" : "");

        var percentage = current / (float) max * 100f;

        var percentSign = config.PercentageSignEnabled ? "%" : "";

        var format = config.ShowDecimalsBelowHundredOnly && percentage >= 100f ? "F0" : $"F{config.DecimalPlaces}";

        return percentage.ToString(format, CultureInfo.InvariantCulture) + percentSign;
    }

    private static bool IsResourcePercentageEnabled(ResourceBarPercentagesConfig resourceConfig, ClassJob classJob) {
        if (classJob.IsCrafter())
            return resourceConfig.PartyListCpEnabled;
        
        if (classJob.IsGatherer())
            return resourceConfig.PartyListGpEnabled;
        
        return resourceConfig.PartyListMpEnabled;
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
