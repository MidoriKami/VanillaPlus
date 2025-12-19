using FFXIVClientStructs.FFXIV.Component.GUI;

namespace VanillaPlus.Extensions;

public static unsafe class AtkUldManagerExtensions {
    extension(AtkUldManager atkUldManager) {
        public T* SearchNodeById<T>(uint nodeId) where T : unmanaged {
            foreach (var node in atkUldManager.Nodes) {
                if (node.Value is not null) {
                    if (node.Value->NodeId == nodeId)
                        return (T*) node.Value;
                }
            }

            return null;
        }

        public AtkResNode* SearchNodeById(uint nodeId)
            => atkUldManager.SearchNodeById<AtkResNode>(nodeId);
    }
}
