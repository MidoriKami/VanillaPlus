using Dalamud.Utility.Signatures;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.BorderlessCutscenes;

public class BorderlessCutscenes : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Borderless Cutscenes",
        Description = "Removes the letterboxing in cutscenes when using ultrawide displays.",
        Type = ModificationType.GameBehavior,
        Authors = ["goat", "Maple", "MidoriKami"],
        CompatibilityModule = new PluginCompatibilityModule("Dalamud.FullscreenCutscenes"),
    };

    [Signature("01 0F 84 ?? ?? ?? ?? 48 8B 0D ?? ?? ?? ?? 0F 57 D2 F3 0F 10 1D ?? ?? ?? ?? 0F 57 C9 48 89 B4 24")]
    private nint? memoryAddress;

    private MemoryReplacement? jumpPatch;

    public override void OnEnableAsync() {
        Services.GameInteropProvider.InitializeFromAttributes(this);

        if (memoryAddress is { } address && memoryAddress != nint.Zero) {
            jumpPatch = new MemoryReplacement(address, [0x00]);
            jumpPatch.Enable();
        }
    }

    public override void OnDisableAsync() {
        jumpPatch?.Dispose();
        jumpPatch = null;

        memoryAddress = null;
    }
}
