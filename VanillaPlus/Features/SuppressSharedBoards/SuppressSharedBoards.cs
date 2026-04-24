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
        Authors = ["Treezy"]
    };

    // TODO: Replace with CS in 7.5
    // https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Client/UI/Misc/TofuHelper.cs#L58
    private delegate void ShowSharedNotificationDelegate(nint thisPtr, bool isNotRealTimeSharing, bool openNotif);
    [Signature("48 89 6C 24 ?? 56 41 56 41 57 48 83 EC ?? 4C 8B F9 0F B6 EA", DetourName = nameof(ShowSharedNotificationDetour))]
    private Hook<ShowSharedNotificationDelegate>? showSharedNotificationHook;

    // https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Client/UI/Misc/TofuHelper.cs#L50
    private delegate void SaveBoardAndPlaySoundDelegate(nint thisPtr, nint packetData, nint boardInfo, uint boardIndexInSharedFolder, uint totalBoardsInSharedFolder);
    [Signature("E8 ?? ?? ?? ?? 40 80 F5", DetourName = nameof(SaveBoardAndPlaySoundDetour))]
    private Hook<SaveBoardAndPlaySoundDelegate>? saveBoardAndPlaySoundHook;

    public override void OnEnable() {
        Services.GameInteropProvider.InitializeFromAttributes(this);
        showSharedNotificationHook?.Enable();
        saveBoardAndPlaySoundHook?.Enable();
    }

    public override void OnDisable() {
        showSharedNotificationHook?.Dispose();
        showSharedNotificationHook = null;
        saveBoardAndPlaySoundHook?.Dispose();
        saveBoardAndPlaySoundHook = null;
    }

    private void ShowSharedNotificationDetour(nint thisPtr, bool isNotRealTimeSharing, bool openNotif) { }

    private void SaveBoardAndPlaySoundDetour(nint thisPtr, nint packetData, nint boardInfo, uint boardIndexInSharedFolder, uint totalBoardsInSharedFolder) { }
}
