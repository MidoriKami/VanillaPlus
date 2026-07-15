using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace VanillaPlus.Features.AprilFools;

/// <summary>
/// When the players status changes to AFK, it will play the "Duty Pop" sound effect, and print a message to chat
/// that indicates that it was a prank. The message is tagged with VanillaPlus so the source of the prank is clearly stated.
/// </summary>
public unsafe class DutyReadyFools : FoolsModule {
    private bool lastAfkState;

    public override bool IsEnabledByConfig
        => Config.DutyPop;

    protected override Task OnEnable() {
        Services.GetService<IFramework>().Update += OnFrameworkUpdate;

        return Task.CompletedTask;
    }

    protected override Task OnDisable() {
        Services.GetService<IFramework>().Update -= OnFrameworkUpdate;

        return Task.CompletedTask;
    }

    private void OnFrameworkUpdate(IFramework framework) {
        if (lastAfkState != IsPlayerAfk) {
            if (IsPlayerAfk) {
                UIGlobals.PlaySoundEffect(67); // hehe, six seven.
                Services.GetService<IChatGui>().Print("Oh, sorry, did you think a duty popped? Must have been the wind.", "VanillaPlus");
            }

            lastAfkState = IsPlayerAfk;
        }
    }

    private static bool IsPlayerAfk
        => Services.GetService<IObjectTable>().LocalPlayer?.OnlineStatus.RowId is 17;
}
