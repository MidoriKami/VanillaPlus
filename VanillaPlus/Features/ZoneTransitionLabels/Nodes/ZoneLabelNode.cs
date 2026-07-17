using System.Numerics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using KamiToolKit.UiOverlay;
using VanillaPlus.Utilities;

namespace VanillaPlus.Features.ZoneTransitionLabels.Nodes;

public class ZoneLabelNode : OverlayNode {

    public override OverlayLayer OverlayLayer => OverlayLayer.Background;

    private readonly TextNineGridNode labelNode;
    private readonly ImGuiImageNode imageNode;

    private readonly ZoneWatcher watcher;

    public ZoneLabelNode(ZoneWatcher zoneWatcher) {
        watcher = zoneWatcher;

        labelNode = new TextNineGridNode {
            TextColor = ColorHelper.GetColor(1),
            FontSize = 22,
            FontType = FontType.Axis,
            AlignmentType = AlignmentType.Center,
        };
        labelNode.AttachNode(this);

        imageNode = new ImGuiImageNode {
            Position = new Vector2(-30.0f, 0.0f),
            TexturePath = Assets.GetAssetPath("ZoneTransitionLabels/OverworldIcon.png"),
            Size = new Vector2(20.0f, 20.0f),
            FitTexture = true,
        };
        imageNode.AttachNode(this);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        imageNode.Size = new Vector2(Height, Height);
        imageNode.Position = new Vector2(0.0f, 0.0f);

        labelNode.Size = new Vector2(Width - Height - 4.0f, Height);
        labelNode.Position = new Vector2(imageNode.Bounds.Right + 4.0f, 0.0f);

        Origin = Size / 2.0f;
    }

    protected override void OnUpdate() {
        IsVisible = TryUpdateLabel();
    }

    private Vector3 previousLocation = Vector3.Zero;

    private const float MinDistance = 10f;
    private const float MaxDistance = 350f;

    private readonly Vector2 minScale = new(0.5f, 0.5f);
    private readonly Vector2 maxScale = new(1.0f, 1.0f);

    private unsafe bool TryUpdateLabel() {
        if (IObjectTable.Get().LocalPlayer is not { } playerCharacter) return false;
        var player = (BattleChara*)playerCharacter.Address;

        if (player == null) return false;
        if (watcher.ZoneExits.Count is 0) return false;

        // Get the closest exit
        Vector3 playerPos = player->Position;
        var exit = watcher.GetClosestExit(playerPos, out var closestPoint);

        // If the player is flying, just get the closest point, else get the closest point on the ground.
        var state = player->MoveController.MovementState;
        closestPoint = state != MovementStateOptions.Normal ? closestPoint : exit.GetClosestGroundPoint(playerPos);
        closestPoint.Y += 1;

        // Check distance
        var distance = Vector3.DistanceSquared(playerPos, closestPoint);
        if (distance >= MaxDistance) return false;

        // Lerp to current location (maybe should be toggleable)
        var deltaTime = (float) IFramework.Get().UpdateDelta.TotalSeconds;

        if (IsVisible) {
            closestPoint = Vector3.Lerp(previousLocation, closestPoint, deltaTime * 30f);
        }

        previousLocation = closestPoint;

        // Update screen position
        if (!IGameGui.WorldToScreenAdjusted(closestPoint, out var screenPos)) return false;
        Position = screenPos - Origin;

        // Adjust scale (farther = smaller)
        var scale = ( distance - MinDistance ) / ( MaxDistance - MinDistance );
        Scale = Vector2.Lerp(maxScale, minScale, scale);

        // Update the text / icon
        var name = exit.Name;
        if (labelNode.String != name) {
            labelNode.String = name;

            Size = new Vector2(labelNode.TextNode.GetTextDrawSize(false).X + Height * 2.0f, Height);
            Origin = Size / 2.0f;

            imageNode.TexturePath = exit.TerritoryType.Value.TerritoryIntendedUse.RowId switch {
                0 => Assets.GetAssetPath("ZoneTransitionLabels/CityIcon.png"),      // City
                1 => Assets.GetAssetPath("ZoneTransitionLabels/OverworldIcon.png"), // Overworld
                13 => Assets.GetAssetPath("ZoneTransitionLabels/HousingIcon.png"),  // Housing ward
                _ => Assets.GetAssetPath("ZoneTransitionLabels/OverworldIcon.png"),  // Shouldn't occur, but just in case
            };
        }

        return true;
    }
}
