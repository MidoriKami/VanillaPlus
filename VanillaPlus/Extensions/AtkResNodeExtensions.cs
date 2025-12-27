using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace VanillaPlus.Extensions;

public static unsafe class AtkResNodeExtensions {
    extension(ref AtkResNode node) {
        public Vector2 Size => new(node.Width, node.Height);

        public Vector2 Position => new(node.X, node.Y);

        public Vector2 ScreenPosition => new(node.ScreenX, node.ScreenY);

        public bool CheckCollisionAtCoords(Vector2 pos, bool inclusive = true)
            => node.CheckCollisionAtCoords((short)pos.X, (short)pos.Y, inclusive);

        public bool IsActuallyVisible => node.GetIsActuallyVisible();
        
        public void ShowActionTooltip(uint actionId, string? textLabel = null) {
            fixed (AtkResNode* nodePointer = &node) {
                AtkStage.Instance()->ShowActionTooltip(nodePointer, actionId, textLabel);
            }
        }

        private bool GetIsActuallyVisible() {
            if (!node.IsVisible()) return false;

            var parentNode = node.ParentNode;

            while (parentNode is not null) {
                if (!parentNode->IsVisible()) return false;
                parentNode = parentNode->ParentNode;
            }

            return true;
        }
    }
}
