using System;
using Dalamud.Game.Agent.AgentArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace VanillaPlus.Extensions;

public static class AgentReceiveEventArgsExtensions {
    extension(AgentReceiveEventArgs args) {
        public unsafe Span<AtkValue> AtkValueSpan
            => new((AtkValue*) args.AtkValues, (int) args.ValueCount);
    }
}
