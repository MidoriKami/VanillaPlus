using System.Numerics;
using KamiToolKit.Overlay;
using VanillaPlus.Features.DutyLootPreview.Nodes;
using Action = System.Action;

namespace VanillaPlus.Features.DutyLootPreview;

/// <summary>
/// Displays the "Open Duty Loot" button near the active duty info
/// </summary>
public class DutyLootInDutyUiController {
    private OverlayController? overlayController;

    public Action? OnButtonClicked { get; init; }

    public void OnEnable() {
        overlayController = new OverlayController();

        overlayController?.CreateNode(() => {
            return new DutyLootInDutyButtonNode {
                OnClick = () => OnButtonClicked?.Invoke(),
                Size = new Vector2(20.0f, 20.0f),
            };
        });
    }

    public void OnDisable() {
        overlayController?.Dispose();
        overlayController = null;
    }
}
