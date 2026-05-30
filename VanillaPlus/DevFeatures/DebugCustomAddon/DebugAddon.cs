using System;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;

namespace VanillaPlus.DevFeatures.DebugCustomAddon;

#if DEBUG
/// <summary>
/// Debug Addon Window for use with playing around with ideas, DO NOT commit changes to this file
/// </summary>
public class DebugAddon : NativeAddon {

    protected override unsafe void OnSetup(AtkUnitBase* addon, Span<AtkValue> atkValueSpan) {

    }
}
#endif
