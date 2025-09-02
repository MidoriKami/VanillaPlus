using System;
using System.Runtime.CompilerServices;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace VanillaPlus.Extensions;

public static unsafe class AgentHudExtensions {
    public static Span<HudPartyMember> GetSizedHudMemberSpan(ref this AgentHUD instance) {
        var hudMembers = Unsafe.AsPointer(ref instance.PartyMembers[0]);
        var hudMemberCount = instance.PartyMemberCount;
        return new Span<HudPartyMember>(hudMembers, hudMemberCount);
    }
}
