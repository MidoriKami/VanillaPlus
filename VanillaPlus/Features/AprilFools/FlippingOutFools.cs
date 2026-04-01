using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;

namespace VanillaPlus.Features.AprilFools;

/// <summary>
/// Flips the loading screen text labels upside down.
/// </summary>
public unsafe class FlippingOutFools : FoolsModule {
    private AddonController? locationTitleController;

    public override bool IsEnabledByConfig 
        => Config.FlippingOut;
    
    protected override void OnEnable() {
        locationTitleController = new AddonController {
            AddonName = "_LocationTitle",
            OnSetup = addon => {
                foreach (var node in addon->UldManager.Nodes) {
                    if (node.Value is null) continue;
                    if (node.Value->GetNodeType() is NodeType.Image) {
                        node.Value->ScaleY = -1;
                        node.Value->Origin = node.Value->Size / 2.0f;
                    }
                }
            },
            OnFinalize = addon => {
                foreach (var node in addon->UldManager.Nodes) {
                    if (node.Value is null) continue;
                    if (node.Value->GetNodeType() is NodeType.Image) {
                        node.Value->ScaleY = 1;
                        node.Value->Origin = Vector2.Zero;
                    }
                }
            },
        };
        locationTitleController.Enable();
    }

    protected override void OnDisable() {
        locationTitleController?.Dispose();
        locationTitleController = null;
    }
}
