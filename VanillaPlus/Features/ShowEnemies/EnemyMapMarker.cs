using System.Drawing;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using KamiToolKit.Premade.Nodes;
using BattleNpcSubKind = Dalamud.Game.ClientState.Objects.Enums.BattleNpcSubKind;

namespace VanillaPlus.Features.ShowEnemies;

public sealed unsafe class EnemyMapMarker : MapMarkerNode {
    public required int ObjectIndex { get; init; }

    public EnemyMapMarker() {
        IconId = 60575;
        Size = new Vector2(16.0f, 16.0f);
    }

    protected override void OnUpdate() {
        IsVisible = false;

        var battleChara = CharacterManager.Instance()->BattleCharas[ObjectIndex].Value;
        if (battleChara is null) return;

        var localChara = Services.ObjectTable.LocalPlayer;
        if (localChara is null) return;

        if (battleChara->ObjectKind is not ObjectKind.BattleNpc) return;
        if (battleChara->SubKind != (byte)BattleNpcSubKind.Enemy) return;
        if (!battleChara->GetIsTargetable()) return;
        if (Vector3.Distance(battleChara->Position, localChara.Position) > 150.0f) return;
        
        var objectPosition = new Vector2(battleChara->Position.X, battleChara->Position.Z);
        var objectLevel = battleChara->Level;
        var objectName = battleChara->NameString;
        
        IsVisible = true;
        Position = objectPosition;
        MapId = AgentMap.Instance()->CurrentMapId;

        if (battleChara->IsBoss) {
            if (battleChara->IsAggroed) {
                IconId = 60401;
            }
            else {
                IconId = 60402;
            }

            MultiplyColor = KnownColor.White.Vector3();
            TextTooltip = $"Lv. ?? {objectName}";
            MarkerScale = 4.0f;
        }
        else {
            IconId = 60575;
            MultiplyColor = battleChara->IsAggroed ? KnownColor.OrangeRed.Vector3() : KnownColor.Yellow.Vector3();
            TextTooltip = $"Lv. {objectLevel} {objectName}";
            MarkerScale = 1.0f;
        }
    }
}
