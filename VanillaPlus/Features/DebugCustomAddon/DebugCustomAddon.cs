using System.Numerics;
using KamiToolKit;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.DebugCustomAddon;

#if DEBUG
/// <summary>
/// Debug Game Modification with a Custom Addon for use with playing around with ideas, DO NOT commit changes to this file
/// </summary>
public class DebugCustomAddon : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings("ModificationDisplay_DebugCustomAddon"),
        Description = Strings("ModificationDescription_DebugCustomAddon"),
        Type = ModificationType.Debug,
        Authors = [ "YourNameHere" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    private NativeAddon? debugAddon;

    public override void OnEnable() {
        debugAddon = new DebugAddon {
            InternalName = "DebugAddon",
            Title = Strings("DebugCustomAddon_Title"),
            Size = new Vector2(500.0f, 500.0f),
        };

        debugAddon.DebugOpen();

        OpenConfigAction = debugAddon.Toggle;
    }

    public override void OnDisable() {
        debugAddon?.Dispose();
        debugAddon = null;
    }
}
#endif
