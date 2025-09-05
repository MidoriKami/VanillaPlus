using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Arrays;
using FFXIVClientStructs.Interop;

namespace VanillaPlus.Extensions;

public static unsafe class AddonPartyListExtensions {
    public static List<PartyListHudData> GetHudMembers(ref this AddonPartyList addon) {
        List<PartyListHudData> hudMembers = [];

        var memberCount = AgentHUD.Instance()->PartyMemberCount;

        foreach (var index in Enumerable.Range(0, memberCount)) {
            var hudMember = AgentHUD.Instance()->PartyMembers.GetPointer(index);
            if (hudMember->Object is null) continue;

            // Member is Trust
            if (hudMember->ContentId is 0) {
                hudMembers.Add(new PartyListHudData {
                    HudMember = hudMember,
                    PartyListMember = addon.TrustMembers.GetPointer(index - 1),
                    NumberArrayData = PartyListNumberArray.Instance()->TrustMembers.GetPointer(index - 1),
                });
            }
            else {
                hudMembers.Add(new PartyListHudData {
                    HudMember = hudMember,
                    PartyListMember = addon.PartyMembers.GetPointer(hudMember->Index),
                    NumberArrayData = PartyListNumberArray.Instance()->PartyMembers.GetPointer(hudMember->Index),
                });
            }
        }
        
        return hudMembers;
    }
}

public unsafe class PartyListHudData {
    public required AddonPartyList.PartyListMemberStruct* PartyListMember { get; init; }
    public required HudPartyMember* HudMember { get; init; }
    public required PartyListNumberArray.PartyListMemberNumberArray* NumberArrayData { get; init; }

    public bool IsSelf() {
        if (Services.ClientState.LocalPlayer is not { EntityId: var playerId } ) return false;
        
        return HudMember->EntityId == playerId;
    }
}
