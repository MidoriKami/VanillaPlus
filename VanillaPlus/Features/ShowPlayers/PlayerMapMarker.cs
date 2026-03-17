using System.Drawing;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using KamiToolKit.Premade.Nodes;

namespace VanillaPlus.Features.ShowPlayers;

public sealed unsafe class PlayerMapMarker : MapMarkerNode {
    public required int PlayerIndex { get; init; }

    public PlayerMapMarker() {
        IconId = 60575;
        Size = new Vector2(16.0f, 16.0f);
        MultiplyColor = KnownColor.DeepSkyBlue.Vector3();
    }
    
    protected override void OnUpdate() {
        IsVisible = false;

        var localChara = Services.ObjectTable.LocalPlayer;
        if (localChara is null) return;
        
        if (PlayerIndex >= Services.ObjectTable.PlayerObjects.Count()) return;

        var player = Services.ObjectTable.PlayerObjects.ElementAt(PlayerIndex);
        if (!player.IsTargetable) return;
        if (Vector3.Distance(player.Position, localChara.Position) > 150.0f) return;
        
        IsVisible = true;
        Position = new Vector2(player.Position.X, player.Position.Z);
        TextTooltip = $"Lv. {player.Level} {player.Name}";
        MapId = AgentMap.Instance()->CurrentMapId;
    }
}
