using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace VanillaPlus.Features.BiggerConfigWindows;

public static unsafe class ResizeHelpers {
    public static void ResizeScrollBarNode(AtkComponentScrollBar* scrollBar, float sizeAdjustment) {
        if (scrollBar is null) return;

        var scrollBarNode = scrollBar->OwnerNode;
        if (scrollBarNode is null) return;
            
        var parentContainer = scrollBarNode->ParentNode;
        if (parentContainer is null) return;
            
        parentContainer->Size += new Vector2(0.0f, sizeAdjustment);
        scrollBarNode->AtkResNode.Size += new Vector2(0.0f, sizeAdjustment);
        scrollBar->ContentNode->ParentNode->Size += new Vector2(0.0f, sizeAdjustment);
            
        scrollBar->EmptyLength = (int)(scrollBar->EmptyLength + sizeAdjustment);
        scrollBar->ScrollMaxPosition = (int)(scrollBar->ScrollMaxPosition - sizeAdjustment);
        scrollBar->ContentNodeOffScreenLength = (short)(scrollBar->ContentNodeOffScreenLength - sizeAdjustment);
        scrollBar->ScrollbarLength = (short)(scrollBar->ScrollbarLength - sizeAdjustment);

        scrollBarNode->ToggleVisibility(scrollBar->ScrollMaxPosition > 0);

        if (scrollBar->ScrollPosition < scrollBar->ScrollMaxPosition) {
            scrollBar->SetScrollPosition(0);
        }
    }
}
