using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace VanillaPlus.Extensions;

public static unsafe class AtkComponentScrollBarExtensions {
    extension(ref AtkComponentScrollBar scrollBar) {
        public void ResizeHeight(float sizeAdjustment) {
            var scrollBarNode = scrollBar.OwnerNode;
            if (scrollBarNode is null) return;

            var parentContainer = scrollBarNode->ParentNode;
            if (parentContainer is null) return;

            parentContainer->Size += new Vector2(0.0f, sizeAdjustment);
            scrollBarNode->AtkResNode.Size += new Vector2(0.0f, sizeAdjustment);
            scrollBar.ContentNode->ParentNode->Size += new Vector2(0.0f, sizeAdjustment);
            scrollBar.ContentCollisionNode->AtkResNode.Size += new Vector2(0.0f, sizeAdjustment);

            scrollBar.EmptyLength = (int)(scrollBar.EmptyLength + sizeAdjustment);
            scrollBar.ScrollMaxPosition = (int)(scrollBar.ScrollMaxPosition - sizeAdjustment);
            scrollBar.ContentNodeOffScreenLength = (short)(scrollBar.ContentNodeOffScreenLength - sizeAdjustment);
            scrollBar.ScrollbarLength = (short)(scrollBar.ScrollbarLength - sizeAdjustment);

            scrollBarNode->ToggleVisibility(scrollBar.ScrollMaxPosition > 0);

            if (scrollBar.ScrollPosition < scrollBar.ScrollMaxPosition) {
                scrollBar.SetScrollPosition(0);
            }
        }
    }
}
