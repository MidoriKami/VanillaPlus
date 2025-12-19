using System;
using System.Runtime.CompilerServices;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace VanillaPlus.Extensions;

public static unsafe class AgentHudExtensions {
    extension(ref AgentHUD instance) {
        /// <summary>
        /// Gets the correctly sized span for the current number of party members.
        /// </summary>
        public Span<HudPartyMember> PartyMemberSpan => instance.GetSizedHudMemberSpan();

        private Span<HudPartyMember> GetSizedHudMemberSpan() {
            var hudMembers = Unsafe.AsPointer(ref instance.PartyMembers[0]);
            var hudMemberCount = instance.PartyMemberCount;
            return new Span<HudPartyMember>(hudMembers, hudMemberCount);
        }
    }
}
