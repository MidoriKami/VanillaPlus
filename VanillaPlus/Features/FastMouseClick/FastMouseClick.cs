using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.FastMouseClick;

public class FastMouseClick : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_FastMouseClick,
        Description = Strings.ModificationDescription_FastMouseClick,
        Type = ModificationType.GameBehavior,
        Authors = ["Haselnussbomber"],
        CompatibilityModule = new HaselTweaksCompatibilityModule("FastMouseClickFix"),
    };

    [Signature("EB 3F B8 ?? ?? ?? ?? 48 8B D7")]
    private nint? memoryAddress;

    private MemoryReplacement? memoryPatch;

    public override async Task OnEnableAsync() {
        Services.GetService<IGameInteropProvider>().InitializeFromAttributes(this);

        if (memoryAddress is { } address && memoryAddress != nint.Zero) {
            memoryPatch = new MemoryReplacement(address, [0x90, 0x90]);
            await Services.GetService<IFramework>().RunSafely(memoryPatch.Enable);
        }
    }

    public override async Task OnDisableAsync() {
        if (memoryPatch is not null) {
            await memoryPatch.DisableAsync();
            memoryPatch = null;
        }

        memoryAddress = null;
    }
}
