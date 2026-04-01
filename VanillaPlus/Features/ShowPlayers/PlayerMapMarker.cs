using System.Drawing;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using KamiToolKit.Overlay.MapOverlay;

namespace VanillaPlus.Features.ShowPlayers;

public sealed unsafe class PlayerMapMarker : MapMarkerNode {
    public required int PlayerIndex { get; init; }

    public PlayerMapMarker() {
        IconId = 60575;
        Size = new Vector2(16.0f, 16.0f);
    }
    
    protected override void OnUpdate() {
        IsVisible = false;

        var localChara = GameObjectManager.Instance()->Objects.IndexSorted[0].Value;
        if (localChara is null) return;

        var battleChara = CharacterManager.Instance()->BattleCharas[PlayerIndex].Value;
        if (battleChara is null) return;

        if (localChara == battleChara) return;

        if (battleChara->ObjectKind is not ObjectKind.Pc) return;
        if (battleChara->IsPartyMember) return;
        if (!battleChara->GetIsTargetable()) return;
        
        var objectPosition = new Vector2(battleChara->Position.X, battleChara->Position.Z);
        var objectName = battleChara->NameString;
        var objectLevel = battleChara->Level.ToString();

        if (Vector3.Distance(battleChara->Position, localChara->Position) > 150.0f) return;

        if (battleChara->IsFriend) {
            MultiplyColor = KnownColor.Orange.Vector3();
        }
        else {
            MultiplyColor = KnownColor.White.Vector3();
        }
        
        IsVisible = true;
        Position = new Vector2(objectPosition.X, objectPosition.Y);
        TextTooltip = $"Lv. {objectLevel} {objectName}";
        MapId = AgentMap.Instance()->CurrentMapId;
    }
}
