﻿using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using VanillaPlus.Classes;
using VanillaPlus.Extensions;

namespace VanillaPlus.Features.HideMpBars;

public unsafe class HideMpBars : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Hide MP Bars",
        Description = "Hides MP Bars in party list for jobs that don't use MP.",
        Type = ModificationType.UserInterface,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "InitialChangelog"),
        ],
        Tags = [ "Party List" ],
    };

    private List<uint>? manaUsingClassJobs;
    
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
        if (manaUsingClassJobs is null) return;
        if (Services.ClientState.LocalPlayer is not { ClassJob: { IsValid: true, Value: var classJob }, EntityId: var playerId } localPlayer) return;

        var addon = args.GetAddon<AddonPartyList>();

        if (GroupManager.Instance()->MainGroup.MemberCount is 0) {
            if (classJob.IsCrafter() || classJob.IsGatherer()) return;

            var mpGaugeNode = addon->PartyMembers[0].MPGaugeBar->OwnerNode;
            mpGaugeNode->ToggleVisibility(manaUsingClassJobs.Contains(localPlayer.ClassJob.RowId));
        }
        else {
            foreach (var hudMember in AgentHUD.Instance()->GetSizedHudMemberSpan()) {
                if (hudMember.EntityId is 0) continue;
                if (hudMember.EntityId == playerId && ( classJob.IsCrafter() || classJob.IsGatherer() )) continue;
                
                var mpGaugeNode = addon->PartyMembers[hudMember.Index].MPGaugeBar->OwnerNode;
                mpGaugeNode->ToggleVisibility(manaUsingClassJobs.Contains(hudMember.Object->ClassJob));
            }
        }
    }
}
