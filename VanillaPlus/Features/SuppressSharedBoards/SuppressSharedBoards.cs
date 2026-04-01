using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.SuppressSharedBoards;

public class SuppressSharedBoards : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Suppress Shared Strategy Boards",
        Description = "Completely suppresses any shared Strategy Board.",
        Type = ModificationType.GameBehavior,
        Authors = ["Treezy"],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    private delegate void ReceiveSharedPopupDelegate(nint thisPtr, byte a2, bool a3);
    [Signature("48 89 6C 24 ?? 56 41 56 41 57 48 83 EC ?? 4C 8B F9 0F B6 EA", DetourName = nameof(ReceiveSharedPopupDetour))]
    private Hook<ReceiveSharedPopupDelegate>? receiveSharedPopupHook;

    private delegate void ReceiveSharedSaveDelegate(nint thisPtr, nint a2, nint a3, int a4, uint a5);
    [Signature("E8 ?? ?? ?? ?? 40 80 F5", DetourName = nameof(ReceiveSharedSaveDetour))]
    private Hook<ReceiveSharedSaveDelegate>? receiveSharedSaveHook;

    public override void OnEnable() {
        Services.GameInteropProvider.InitializeFromAttributes(this);
        receiveSharedPopupHook?.Enable();
        receiveSharedSaveHook?.Enable();
    }

    public override void OnDisable() {
        receiveSharedPopupHook?.Dispose();
        receiveSharedPopupHook = null;
        receiveSharedSaveHook?.Dispose();
        receiveSharedSaveHook = null;
    }

    private void ReceiveSharedPopupDetour(nint thisPtr, byte a2, bool a3) { }

    private void ReceiveSharedSaveDetour(nint thisPtr, nint a2, nint a3, int a4, uint a5) { }
}
