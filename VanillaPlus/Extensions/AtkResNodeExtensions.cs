using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace VanillaPlus.Extensions;

public static unsafe class AtkResNodeExtensions {
    public static Vector2 Size(this ref AtkResNode node)
        => new(node.Width, node.Height);

    public static Vector2 Position(this ref AtkResNode node)
        => new(node.X, node.Y);

    public static Vector2 ScreenPosition(this ref AtkResNode node)
        => new(node.ScreenX, node.ScreenY);

    public static bool CheckCollisionAtCoords(this ref AtkResNode node, Vector2 pos, bool inclusive = true)
        => node.CheckCollisionAtCoords((short)pos.X, (short)pos.Y, inclusive);

    public static bool IsActuallyVisible(this ref AtkResNode node) {
        if (!node.IsVisible()) return false;

        var parentNode = node.ParentNode;

        while (parentNode is not null) {
            if (!parentNode->IsVisible()) return false;
            parentNode = parentNode->ParentNode;
        }

        return true;
    }
}
