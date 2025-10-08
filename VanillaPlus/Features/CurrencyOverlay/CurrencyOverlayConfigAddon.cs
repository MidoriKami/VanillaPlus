using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Addon;
using KamiToolKit.Nodes;

namespace VanillaPlus.Features.CurrencyOverlay;

public class CurrencyOverlayConfigAddon : NativeAddon {
    public required Action<TextButtonNode?> OnEnableMoving { get; init; }
    public required Action OnEditEntriesClicked { get; init; }

    private TextButtonNode? enableMovingButton;
    private TextButtonNode? editEntriesButton;
    
    protected override unsafe void OnSetup(AtkUnitBase* addon) {
        enableMovingButton = new TextButtonNode {
            Size = new Vector2(125.0f, 24.0f),
            Position = ContentStartPosition + new Vector2(0.0f, 10.0f),
            String = "Toggle Moving",
            IsVisible = true,
            OnClick = () => OnEnableMoving(enableMovingButton),
            TooltipString = "Toggles currencies ability to be moved via click-drag",
        };
        System.NativeController.AttachNode(enableMovingButton, this);

        editEntriesButton = new TextButtonNode {
            Size = new Vector2(125.0f, 24.0f),
            Position = ContentStartPosition + new Vector2(ContentSize.X - 125.0f, 10.0f),
            String = "Edit Entries",
            IsVisible = true,
            OnClick = OnEditEntriesClicked,
            TooltipString = "Open Add/Remove Window",
        };
        System.NativeController.AttachNode(editEntriesButton, this);
    }
}
