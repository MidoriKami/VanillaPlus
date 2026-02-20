using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace VanillaPlus.Extensions;

public static unsafe class AtkUnitBaseExtensions {
    extension(ref AtkUnitBase addon) {
        public T* GetNodeById<T>(uint nodeId) where T : unmanaged => addon.UldManager.SearchNodeById<T>(nodeId);

        public T* GetComponentById<T>(uint nodeId) where T : unmanaged => (T*)addon.GetComponentByNodeId(nodeId);
        
        public bool IsActuallyVisible => addon.GetIsActuallyVisible();

        public bool IsFocused => addon.GetIsFocused();

        public void SubscribeStringArrayData(StringArrayType arrayType) => addon.SubscribeAtkArrayData(0, (byte)arrayType);
        public void UnsubscribeStringArrayData(StringArrayType arrayType) => addon.UnsubscribeAtkArrayData(0, (byte)arrayType);
        public void SubscribeNumberArrayData(NumberArrayType arrayType) => addon.SubscribeAtkArrayData(1, (byte)arrayType);
        public void UnsubscribeNumberArrayData(NumberArrayType arrayType) => addon.UnsubscribeAtkArrayData(1, (byte)arrayType);
        
        private bool GetIsActuallyVisible() {
            if (!addon.IsVisible) return false;
            if (addon.RootNode is null) return false;
            if (!addon.RootNode->IsVisible()) return false;
            if ((addon.VisibilityFlags & 5) is not 0) return false;

            return true;
        }

        private bool GetIsFocused() {
            foreach (var focusedAddon in RaptureAtkUnitManager.Instance()->FocusedUnitsList.Entries) {
                if (focusedAddon.Value is null) continue;
                if (focusedAddon.Value->NameString == addon.NameString) return true;
            }

            return false;
        }
    }
}
