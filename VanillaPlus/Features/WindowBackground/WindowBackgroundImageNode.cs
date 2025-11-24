using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Classes.Controllers;
using KamiToolKit.Nodes;

namespace VanillaPlus.Features.WindowBackground;

public unsafe class WindowBackgroundImageNode : OverlayNode {

    public override OverlayLayer OverlayLayer => OverlayLayer.Background;
    
    public required WindowBackgroundSetting Settings { get; init; }

    public bool IsOverlayNode;
    
    private readonly BackgroundImageNode backgroundImageNode;

    public WindowBackgroundImageNode() {
        backgroundImageNode = new BackgroundImageNode {
            IsVisible = false,
        };
        backgroundImageNode.AttachNode(this);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        backgroundImageNode.Size = Size;
    }

    public override void Update() {
        var addon = Services.GameGui.GetAddonByName<AtkUnitBase>(Settings.AddonName);
        backgroundImageNode.IsVisible = addon is not null && addon->IsActuallyVisible();

        if (addon is not null) {
            backgroundImageNode.Color = Settings.Color;
            Size = (addon->RootSize() + Settings.Padding) * addon->Scale;

            if (IsOverlayNode) {
                Position = addon->Position() - Settings.Padding / 2.0f;
            }
        }
    }
}
