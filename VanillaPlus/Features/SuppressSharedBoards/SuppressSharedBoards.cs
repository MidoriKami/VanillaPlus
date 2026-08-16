using System.Threading.Tasks;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.SuppressSharedBoards;

public class SuppressSharedBoards : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_SuppressSharedBoards,
        Description = Strings.ModificationDescription_SuppressSharedBoards,
        Type = ModificationType.GameBehavior,
        Authors = ["Treezy"],
    };

    private Hook<TofuHelper.TofuHelperData.Delegates.ShowSharedNotification>? showSharedNotificationHook;
    private Hook<TofuHelper.TofuHelperData.Delegates.SaveBoardAndPlaySound>? saveBoardAndPlaySoundHook;

    public override async Task OnEnableAsync() {
        unsafe {
            showSharedNotificationHook = IGameInteropProvider.Get().HookFromAddress<TofuHelper.TofuHelperData.Delegates.ShowSharedNotification>(
                TofuHelper.TofuHelperData.MemberFunctionPointers.ShowSharedNotification,
                (_, _, _) => { }
            );
        }

        await showSharedNotificationHook.EnableAsync();

        unsafe {
            saveBoardAndPlaySoundHook = IGameInteropProvider.Get().HookFromAddress<TofuHelper.TofuHelperData.Delegates.SaveBoardAndPlaySound>(
                TofuHelper.TofuHelperData.MemberFunctionPointers.SaveBoardAndPlaySound,
                (_, _, _, _, _) => { }
            );
        }

        await saveBoardAndPlaySoundHook.EnableAsync();
    }

    public override async Task OnDisableAsync() {
        await showSharedNotificationHook.DisposeAsync();
        showSharedNotificationHook = null;

        await saveBoardAndPlaySoundHook.DisposeAsync();
        saveBoardAndPlaySoundHook = null;
    }
}
