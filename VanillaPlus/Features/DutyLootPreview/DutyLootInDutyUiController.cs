using System;
using System.Numerics;
using KamiToolKit.Overlay.UiOverlay;
using VanillaPlus.Features.DutyLootPreview.Data;
using VanillaPlus.Features.DutyLootPreview.Nodes;
using Action = System.Action;

namespace VanillaPlus.Features.DutyLootPreview;

/// <summary>
/// Displays the "Open Duty Loot" button near the active duty info
/// </summary>
public class DutyLootInDutyUiController : IDisposable {
    private OverlayController? overlayController;

    public required DutyLootDataLoader DataLoader;

    public Action? OnButtonClicked { get; init; }

    public void Enable() {
        overlayController = new OverlayController();

        overlayController.AddNode(new DutyLootInDutyButtonNode(DataLoader) {
            OnClick = () => OnButtonClicked?.Invoke(),
            Size = new Vector2(20.0f, 20.0f),
        });
    }

    public void Dispose() {
        overlayController?.Dispose();
        overlayController = null;
    }
}
