using System.Numerics;
using System.Threading.Tasks;
using KamiToolKit.Overlay.UiOverlay;
using VanillaPlus.Features.DutyLootPreview.Data;
using VanillaPlus.Features.DutyLootPreview.Nodes;
using Action = System.Action;

namespace VanillaPlus.Features.DutyLootPreview;

/// <summary>
/// Displays the "Open Duty Loot" button near the active duty info
/// </summary>
public class DutyLootInDutyUiController {
    private OverlayController? overlayController;

    public required DutyLootDataLoader DataLoader;

    public Action? OnButtonClicked { get; init; }

    public async Task EnableAsync() {
        overlayController = new OverlayController();

        await Services.Framework.Run(() => {
            overlayController.Initialize();

            overlayController.AddNode(new DutyLootInDutyButtonNode(DataLoader) {
                OnClick = () => OnButtonClicked?.Invoke(),
                Size = new Vector2(20.0f, 20.0f),
            });
        });
    }

    public async Task DisableAsync() {
        if (overlayController is not null) {
            await overlayController.DisableAsync();
            overlayController = null;
        }
    }
}
