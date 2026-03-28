using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace VanillaPlus.Features.AprilFools;

public class DutyReadyFools : IFoolsModule {
    public required AprilFoolsConfig Config { get; set; }

    private bool lastAfkState;
    
    public void Enable() {
        Services.Framework.Update += OnFrameworkUpdate;
    }

    public void Disable() {
        Services.Framework.Update -= OnFrameworkUpdate;
    }

    private void OnFrameworkUpdate(IFramework framework) {
        if (!Config.DutyPop) return;

        if (lastAfkState != IsPlayerAfk) {
            if (IsPlayerAfk) {
                UIGlobals.PlaySoundEffect(67); // hehe, six seven.
                Services.ChatGui.Print("Oh, sorry, did you think a duty popped? Must have been the wind.", "VanillaPlus");
            }

            lastAfkState = IsPlayerAfk;
        }
    }
    
    private static bool IsPlayerAfk 
        => Services.ObjectTable.LocalPlayer?.OnlineStatus.RowId is 17;
}
