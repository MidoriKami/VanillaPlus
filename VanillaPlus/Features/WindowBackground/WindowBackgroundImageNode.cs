using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Classes.Controllers.Overlay;
using KamiToolKit.Nodes;

namespace VanillaPlus.Features.WindowBackground;

public unsafe class WindowBackgroundImageNode : OverlayNode {

    public override OverlayLayer OverlayLayer => OverlayLayer.BackLayer;
    
    public required WindowBackgroundSetting Settings { get; init; }
    
    private readonly BackgroundImageNode backgroundImageNode;

    public WindowBackgroundImageNode() {
        backgroundImageNode = new BackgroundImageNode();
        backgroundImageNode.AttachNode(this);
    }
    
    public override void Update() {
        var addon = Services.GameGui.GetAddonByName<AtkUnitBase>(Settings.AddonName);
        backgroundImageNode.IsVisible = addon is not null && addon->IsActuallyVisible();

        if (addon is not null) {
            backgroundImageNode.Color = Settings.Color;
            backgroundImageNode.Position = addon->Position() - Settings.Padding / 2.0f;
            backgroundImageNode.Size = (addon->RootSize() + Settings.Padding) * addon->Scale;
        }
    }
}
