using System.Threading.Tasks;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.RightClickMapNav;

public class RightClickMapNav : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Right-Click Map Nav",
        Description = "Allows you to navigate 'up' a layer by right clicking on the map.",
        Type = ModificationType.GameBehavior,
        Authors = ["MidoriKami"],
    };

    public override Task OnEnableAsync() {
        IAddonLifecycle.Get().RegisterListener(AddonEvent.PreReceiveEvent, "AreaMap", OnAreaMapReceiveEvent);

        return Task.CompletedTask;
    }

    public override Task OnDisableAsync() {
        IAddonLifecycle.Get().UnregisterListener(OnAreaMapReceiveEvent);

        return Task.CompletedTask;
    }

    private static unsafe void OnAreaMapReceiveEvent(AddonEvent type, AddonArgs args) {
        if (args is not AddonReceiveEventArgs receiveEventArgs) return;

        var addon = args.GetAddon<AddonAreaMap>();
        if (!addon->MapUpButton->OwnerNode->NodeFlags.HasFlag(NodeFlags.Enabled)) return;

        var eventData = (AtkEventData*)receiveEventArgs.AtkEventData;
        if (!eventData->IsRightClick) return;

        AgentMap.Instance()->AgentInterface.SendCommand(0, [5]);

        IPluginLog.Get().Debug("RightClickMapNav prevented map right click to nav up.");
        args.PreventOriginal();
    }
}
