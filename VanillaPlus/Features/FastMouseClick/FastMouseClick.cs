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
        ChangeLog = [
            new ChangeLogInfo(1, "InitialVersion"),
        ],
        CompatibilityModule = new HaselTweaksCompatibilityModule("FastMouseClickFix"),
    };

    [Signature("EB 3F B8 ?? ?? ?? ?? 48 8B D7")]
    private nint? memoryAddress;

    private MemoryReplacement? memoryPatch;

    public override void OnEnable() {
        Services.GameInteropProvider.InitializeFromAttributes(this);
        
        if (memoryAddress is { } address && memoryAddress != nint.Zero) {
            memoryPatch = new MemoryReplacement(address, [0x90, 0x90]);
            memoryPatch.Enable();
        }
    }

    public override void OnDisable() {
        memoryPatch?.Dispose();
        memoryPatch = null;

        memoryAddress = null;
    }
}
