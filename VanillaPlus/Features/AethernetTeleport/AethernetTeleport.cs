using System.Threading.Tasks;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Agent;
using Dalamud.Game.Agent.AgentArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Dalamud.Excel.Sheets;
using Lumina.Extensions;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using AgentId = Dalamud.Game.Agent.AgentId;

namespace VanillaPlus.Features.AethernetTeleport;

public class AethernetTeleport : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Aethernet Teleport",
        Description = "Teleports you to the main aetheryte when clicking on an aethernet from the game map.",
        Type = ModificationType.GameBehavior,
        Authors = ["MidoriKami"],
        CompatibilityModule = new PluginCompatibilityModule("Mappy"),
    };

    private int? hoveredMarkerIndex;

    public override Task OnEnableAsync() {
        IAgentLifecycle.Get().RegisterListener(AgentEvent.PreReceiveEvent, AgentId.Map, OnAgentEvent);
        IAddonLifecycle.Get().RegisterListener(AddonEvent.PreReceiveEvent, "AreaMap", OnMapEvent);

        return Task.CompletedTask;
    }

    public override Task OnDisableAsync() {
        IAgentLifecycle.Get().UnregisterListener(OnAgentEvent);
        IAddonLifecycle.Get().UnregisterListener(OnMapEvent);

        return Task.CompletedTask;
    }

    private unsafe void OnMapEvent(AddonEvent type, AddonArgs args) {
        if (args is not AddonReceiveEventArgs receiveArgs) return;

        switch (receiveArgs) {
            case { EventType: AtkEventType.MouseMove }:
                hoveredMarkerIndex = null;
                return;

            case { EventType: AtkEventType.MouseUp, EventParam: 16 } when hoveredMarkerIndex is { } markerIndex: {
                MapMarkerInfo? markerInfo = AgentMap.Instance()->MapMarkers[markerIndex];

                if (markerInfo is not { } markerData) return;

                var aetheryteInfo = IDataManager.Get()
                    .GetExcelSheet<Aetheryte>()
                    .FirstOrNull(aetheryte => aetheryte.AethernetName.RowId == markerData.DataKey);

                if (aetheryteInfo is null) return;

                var aethernetGroup = aetheryteInfo.Value.AethernetGroup;

                var mainAetheryte = IDataManager.Get()
                    .GetExcelSheet<Aetheryte>()
                    .FirstOrNull(aetheryte => aetheryte.IsAetheryte && aetheryte.AethernetGroup == aethernetGroup);

                if (mainAetheryte is null) return;

                using var rentedStringBuilder = new RentedSeStringBuilder();
                var teleportMessage = rentedStringBuilder
                    .PushColorType(45)
                    .Append("[VanillaPlus] ")
                    .PopColorType()
                    .PushColorType(62)
                    .Append("[Teleport] ")
                    .PopColorType()
                    .Append("Teleporting to ")
                    .PushColorType(576)
                    .Append(mainAetheryte.Value.PlaceName.Value.Name.ToString())
                    .PopColorType()
                    .ToReadOnlySeString();

                Telepo.Instance()->Teleport(mainAetheryte.Value.RowId, 0);

                IChatGui.Get().Print(teleportMessage);
                return;
            }
        }
    }

    private void OnAgentEvent(AgentEvent type, AgentArgs args) {
        if (args is not AgentReceiveEventArgs receiveArgs) return;

        switch (receiveArgs) {
            case { EventKind: 0, ValueCount: 2 } when receiveArgs.AtkValueSpan[0] is { Type: AtkValueType.Int, Int: 9 }:
                var markerIndexInfo = receiveArgs.AtkValueSpan[1].Int;

                // Unused, but can be used to check which specific AgentMap array to check for the hovered marker.
                // var maskedType = markerIndexInfo >> 8;

                var maskedIndex = markerIndexInfo & 0xFFFFFF;

                hoveredMarkerIndex = maskedIndex;
                return;
        }
    }
}
