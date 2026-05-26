using System.Numerics;
using System.Threading.Tasks;
using KamiToolKit;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.DevFeatures.DebugCustomAddon;

#if DEBUG
/// <summary>
/// Debug Game Modification with a Custom Addon for use with playing around with ideas, DO NOT commit changes to this file
/// </summary>
public class DebugCustomAddon : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Debug Custom Addon",
        Description = "A module for playing around and testing VanillaPlus features",
        Type = ModificationType.Debug,
        Authors = ["YourNameHere"],
    };

    private NativeAddon? debugAddon;

    public override async Task OnEnableAsync() {
        debugAddon = new DebugAddon {
            InternalName = "DebugAddon",
            Title = Strings.DebugCustomAddon_Title,
            Size = new Vector2(500.0f, 500.0f),
        };

        OpenConfigAction = debugAddon.Toggle;

        await debugAddon.OpenAsync();
    }

    public override Task OnDisableAsync() {
        debugAddon?.Dispose();
        debugAddon = null;

        return Task.CompletedTask;
    }
}
#endif
