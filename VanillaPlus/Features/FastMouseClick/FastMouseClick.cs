using Dalamud.Utility.Signatures;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.FastMouseClick;

public class FastMouseClick : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Fast Mouse Click",
        Description = "The game does not fire UI events for single mouse clicks whenever a double click is detected.\n\n" +
                      "This game modification fixes it by always triggering the normal mouse click in addition to the double click.",
        Type = ModificationType.GameBehavior,
        Authors = ["Haselnussbomber"],
        ChangeLog = [
            new ChangeLogInfo(1, "InitialVersion"),
        ],
        CompatabilityModule = new HaselTweaksCompatabilityModule("FastMouseClickFix"),
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
        
        memoryAddress = nint.Zero;
    }
}
