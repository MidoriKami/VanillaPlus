using FFXIVClientStructs.FFXIV.Component.GUI;

namespace VanillaPlus.Extensions;

public static unsafe class AtkResNodeExtensions {
    extension(ref AtkResNode node) {
        public void ShowActionTooltip(uint actionId, string? textLabel = null) {
            fixed (AtkResNode* nodePointer = &node) {
                AtkStage.Instance()->ShowActionTooltip(nodePointer, actionId, textLabel);
            }
        }
    }
}
