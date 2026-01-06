using System.Numerics;

namespace VanillaPlus.Features.QuickPanelTweaks;

public class QuickPanelTweaksState {
    public bool hideFocusBorder = false;
    public bool hideHighlighting = false;
    public bool hidePanelBackground = false;
    public bool hideEmptySlots = false;

    public Vector2 closeButtonPosition = new(232f, 6f);
    public Vector2 settingsButtonPosition = new(258f, 10f);
    public Vector3 backgroundColor = new(0f, 0f, 0f);
    public Vector2 backgroundPosition = new(0f, 0f);
    public Vector2 backgroundSize = new(292f, 320f);

    public void updateFromConfig(QuickPanelTweaksConfig config) {
        hideFocusBorder = config.HideFocusBorder;
        hideHighlighting = config.HideHighlighting;
        hidePanelBackground = config.HidePanelBackground;
        hideEmptySlots = config.HideEmptySlots;
        backgroundColor = config.BackgroundColor.AsVector3();

        closeButtonPosition = config.MoveButtons ? new(234f, 37f) : new(258f, 10f);
        settingsButtonPosition = config.MoveButtons ? new(206f, 32f) : new(232f, 6f);
        backgroundPosition = config.MoveButtons ? new(20f, 20f) : new(0f, 0f);
        backgroundSize = config.MoveButtons ? new(252f, 286f) : new(292f, 320f);
    }
}
