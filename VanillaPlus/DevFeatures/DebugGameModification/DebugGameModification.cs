// ReSharper disable RedundantUnsafeContext

using System.Drawing;
using System.Threading.Tasks;
using Dalamud.Interface;
using Dalamud.Plugin.Services;
using KamiToolKit.Addons;
using KamiToolKit.BaseTypes;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.DevFeatures.DebugGameModification;

#if DEBUG
/// <summary>
/// Debug Game Modification for use with playing around with ideas, DO NOT commit changes to this file
/// </summary>
public class DebugGameModification : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Debug Game Modification",
        Description = "A module for playing around and testing VanillaPlus features",
        Type = ModificationType.Debug,
        Authors = ["YourNameHere"],
    };

    private NativeAddon? addon;

    public override async Task OnEnableAsync() {

        addon = new ColorPickerAddon {
            InternalName = "ColorPicker",
            Title = "Color Picker",
            DefaultColor = KnownColor.Blue.Vector(),
        };

        await IFramework.Get().Run(addon.Toggle);
    }

    public override async Task OnDisableAsync() {
        await Task.WhenAllDisposed(addon);
        addon = null;
    }
}
#endif
