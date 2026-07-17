using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;

namespace VanillaPlus.Features.AprilFools;

/// <summary>
/// Flips the loading screen text labels upside down.
/// </summary>
public class FlippingOutFools : FoolsModule {
    private AddonController? locationTitleController;

    public override bool IsEnabledByConfig
        => Config.FlippingOut;

    protected override async Task OnEnable() {
        unsafe {
            locationTitleController = new AddonController {
                AddonName = "_LocationTitle",
                OnSetup = LocationTitleSetup,
                OnFinalize = LocationTitleFinalize,
            };
        }

        await IFramework.Get().RunSafely(locationTitleController.Enable);
    }

    protected override async Task OnDisable() {
        await IFramework.Get().RunSafely(() => locationTitleController?.Dispose());
        locationTitleController = null;
    }

    private static unsafe void LocationTitleFinalize(AtkUnitBase* addon) {
        foreach (var node in addon->UldManager.Nodes) {
            if (node.Value is null) continue;
            if (node.Value->GetNodeType() is NodeType.Image) {
                node.Value->ScaleY = 1;
                node.Value->Origin = Vector2.Zero;
            }
        }
    }

    private static unsafe void LocationTitleSetup(AtkUnitBase* addon) {
        foreach (var node in addon->UldManager.Nodes) {
            if (node.Value is null) continue;
            if (node.Value->GetNodeType() is NodeType.Image) {
                node.Value->ScaleY = -1;
                node.Value->Origin = node.Value->Size / 2.0f;
            }
        }
    }
}
