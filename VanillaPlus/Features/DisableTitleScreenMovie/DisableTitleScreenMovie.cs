using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.DisableTitleScreenMovie;

public class DisableTitleScreenMovie : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_DisableTitleScreenMovie,
        Description = Strings.ModificationDescription_DisableTitleScreenMovie,
        Type = ModificationType.GameBehavior,
        Authors = ["MidoriKami"],
        CompatibilityModule = new SimpleTweaksCompatibilityModule("DisableTitleScreenMovie"),
    };

    [Signature("0F 85 ?? ?? ?? ?? 8B 8E ?? ?? ?? ?? 8D 41 ?? 83 F8 ?? 0F 86 ?? ?? ?? ?? 8D 41")]
    private nint? memoryAddress;

    private MemoryReplacement? jumpPatch;

    public override async Task OnEnableAsync() {
        IGameInteropProvider.Get().InitializeFromAttributes(this);

        // Convert the jnz instruction to a unconditional jmp instruction,
        // but keeps the same address and replace the last byte with nop
        if (memoryAddress is { } address && memoryAddress != nint.Zero) {

            unsafe {
                var originalJumpOffset = (byte*) address + 2;

                jumpPatch = new MemoryReplacement(address, [
                    0xE9,
                    (byte) (originalJumpOffset[0] + 1), // Increase by 1 because we moved the instruction up by one.
                    originalJumpOffset[1],
                    originalJumpOffset[2],
                    originalJumpOffset[3],
                    0x90,
                ]);
            }

            await IFramework.Get().RunSafely(jumpPatch.Enable);
        }
    }

    public override async Task OnDisableAsync() {
        await Task.WhenAll(jumpPatch?.DisposeAsync().AsTask() ?? Task.CompletedTask);
        jumpPatch = null;

        memoryAddress = null;
    }
}
