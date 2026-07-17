using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Utilities;

namespace VanillaPlus.Features.CommandPanelSync;

public class CommandPanelSync : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.CommandPanelSync_DisplayName,
        Description = Strings.CommandPanelSync_Description,
        Type = ModificationType.GameBehavior,
        Authors = ["MidoriKami"],
    };

    private const int CurrentVersion = 2;

    public override async Task OnEnableAsync() {
        IClientState.Get().Login += OnLogin;
        IClientState.Get().Logout += OnLogout;

        if (IClientState.Get().IsLoggedIn) {
            await IFramework.Get().RunSafely(ApplySharedQuickPanel);
        }
    }

    public override async Task OnDisableAsync() {
        IClientState.Get().Login -= OnLogin;
        IClientState.Get().Logout -= OnLogout;

        if (IClientState.Get().IsLoggedIn) {
            await IFramework.Get().RunSafely(RestoreOriginalQuickPanel);
        }
    }

    private static void OnLogin()
        => ApplySharedQuickPanel();

    private static void OnLogout(int type, int code)
        => RestoreOriginalQuickPanel();

    private static void ApplySharedQuickPanel() {
        SaveOriginal();

        if (SharedExists) {
            LoadShared();
        }
        else {
            SaveShared();
        }
    }

    private static void RestoreOriginalQuickPanel() {
        SaveShared();

        if (OriginalExists) {
            LoadOriginal();
        }
    }

    private static bool OriginalExists
        => FileHelpers.GetFileInfo("Data", "CommandPanelSync", $"Original.v{CurrentVersion}.qpnl.dat").Exists;

    private static bool SharedExists
        => FileHelpers.GetFileInfo("Data", "CommandPanelSync", $"Shared.v{CurrentVersion}.qpnl.dat").Exists;

    private static unsafe nint QuickPanelAddress => (nint)QuickPanelModule.Instance() + sizeof(UserFileManager.UserFileEvent);

    private static unsafe int QuickPanelSize => sizeof(QuickPanelModule) - sizeof(UserFileManager.UserFileEvent);

    private static void SaveOriginal()
        => Task.Run(() => Data.SaveBinaryData(QuickPanelAddress, QuickPanelSize, "CommandPanelSync", $"Original.v{CurrentVersion}.qpnl.dat"));

    private static void LoadOriginal()
        => Task.Run(() => Data.LoadBinaryData(QuickPanelAddress, QuickPanelSize, "CommandPanelSync", $"Original.v{CurrentVersion}.qpnl.dat"));

    private static void SaveShared()
        => Task.Run(() => Data.SaveBinaryData(QuickPanelAddress, QuickPanelSize, "CommandPanelSync", $"Shared.v{CurrentVersion}.qpnl.dat"));

    private static void LoadShared()
        => Task.Run(() => Data.LoadBinaryData(QuickPanelAddress, QuickPanelSize, "CommandPanelSync", $"Shared.v{CurrentVersion}.qpnl.dat"));
}
