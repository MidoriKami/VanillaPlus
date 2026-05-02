using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using KamiToolKit.Controllers;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.NativeElements.Config;

namespace VanillaPlus.Features.HideMpBars;

public unsafe class HideMpBars : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_HideMpBars,
        Description = Strings.ModificationDescription_HideMpBars,
        Type = ModificationType.UserInterface,
        Authors = [ "MidoriKami" ],
        Tags = [ "Party List" ],
    };

    private List<uint>? manaUsingClassJobs;

    public override string ImageName => "HideMpBars.png";

    private AddonController<AddonPartyList>? partyListController;
    private AddonController<AddonParameterWidget>? paramController;

    private HideMpBarsConfig? config;
    private ConfigAddon? configAddon;

    public override void OnEnable() {
        config = HideMpBarsConfig.Load();

        configAddon = new ConfigAddon {
            Config = config,
            InternalName = "HideMpBarsConfig",
            Title = "Hide MP Bars Config",
        };

        configAddon.AddCategory("General")
            .AddCheckbox("Hide in Party List", nameof(config.HidePartyList))
            .AddCheckbox("Hide in Parameter Widget", nameof(config.HideParamWidget));

        OpenConfigAction = configAddon.Toggle;

        manaUsingClassJobs = Services.DataManager.GetManaUsingClassJobs().ToList();

        partyListController = new AddonController<AddonPartyList> {
            AddonName = "_PartyList",
            OnPreUpdate = UpdatePartyList,
            OnFinalize = addon => {
                foreach (var member in addon->PartyMembers) {
                    member.MPGaugeBar->OwnerNode->ToggleVisibility(true);
                }
            },
        };
        partyListController?.Enable();

        paramController = new AddonController<AddonParameterWidget> {
            AddonName = "_ParameterWidget",
            OnPreUpdate = UpdateParamWidget,
            OnFinalize = addon => {
                var paramElement = addon->GetNodeById(4);
                if (paramElement is null) return;

                paramElement->ToggleVisibility(true);
            },
        };
        paramController?.Enable();
    }

    public override void OnDisable() {
        partyListController?.Dispose();
        partyListController = null;
        
        paramController?.Dispose();
        paramController = null;
        
        manaUsingClassJobs = null;

        configAddon?.Dispose();
        configAddon = null;
        
        config = null;
    }

    private void UpdatePartyList(AddonPartyList* addon) {
        if (config is not { HidePartyList: true }) return;
        if (Services.ClientState.IsPvP) return;
        if (manaUsingClassJobs is null) return;
        if (Services.ObjectTable.LocalPlayer is not { ClassJob: { IsValid: true, Value: var classJob }, EntityId: var playerId } localPlayer) return;

        if (GroupManager.Instance()->MainGroup.MemberCount is 0) {
            if (classJob.IsCrafter || classJob.IsGatherer) return;

            var mpGaugeNode = addon->PartyMembers[0].MPGaugeBar->OwnerNode;
            mpGaugeNode->ToggleVisibility(manaUsingClassJobs.Contains(localPlayer.ClassJob.RowId));
        }
        else {
            foreach (var hudMember in AgentHUD.Instance()->PartyMemberSpan) {
                if (hudMember.Value->EntityId is 0) continue;
                if (hudMember.Value->EntityId == playerId && ( classJob.IsCrafter || classJob.IsGatherer )) continue;
                if (hudMember.Value->Object is null) continue;

                var mpGaugeNode = addon->PartyMembers[hudMember.Value->Index].MPGaugeBar->OwnerNode;
                mpGaugeNode->ToggleVisibility(manaUsingClassJobs.Contains(hudMember.Value->Object->ClassJob));
            }
        }
    }

    private void UpdateParamWidget(AddonParameterWidget* addon) {
        if (config is not { HideParamWidget: true }) return;
        if (Services.ClientState.IsPvP) return;
        if (manaUsingClassJobs is null) return;
        if (Services.ObjectTable.LocalPlayer is not { ClassJob: { IsValid: true, Value: var classJob } }) return;

        var paramElement = addon->GetNodeById(4);
        if (paramElement is null) return;
        
        paramElement->ToggleVisibility(manaUsingClassJobs.Contains(classJob.RowId) || classJob.IsCrafter || classJob.IsGatherer);
    }
}
