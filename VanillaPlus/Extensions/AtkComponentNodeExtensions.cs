using FFXIVClientStructs.FFXIV.Component.GUI;

namespace VanillaPlus.Extensions;

public static unsafe class AtkComponentNodeExtensions {
    extension(ref AtkComponentNode node) {
        public T* SearchNodeById<T>(uint id) where T : unmanaged
            => node.Component->UldManager.SearchNodeById<T>(id);

        public void FadeNode(float fadePercentage) {
            node.MultiplyRed = (byte) ((1 - fadePercentage) * 100);
            node.MultiplyGreen = (byte) ((1 - fadePercentage) * 100);
            node.MultiplyBlue = (byte) ((1 - fadePercentage) * 100);
        }
    }

}
