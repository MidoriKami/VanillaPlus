using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.Interop;

namespace VanillaPlus.Extensions;

public static unsafe class AgentHudExtensions {
    extension(ref AgentHUD instance) {
        /// <summary>
        /// Gets the correctly sized span for the current number of party members.
        /// </summary>
        public IEnumerable<Pointer<HudPartyMember>> PartyMemberSpan => instance.GetSizedHudMemberSpan();

        private List<Pointer<HudPartyMember>> GetSizedHudMemberSpan() {
            List<Pointer<HudPartyMember>> members = [];
            
            foreach (var member in instance.PartyMembers.PointerEnumerator()) {
                if (member->EntityId is not 0xE0000000) {
                    members.Add(member);
                }
            }
            
            return members;
        }
    }
}
