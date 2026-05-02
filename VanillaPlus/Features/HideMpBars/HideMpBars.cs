using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using KamiToolKit.Controllers;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

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

    public override void OnEnable() {
        manaUsingClassJobs = Services.DataManager.GetManaUsingClassJobs().ToList();

        partyListController = new AddonController<AddonPartyList> {
            AddonName = "_PartyList",
            OnPreUpdate = UpdatePartyList,
        };
        partyListController?.Enable();

        paramController = new AddonController<AddonParameterWidget> {
            AddonName = "_ParameterWidget",
            OnPreUpdate = UpdateParamWidget,
        };
        paramController?.Enable();
    }

    public override void OnDisable() {
        partyListController?.Dispose();
        partyListController = null;
        
        paramController?.Dispose();
        paramController = null;
        
        manaUsingClassJobs = null;
    }

    private void UpdatePartyList(AddonPartyList* addon) {
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
        if (manaUsingClassJobs is null) return;
        if (Services.ObjectTable.LocalPlayer is not { ClassJob: { IsValid: true, Value: var classJob } }) return;

        var paramElement = addon->GetNodeById(4);
        if (paramElement is null) return;
        
        paramElement->ToggleVisibility(manaUsingClassJobs.Contains(classJob.RowId) || classJob.IsCrafter || classJob.IsGatherer);
    }
}
