using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.SuppressSharedBoards;

public unsafe class SuppressSharedBoards : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Suppress Shared Strategy Boards",
        Description = "Completely suppresses any shared Strategy Board.",
        Type = ModificationType.GameBehavior,
        Authors = ["Treezy"],
    };

    private Hook<TofuHelper.TofuHelperData.Delegates.ShowSharedNotification>? showSharedNotificationHook;
    private Hook<TofuHelper.TofuHelperData.Delegates.SaveBoardAndPlaySound>? saveBoardAndPlaySoundHook;

    public override void OnEnable() {
        showSharedNotificationHook = Services.Hooker.HookFromAddress<TofuHelper.TofuHelperData.Delegates.ShowSharedNotification>(
            TofuHelper.TofuHelperData.MemberFunctionPointers.ShowSharedNotification,
            (_, _, _) => { }
        );

        saveBoardAndPlaySoundHook = Services.Hooker.HookFromAddress<TofuHelper.TofuHelperData.Delegates.SaveBoardAndPlaySound>(
            TofuHelper.TofuHelperData.MemberFunctionPointers.SaveBoardAndPlaySound,
            (_, _, _, _, _) => { }
        );

        showSharedNotificationHook?.Enable();
        saveBoardAndPlaySoundHook?.Enable();
    }

    public override void OnDisable() {
        showSharedNotificationHook?.Dispose();
        showSharedNotificationHook = null;

        saveBoardAndPlaySoundHook?.Dispose();
        saveBoardAndPlaySoundHook = null;
    }
}
