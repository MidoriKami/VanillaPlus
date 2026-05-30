using System.Threading.Tasks;
using KamiToolKit;

namespace VanillaPlus.DevFeatures.DebugCustomAddon;

#if DEBUG
/// <summary>
/// Debug Addon Window for use with playing around with ideas, DO NOT commit changes to this file
/// </summary>
public class DebugAddon : NativeAddon {

    protected override Task BuildUiAsync() {

        return Task.CompletedTask;
    }
}
#endif
