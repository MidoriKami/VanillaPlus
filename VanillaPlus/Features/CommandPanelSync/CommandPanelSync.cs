using System.Threading.Tasks;
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
        Services.ClientState.Login += OnLogin;
        Services.ClientState.Logout += OnLogout;

        if (Services.ClientState.IsLoggedIn) {
            await Services.Framework.Run(ApplySharedQuickPanel);
        }
    }

    public override async Task OnDisableAsync() {
        Services.ClientState.Login -= OnLogin;
        Services.ClientState.Logout -= OnLogout;

        if (Services.ClientState.IsLoggedIn) {
            await Services.Framework.Run(RestoreOriginalQuickPanel);
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
        => Data.SaveBinaryData(QuickPanelAddress, QuickPanelSize, "CommandPanelSync", $"Original.v{CurrentVersion}.qpnl.dat");

    private static void LoadOriginal()
        => Data.LoadBinaryData(QuickPanelAddress, QuickPanelSize, "CommandPanelSync", $"Original.v{CurrentVersion}.qpnl.dat");

    private static void SaveShared()
        => Data.SaveBinaryData(QuickPanelAddress, QuickPanelSize, "CommandPanelSync", $"Shared.v{CurrentVersion}.qpnl.dat");

    private static void LoadShared()
        => Data.LoadBinaryData(QuickPanelAddress, QuickPanelSize, "CommandPanelSync", $"Shared.v{CurrentVersion}.qpnl.dat");
}
