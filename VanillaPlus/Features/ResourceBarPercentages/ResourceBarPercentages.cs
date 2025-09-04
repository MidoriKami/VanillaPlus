using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
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
        if (Services.ClientState.LocalPlayer is not { ClassJob: { IsValid: true, Value: var classJob }, EntityId: var playerId } ) return;

        var isTrustParty = addon->TrustCount > 0;

        var trustMemberMap = new Dictionary<ulong, TrustMember>();
        if (isTrustParty) {
            trustMemberMap = CreateTrustMemberMap(addon);
        }

        foreach (var hudMember in AgentHUD.Instance()->GetSizedHudMemberSpan()) {
            if (hudMember.EntityId == 0) continue;
            var isSelf = hudMember.EntityId == playerId;
            var isTrustMember = !isSelf && isTrustParty;

            if (isTrustMember && trustMemberMap.TryGetValue(hudMember.EntityId, out var trustMember)) {
                ref var partyMember = ref addon->TrustMembers[trustMember.Index];
                HandlePartyMember(ref partyMember, hudMember, isSelf, isTrustMember, classJob, trustMember.Chara);
            } else {
                ref var partyMember = ref addon->PartyMembers[hudMember.Index];
                HandlePartyMember(ref partyMember, hudMember, isSelf, isTrustMember, classJob);
            }
        }
    }

    private void HandlePartyMember(ref AddonPartyList.PartyListMemberStruct partyMember, HudPartyMember hudMember, bool isSelf, bool isTrustMember, ClassJob classJob, IGameObject? gameObject = null, bool revertToDefault = false) {
        if (config is null) return;
        ref var hudPartyMember = ref PartyListNumberArray.Instance()->PartyMembers[hudMember.Index];
        var currentHealth = hudPartyMember.CurrentHealth;
        var maxHealth = hudPartyMember.MaxHealth;

        if (isTrustMember && gameObject is not null) {
            if (gameObject is not IBattleChara battleChara) return;
            currentHealth = (int)battleChara.CurrentHp;
            maxHealth = (int)battleChara.MaxHp;
        }

        var hpGaugeTextNode = partyMember.HPGaugeComponent->GetTextNodeById(2);
        if (hpGaugeTextNode is not null) {
            if ((isSelf && config.PartyListSelf || (!isSelf && config.PartyListOtherMembers)) && !revertToDefault) {
                hpGaugeTextNode->SetText(GetCorrectText((uint)currentHealth, (uint)maxHealth, config.PartyListHpEnabled));
            } else {
                hpGaugeTextNode->SetText(currentHealth.ToString());
            }
        }

        var resourceGaugeNode = partyMember.MPGaugeBar;
        if (resourceGaugeNode is not null && !isTrustMember) {
            var isCombatClass = !classJob.IsCrafter() && !classJob.IsGatherer();
            if (!isSelf && !isCombatClass) return;

            var shouldRevertResource = (isSelf && !config.PartyListSelf) || (!isSelf && !config.PartyListOtherMembers) || revertToDefault;
            var isMpDisabled = (!config.PartyListMpEnabled || shouldRevertResource) && isCombatClass;
            var resourceGaugeTextNode = resourceGaugeNode->GetTextNodeById(2);
            var resourceGaugeTextSubNode = resourceGaugeNode->GetTextNodeById(3);

            resourceGaugeTextNode->SetXShort((short)(isMpDisabled ? -17 : 4));
            resourceGaugeTextNode->SetText(GetCorrectPartyResourceText((uint)hudPartyMember.CurrentMana, (uint)hudPartyMember.MaxMana, classJob, IsResourcePercentageEnabled(config, classJob), shouldRevertResource));
            resourceGaugeTextSubNode->ToggleVisibility(isMpDisabled);
        }
    }

    private void OnPartyListDisable() {
        var addon = Services.GameGui.GetAddonByName<AddonPartyList>("_PartyList");
        if (addon is null) return;
        if (Services.ClientState.LocalPlayer is not { ClassJob: { IsValid: true, Value: var classJob }, EntityId: var playerId } ) return;

        var isTrustParty = addon->TrustCount > 0;

        var trustMemberMap = new Dictionary<ulong, TrustMember>();
        if (isTrustParty) {
            trustMemberMap = CreateTrustMemberMap(addon);
        }

        foreach (var hudMember in AgentHUD.Instance()->GetSizedHudMemberSpan()) {
            if (hudMember.EntityId == 0) continue;
            var isSelf = hudMember.EntityId == playerId;
            var isTrustMember = !isSelf && isTrustParty;

            if (isTrustMember && trustMemberMap.TryGetValue(hudMember.EntityId, out var trustMember)) {
                ref var partyMember = ref addon->TrustMembers[trustMember.Index];
                HandlePartyMember(ref partyMember, hudMember, isSelf, isTrustMember, classJob, trustMember.Chara, true);
            } else {
                ref var partyMember = ref addon->PartyMembers[hudMember.Index];
                HandlePartyMember(ref partyMember, hudMember, isSelf, isTrustMember, classJob, revertToDefault: true);
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

    private string GetCorrectPartyResourceText(uint current, uint max, ClassJob classJob, bool enabled = true, bool revertToDefault = false) {
        if (revertToDefault || (!classJob.IsCrafter() && !classJob.IsGatherer() && !enabled)) {
            if (!classJob.IsCrafter() && !classJob.IsGatherer())
                current /= 100; // Only divide MP for combat classes
            return current.ToString();
        }
        return GetCorrectText(current, max, enabled);
    }

    private Dictionary<ulong, TrustMember> CreateTrustMemberMap(AddonPartyList* addon) {
        var trustMemberMap = new Dictionary<ulong, TrustMember>();
        for (var i = 0; i < addon->TrustCount; i++) {
            var trustName = addon->TrustMembers[i].Name->GetText().ToString();
            var trustChara = Services.ObjectTable.CharacterManagerObjects
                .FirstOrDefault(member => member.Name.TextValue == SanitizeName(trustName));
            if (trustChara != null)
                trustMemberMap[trustChara.EntityId] = new TrustMember(i, trustChara);
        }
        return trustMemberMap;
    }

    private string FormatPercentage(uint current, uint max) {
        if (config is null) return current.ToString();
        if (max == 0) return "0" + (config.PercentageSignEnabled ? "%" : "");
        var percentage = current / (float)max * 100f;
        var percentSign = config.PercentageSignEnabled ? "%" : "";
        return percentage.ToString($"F{config.DecimalPlaces}", CultureInfo.InvariantCulture) + percentSign;
    }

    private bool IsResourcePercentageEnabled(ResourceBarPercentagesConfig resourceConfig, ClassJob classJob) {
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

    private string SanitizeName(string name) {
        return new string(name.Where(c => !char.IsSurrogate(c) && (c < '\uE000' || c > '\uF8FF')).ToArray()).Trim();
    }

    private record ActiveResource(uint Current, uint Max, bool Enabled);

    private record TrustMember(int Index, IGameObject Chara);
}
