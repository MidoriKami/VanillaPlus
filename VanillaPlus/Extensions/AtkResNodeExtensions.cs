using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace VanillaPlus.Extensions;

public static class AtkResNodeExtensions {
    public static Vector2 Size(this ref AtkResNode node)
        => new(node.Width, node.Height);
    
    public static Vector2 Position(this ref AtkResNode node)
        => new(node.X, node.Y);
    
    public static bool CheckCollisionAtCoords(this ref AtkResNode node, Vector2 pos, bool inclusive = true)
        => node.CheckCollisionAtCoords((short)pos.X, (short)pos.Y, inclusive);
}
