using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace VanillaPlus.Extensions;

public static unsafe class AtkUnitBaseExtensions {
    /// <param name="addon">Pointer to the addon you wish to resize</param>
    extension(ref AtkUnitBase addon) {
        public T* GetNodeById<T>(uint nodeId) where T : unmanaged => addon.UldManager.SearchNodeById<T>(nodeId);

        public T* GetComponentById<T>(uint nodeId) where T : unmanaged => (T*)addon.GetComponentByNodeId(nodeId);
        
        public bool IsActuallyVisible => addon.GetIsActuallyVisible();

        public void SubscribeStringArrayData(StringArrayType arrayType) => addon.SubscribeAtkArrayData(0, (byte)arrayType);
        public void UnsubscribeStringArrayData(StringArrayType arrayType) => addon.UnsubscribeAtkArrayData(0, (byte)arrayType);
        public void SubscribeNumberArrayData(NumberArrayType arrayType) => addon.SubscribeAtkArrayData(1, (byte)arrayType);
        public void UnsubscribeNumberArrayData(NumberArrayType arrayType) => addon.SubscribeAtkArrayData(1, (byte)arrayType);
        
        private bool GetIsActuallyVisible() {
            if (!addon.IsVisible) return false;
            if (addon.RootNode is null) return false;
            if (!addon.RootNode->IsVisible()) return false;
            if ((addon.VisibilityFlags & 5) is not 0) return false;

            return true;
        }

        /// <summary>
        /// Resizes the target addon to the new size, making sure to adjust various WindowNode properties
        /// to make the window appear and behave normally.
        /// </summary>
        /// <param name="newSize">The new size of the addon</param>
        public void Resize(Vector2 newSize) {
            var windowNode = addon.WindowNode;
            if (windowNode is null) return;

            addon.WindowNode->SetWidth((ushort)newSize.X);
            addon.WindowNode->SetHeight((ushort)newSize.Y);

            if (addon.WindowHeaderCollisionNode is not null) {
                addon.WindowHeaderCollisionNode->SetWidth((ushort)(newSize.X - 14.0f));
            }

            addon.SetSize((ushort)newSize.X, (ushort)newSize.Y);

            addon.WindowNode->Component->UldManager.UpdateDrawNodeList();
            addon.UpdateCollisionNodeList(false);
        }
    }
}
