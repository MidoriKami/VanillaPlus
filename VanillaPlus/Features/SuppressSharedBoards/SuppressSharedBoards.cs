using System.Threading.Tasks;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.SuppressSharedBoards;

public unsafe class SuppressSharedBoards : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_SuppressSharedBoards,
        Description = Strings.ModificationDescription_SuppressSharedBoards,
        Type = ModificationType.GameBehavior,
        Authors = ["Treezy"],
    };

    private Hook<TofuHelper.TofuHelperData.Delegates.ShowSharedNotification>? showSharedNotificationHook;
    private Hook<TofuHelper.TofuHelperData.Delegates.SaveBoardAndPlaySound>? saveBoardAndPlaySoundHook;

    public override Task OnEnableAsync() {
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

        return Task.CompletedTask;
    }

    public override Task OnDisableAsync() {
        showSharedNotificationHook?.Dispose();
        showSharedNotificationHook = null;

        saveBoardAndPlaySoundHook?.Dispose();
        saveBoardAndPlaySoundHook = null;

        return Task.CompletedTask;
    }
}
