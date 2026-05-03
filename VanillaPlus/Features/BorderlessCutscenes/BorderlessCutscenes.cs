using Dalamud.Utility.Signatures;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.BorderlessCutscenes;

public class BorderlessCutscenes : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Borderless Cutscenes",
        Description = "Removes the letterboxing in cutscenes when using ultrawide displays.",
        Type = ModificationType.GameBehavior,
        Authors = [ "goat", "Maple", "MidoriKami" ],
        CompatibilityModule = new PluginCompatibilityModule("Dalamud.FullscreenCutscenes"),
    };

    [Signature("01 0F 84 83 01 00 00 48 8B 0D A7 A1 6A 02")]
    private nint? memoryAddress;

    private MemoryReplacement? jumpPatch;

    public override void OnEnable() {
        Services.GameInteropProvider.InitializeFromAttributes(this);
        
        if (memoryAddress is { } address && memoryAddress != nint.Zero) {
            jumpPatch = new MemoryReplacement(address, [0x00]);
            jumpPatch.Enable();
        }
    }

    public override void OnDisable() {
        jumpPatch?.Dispose();
        jumpPatch = null;
        
        memoryAddress = null;
    }
}
