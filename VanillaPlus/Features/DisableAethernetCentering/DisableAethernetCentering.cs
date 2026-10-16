using System.Threading.Tasks;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.DisableAethernetCentering;

public class DisableAethernetCentering : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Disable Aethernet Centering",
        Description = "Prevents the map from opening centered on the player when using aethernets.",
        Type = ModificationType.GameBehavior,
        Authors = ["MidoriKami"],
    };

    private unsafe delegate void SetPositionDelegate(Atk2DAreaMap* thisPtr, float x, float y);
    private Hook<SetPositionDelegate>? setPositionHook;

    public override async Task OnEnableAsync() {
        unsafe {
            // Pending being added to ClientStructs as Atk2DAreaMap->SetPosition(float, float);
            setPositionHook = IGameInteropProvider.Get().HookFromSignature<SetPositionDelegate>(
                "8B 41 ?? 89 41 ?? 8B 41 ?? 89 41 ?? F3 0F 11 49",
                (_, _, _) => { }
            );
        }

        await setPositionHook.EnableAsync();
    }

    public override async Task OnDisableAsync() {
        await setPositionHook.DisposeAsync();
        setPositionHook = null;
    }
}
