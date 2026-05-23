// ReSharper disable RedundantUnsafeContext

using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.DevFeatures.DebugHook;

#if DEBUG
/// <summary>
/// Debug Game Modification for use with playing around with ideas, DO NOT commit changes to this file
/// </summary>
public unsafe class DebugHook : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Debug Hook",
        Description = "Templated Helper for debugging hooks.",
        Type = ModificationType.Debug,
        Authors = ["YourNameHere"],
    };

    private delegate void HookDelegate();

    [Signature("AA BB CC DD EE FF", DetourName = nameof(HookDetour))]
    private Hook<HookDelegate>? hook;

    public override void OnEnableAsync() {
        Services.GameInteropProvider.InitializeFromAttributes(this);

        hook?.Enable();
    }

    public override void OnDisableAsync() {
        hook?.Dispose();
        hook = null;
    }

    private void HookDetour() {

        hook!.Original();

    }
}
#endif
