using Dalamud.Game.Gui.ContextMenu;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace VanillaPlus.Extensions;

public static unsafe class MenuOpenedArgsExtensions {
    extension(IMenuOpenedArgs args) {
        public AgentContext* AgentContext => (AgentContext*)args.AgentPtr;
    }
}
