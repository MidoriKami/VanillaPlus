using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.HideMpBars;

public unsafe class HideMpBars : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_HideMpBars,
        Description = Strings.ModificationDescription_HideMpBars,
        Type = ModificationType.UserInterface,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
        Tags = [ "Party List" ],
    };

    private List<uint>? manaUsingClassJobs;

    public override string ImageName => "HideMpBars.png";

    public override void OnEnable() {
        manaUsingClassJobs = Services.DataManager.GetManaUsingClassJobs().ToList();
        
        Services.AddonLifecycle.RegisterListener(AddonEvent.PostDraw, "_PartyList", OnPartyListDraw);
        Services.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "_PartyList", OnPartyListDraw);
    }

    public override void OnDisable() {
        Services.AddonLifecycle.UnregisterListener(OnPartyListDraw);
        
        manaUsingClassJobs = null;
    }
    
    private void OnPartyListDraw(AddonEvent type, AddonArgs args) {
        if (Services.ClientState.IsPvP) return;
        if (manaUsingClassJobs is null) return;
        if (Services.ObjectTable.LocalPlayer is not { ClassJob: { IsValid: true, Value: var classJob }, EntityId: var playerId } localPlayer) return;

        var addon = args.GetAddon<AddonPartyList>();

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
}
