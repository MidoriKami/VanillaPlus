using System.Collections.Generic;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using KamiToolKit.Controllers;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Native.Addons;

namespace VanillaPlus.Features.HideMpBars;

public class HideMpBars : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_HideMpBars,
        Description = Strings.ModificationDescription_HideMpBars,
        Type = ModificationType.UserInterface,
        Authors = ["MidoriKami"],
        Tags = ["Party List"],
    };

    private List<uint>? manaUsingClassJobs;

    public override string ImageName => "HideMpBars.png";

    private AddonController<AddonPartyList>? partyListController;
    private AddonController<AddonParameterWidget>? paramController;

    private HideMpBarsConfig? config;
    private ConfigAddon? configAddon;

    public override async Task OnEnableAsync() {
        manaUsingClassJobs = [.. IDataManager.Get().GetManaUsingClassJobs()];

        config = await HideMpBarsConfig.Load();

        configAddon = new ConfigAddon {
            Config = config,
            InternalName = "HideMpBarsConfig",
            Title = Strings.HideMpBars_ConfigTitle,
        };

        unsafe {
            configAddon.AddCategory(Strings.CurrencyWarning_CategoryGeneral)
                .AddCheckbox(Strings.HideMpBars_LabelHidePartyList, nameof(config.HidePartyList), _ => ResetPartyList())
                .AddCheckbox(Strings.HideMpBars_LabelHideParameterWidget, nameof(config.HideParamWidget), _ => ResetParamWidget());
        }

        OpenConfigAction = configAddon.Toggle;

        unsafe {
            partyListController = new AddonController<AddonPartyList> {
                AddonName = "_PartyList",
                OnPreUpdate = UpdatePartyList,
                OnFinalize = ResetPartyList,
            };
        }

        await partyListController.EnableAsync();

        unsafe {
            paramController = new AddonController<AddonParameterWidget> {
                AddonName = "_ParameterWidget",
                OnPreUpdate = UpdateParamWidget,
                OnFinalize = ResetParamWidget,
            };
        }

        await paramController.EnableAsync();
    }

    public override async Task OnDisableAsync() {
        await partyListController.DisposeAsyncSafe();
        partyListController = null;

        await paramController.DisposeAsyncSafe();
        paramController = null;

        await configAddon.DisposeAsyncSafe();
        configAddon = null;

        manaUsingClassJobs = null;
        config = null;
    }

    private unsafe void UpdatePartyList(AddonPartyList* addon) {
        if (IClientState.Get().IsPvP) return;
        if (manaUsingClassJobs is null) return;
        if (IObjectTable.Get().LocalPlayer is not { ClassJob: { IsValid: true, Value: var classJob }, EntityId: var playerId } localPlayer) return;

        var isEnabled = config is { HidePartyList: true };

        if (GroupManager.Instance()->MainGroup.MemberCount is 0) {
            var mpGaugeNode = addon->PartyMembers[0].MPGaugeBar->OwnerNode;
            mpGaugeNode->ToggleVisibility(!isEnabled || manaUsingClassJobs.Contains(localPlayer.ClassJob.RowId) || classJob.IsNonCombatant);
        }
        else {
            foreach (var hudMember in AgentHUD.Instance()->PartyMemberSpan) {
                if (hudMember.Value->EntityId is 0) continue;
                if (hudMember.Value->EntityId == playerId && (classJob.IsCrafter || classJob.IsGatherer)) continue;
                if (hudMember.Value->Object is null) continue;

                var mpGaugeNode = addon->PartyMembers[hudMember.Value->Index].MPGaugeBar->OwnerNode;
                mpGaugeNode->ToggleVisibility(!isEnabled || manaUsingClassJobs.Contains(hudMember.Value->Object->ClassJob));
            }
        }
    }

    private unsafe void UpdateParamWidget(AddonParameterWidget* addon) {
        if (IClientState.Get().IsPvP) return;
        if (manaUsingClassJobs is null) return;
        if (IObjectTable.Get().LocalPlayer is not { ClassJob: { IsValid: true, Value: var classJob } }) return;

        var isEnabled = config is { HideParamWidget: true };

        var paramElement = addon->GetNodeById(4);
        if (paramElement is null) return;

        paramElement->ToggleVisibility(!isEnabled || manaUsingClassJobs.Contains(classJob.RowId) || classJob.IsNonCombatant);
    }

    private static unsafe void ResetPartyList(AddonPartyList* addon = null) {
        if (addon is null) {
            addon = IGameGui.Get().GetAddonByName<AddonPartyList>("_PartyList");
        }
        if (addon is null) return;

        foreach (var member in addon->PartyMembers) {
            member.MPGaugeBar->OwnerNode->ToggleVisibility(true);
        }
    }

    private static unsafe void ResetParamWidget(AddonParameterWidget* addon = null) {
        if (addon is null) {
            addon = IGameGui.Get().GetAddonByName<AddonParameterWidget>("_ParameterWidget");
        }
        if (addon is null) return;

        var paramElement = addon->GetNodeById(4);
        if (paramElement is null) return;

        paramElement->ToggleVisibility(true);
    }
}
