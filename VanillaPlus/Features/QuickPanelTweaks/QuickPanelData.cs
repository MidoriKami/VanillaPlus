using System.Numerics;

namespace VanillaPlus.Features.QuickPanelTweaks;

public class QuickPanelData {
    public bool hideFocusBorder;
    public bool hideHighlighting;
    public bool hidePanelBackground;
    public bool hideEmptySlots;

    public Vector2 closeButtonPosition;
    public Vector2 settingsButtonPosition;
    public Vector4 backgroundColor;
    public Vector2 backgroundPosition;
    public Vector2 backgroundSize;

    public QuickPanelData() {
        reset();
    }

    public void updateFromConfig(QuickPanelTweaksConfig config) {
        hideFocusBorder = config.HideFocusBorder;
        hideHighlighting = config.HideHighlighting;
        hidePanelBackground = config.HidePanelBackground;
        hideEmptySlots = config.HideEmptySlots;
        backgroundColor = config.BackgroundColor;

        closeButtonPosition = config.MoveButtons ? new(234f, 37f) : new(258f, 10f);
        settingsButtonPosition = config.MoveButtons ? new(206f, 32f) : new(232f, 6f);
        backgroundPosition = config.MoveButtons ? new(20f, 20f) : new(0f, 0f);
        backgroundSize = config.MoveButtons ? new(252f, 286f) : new(292f, 320f);
    }

    public void reset() {
        hideFocusBorder = false;
        hideHighlighting = false;
        hidePanelBackground = false;
        hideEmptySlots = false;
        backgroundColor = new(0f, 0f, 0f, 1f);
        
        closeButtonPosition = new(258f, 10f);
        settingsButtonPosition = new(232f, 6f);
        backgroundPosition = new(0f, 0f);
        backgroundSize = new(292f, 320f);
    }
}
