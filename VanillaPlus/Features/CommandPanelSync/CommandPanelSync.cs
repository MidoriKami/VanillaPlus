using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Utilities;

namespace VanillaPlus.Features.CommandPanelSync;

public unsafe class CommandPanelSync : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Command Panel Sync",
        Description = "Synchronizes the command panel across all your characters.",
        Type = ModificationType.GameBehavior,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    public override void OnEnable() {
        Services.ClientState.Login += OnLogin;
        Services.ClientState.Logout += OnLogout;
        
        if (Services.ClientState.IsLoggedIn) {
            ApplySharedQuickPanel();
        }
    }

    public override void OnDisable()
        => RestoreOriginalQuickPanel();

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
        => FileHelpers.GetFileInfo("Data", "CommandPanelSync", "Original.qpnl.dat").Exists;
    
    private static bool SharedExists
        => FileHelpers.GetFileInfo("Data", "CommandPanelSync", "Shared.qpnl.dat").Exists;
    
    private static void SaveOriginal()
        => Data.SaveBinaryData(QuickPanelModule.Instance(), sizeof(QuickPanelModule), "CommandPanelSync", "Original.qpnl.dat");
    
    private static void LoadOriginal()
        => Data.LoadBinaryData(QuickPanelModule.Instance(), sizeof(QuickPanelModule), "CommandPanelSync", "Original.qpnl.dat");
    
    private static void SaveShared()
        => Data.SaveBinaryData(QuickPanelModule.Instance(), sizeof(QuickPanelModule), "CommandPanelSync", "Shared.qpnl.dat");
    
    private static void LoadShared()
        => Data.LoadBinaryData(QuickPanelModule.Instance(), sizeof(QuickPanelModule), "CommandPanelSync", "Shared.qpnl.dat");
}
