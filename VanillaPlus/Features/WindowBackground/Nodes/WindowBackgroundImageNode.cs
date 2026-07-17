using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using KamiToolKit.UiOverlay;

namespace VanillaPlus.Features.WindowBackground.Nodes;

public unsafe class WindowBackgroundImageNode : OverlayNode {

    public override OverlayLayer OverlayLayer => OverlayLayer.Background;

    public required WindowBackgroundSetting Settings { get; init; }

    public bool IsOverlayNode;

    private readonly ColorImageNode colorImageNode;

    public WindowBackgroundImageNode() {
        colorImageNode = new ColorImageNode {
            IsVisible = false,
        };
        colorImageNode.AttachNode(this);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        colorImageNode.Size = Size;
    }

    protected override void OnUpdate() {
        var addon = Service<IGameGui>.Get().GetAddonByName<AtkUnitBase>(Settings.AddonName);
        colorImageNode.IsVisible = addon is not null && addon->IsActuallyVisible;

        if (addon is not null) {
            colorImageNode.Color = Settings.Color;
            Size = (addon->RootSize + Settings.Padding) * addon->Scale;

            if (IsOverlayNode) {
                Position = addon->Position - Settings.Padding / 2.0f;
            }
        }
    }
}
