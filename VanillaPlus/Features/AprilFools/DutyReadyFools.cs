using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace VanillaPlus.Features.AprilFools;

/// <summary>
/// When the players status changes to AFK, it will play the "Duty Pop" sound effect, and print a message to chat
/// that indicates that it was a prank. The message is tagged with VanillaPlus so the source of the prank is clearly stated.
/// </summary>
public class DutyReadyFools : FoolsModule {
    private bool lastAfkState;
    
    protected override void OnEnable()
        => Services.Framework.Update += OnFrameworkUpdate;

    protected override void OnDisable()
        => Services.Framework.Update -= OnFrameworkUpdate;

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
