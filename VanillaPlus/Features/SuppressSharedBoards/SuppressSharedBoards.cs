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

    private delegate void ReceiveSharedPopupDelegate(ulong a1, byte a2, byte a3);
    [Signature("48 89 6C 24 ?? 56 41 56 41 57 48 83 EC ?? 4C 8B F9 0F B6 EA", DetourName = nameof(ReceiveSharedPopupDetour))]
    private Hook<ReceiveSharedPopupDelegate> receiveSharedPopupHook = null!;

    private delegate void ReceiveSharedSaveDelegate(ulong a1, ulong a2, ulong a3, int a4, uint a5);
    [Signature("E8 ?? ?? ?? ?? 40 80 F5", DetourName = nameof(ReceiveSharedSaveDetour))]
    private Hook<ReceiveSharedSaveDelegate> receiveSharedSaveHook = null!;

    public override void OnEnable() {
        Services.GameInteropProvider.InitializeFromAttributes(this);
        receiveSharedPopupHook.Enable();
        receiveSharedSaveHook.Enable();
    }

    public override void OnDisable() {
        receiveSharedPopupHook.Dispose();
        receiveSharedSaveHook.Dispose();
    }

    private void ReceiveSharedPopupDetour(ulong a1, byte a2, byte a3) {
        return;
    }

    private void ReceiveSharedSaveDetour(ulong a1, ulong a2, ulong a3, int a4, uint a5) {
        return;
    }
}
