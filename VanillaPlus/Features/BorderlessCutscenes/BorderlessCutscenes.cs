using System.Threading.Tasks;
using Dalamud.Utility.Signatures;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.BorderlessCutscenes;

public class BorderlessCutscenes : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_BorderlessCutscenes,
        Description = Strings.ModificationDescription_BorderlessCutscenes,
        Type = ModificationType.GameBehavior,
        Authors = ["goat", "Maple", "MidoriKami"],
        CompatibilityModule = new PluginCompatibilityModule("Dalamud.FullscreenCutscenes"),
    };

    [Signature("01 0F 84 ?? ?? ?? ?? 48 8B 0D ?? ?? ?? ?? 0F 57 D2 F3 0F 10 1D ?? ?? ?? ?? 0F 57 C9 48 89 B4 24")]
    private nint? memoryAddress;

    private MemoryReplacement? jumpPatch;

    public override async Task OnEnableAsync() {
        Services.GameInteropProvider.InitializeFromAttributes(this);

        if (memoryAddress is { } address && memoryAddress != nint.Zero) {
            jumpPatch = new MemoryReplacement(address, [0x00]);
            await Services.Framework.Run(jumpPatch.Enable);
        }
    }

    public override async Task OnDisableAsync() {
        await Task.WhenAll(jumpPatch?.DisposeAsync().AsTask() ?? Task.CompletedTask);
        jumpPatch = null;

        memoryAddress = null;
    }
}
