using System.Numerics;
using KamiToolKit.Addon;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.DebugCustomAddon;

#if DEBUG
/// <summary>
/// Debug Game Modification with a Custom Addon for use with playing around with ideas, DO NOT commit changes to this file
/// </summary>
public class DebugCustomAddon : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Debug CustomAddon",
        Description = "A module for playing around and testing VanillaPlus features",
        Type = ModificationType.Debug,
        Authors = [ "YourNameHere" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    private NativeAddon? debugAddon;

    public override void OnEnable() {
        debugAddon = new DebugAddon {
            NativeController = System.NativeController,
            InternalName = "DebugAddon",
            Title = "Debug Addon Window",
            Size = new Vector2(500.0f, 500.0f),
        };

        debugAddon.Open();

        OpenConfigAction = debugAddon.Toggle;
    }

    public override void OnDisable() {
        debugAddon?.Dispose();
        debugAddon = null;
    }
}
#endif
