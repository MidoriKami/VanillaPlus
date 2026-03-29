using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;

namespace VanillaPlus.Features.AprilFools;

/// <summary>
/// Flips the loading screen text labels upside down.
/// </summary>
public unsafe class FlippingOutFools : FoolsModule {
    private AddonController? locationTitleController;
    
    protected override void OnEnable() {
        locationTitleController = new AddonController("_LocationTitle");

        locationTitleController.OnAttach += addon => {
            if (!Config.FlippingOut) return;

            foreach (var node in addon->UldManager.Nodes) {
                if (node.Value is null) continue;
                if (node.Value->GetNodeType() is NodeType.Image) {
                    node.Value->ScaleY = -1;
                }
            }
        };

        locationTitleController.OnDetach += addon => {
            
            foreach (var node in addon->UldManager.Nodes) {
                if (node.Value is null) continue;
                if (node.Value->GetNodeType() is NodeType.Image) {
                    node.Value->ScaleY = 1;
                }
            }
        };
        
        locationTitleController.Enable();
    }

    protected override void OnDisable() {
        locationTitleController?.Dispose();
        locationTitleController = null;
    }
}
