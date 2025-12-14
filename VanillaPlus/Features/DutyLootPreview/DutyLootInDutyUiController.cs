using KamiToolKit.Overlay;
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
            };
        });
    }

    public void OnDisable() {
        overlayController?.Dispose();
        overlayController = null;
    }
}
