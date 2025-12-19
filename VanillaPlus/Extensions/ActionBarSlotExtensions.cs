using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace VanillaPlus.Extensions;

public static unsafe class ActionBarSlotExtensions {
    extension(ref ActionBarSlot slot) {
        public AtkImageNode* ImageNode => slot.GetImageNode();
        public AtkResNode* FrameNode => slot.GetFrameNode();
        public AtkComponentIcon* IconComponent => slot.GetIconComponent();

        private AtkImageNode* GetImageNode() {
            var component = slot.GetIconComponent();
            if (component is null) return null;

            return component->IconImage;
        }

        private AtkResNode* GetFrameNode() {
            var component = slot.GetIconComponent();
            if (component is null) return null;

            return component->Frame;
        }

        private AtkComponentIcon* GetIconComponent() {
            if (slot.Icon is null) return null;
            return (AtkComponentIcon*) slot.Icon->Component;
        }
    }
}
