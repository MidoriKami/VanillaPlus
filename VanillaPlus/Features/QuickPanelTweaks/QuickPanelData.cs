namespace VanillaPlus.Features.QuickPanelTweaks;

public class QuickPanelData {
    public const uint WindowComponentNodeId = 45;
    public const uint FocusBorderNodeId = 8;
    public const uint HighlightNodeId = 10;
    public const uint BackgroundNodeId = 9;
    public const uint CloseButtonNodeId = 6;
    public const uint SettingsButtonNodeId = 2;
    public const uint PanelBackgroundNodeId = 44;
    public const uint CommandsStartNodeId = 19;
    public const uint CommandsEndNodeId = 43;

    public bool hideFocusBorder;
    public bool hideHighlighting;
    public bool hidePanelBackground;
    public bool hideEmptySlots;

    public (float x, float y) closeButtonPosition;
    public (float x, float y) settingsButtonPosition;
    public (short red, short green, short blue) backgroundColor;
    public (float x, float y) backgroundPosition;
    public (ushort width, ushort height) backgroundSize;

    public QuickPanelData() {
        reset();
    }

    public void updateFromConfig(QuickPanelTweaksConfig config) {
        hideFocusBorder = config.HideFocusBorder;
        hideHighlighting = config.HideHighlighting;
        hidePanelBackground = config.HidePanelBackground;

        closeButtonPosition = config.MoveButtons ? (234f, 37f) : (258f, 10f);
        settingsButtonPosition = config.MoveButtons ? (206f, 32f) : (232f, 6f);
        backgroundPosition = config.MoveButtons ? (20f, 20f) : (0f, 0f);
        backgroundSize = config.MoveButtons ? ((ushort)252, (ushort)286) : ((ushort)292, (ushort)320);

        backgroundColor = (
            (short)(config.BackgroundColor.X * 255 - 255),
            (short)(config.BackgroundColor.Y * 255 - 255),
            (short)(config.BackgroundColor.Z * 255 - 255)
        );
    }

    public void reset() {
        hideFocusBorder = false;
        hideHighlighting = false;
        hidePanelBackground = false;
        hideEmptySlots = false;
        closeButtonPosition = (258f, 10f);
        settingsButtonPosition = (232f, 6f);
        backgroundColor = (-255, -255, -255);
        backgroundPosition = (0f, 0f);
        backgroundSize = (292, 320);
    }
}
