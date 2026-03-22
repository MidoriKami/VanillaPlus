using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace VanillaPlus.Features.AprilFools;

public unsafe class BeegWindowFools : IFoolsModule {
    public required AprilFoolsConfig Config { get; set; }

    private AtkUnitBase* focusedAddon = null;
    
    public void Enable()
        => Services.Framework.Update += OnFrameworkUpdate;

    public void Disable()
        => Services.Framework.Update -= OnFrameworkUpdate;

    private void OnFrameworkUpdate(IFramework framework) {
        if (!Config.BeegWindow) return;
        
        var newCollisionNode = AtkStage.Instance()->AtkCollisionManager->IntersectingAddon;

        if (focusedAddon != newCollisionNode) {
            RestoreNode(focusedAddon);
            ReverseNode(newCollisionNode);
            focusedAddon = newCollisionNode;
        }
    }

    private static void ReverseNode(AtkUnitBase* addon) {
        if (addon is null) return;

        addon->SetScale(addon->Scale * 1.5f, false);
    }

    private static void RestoreNode(AtkUnitBase* addon) {
        if (addon is null) return;
        
        addon->SetScale(addon->Scale / 1.5f, false);
    }
}
