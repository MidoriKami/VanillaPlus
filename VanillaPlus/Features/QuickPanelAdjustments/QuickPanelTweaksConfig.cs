using System.Numerics;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.QuickPanelAdjustments;

public class QuickPanelAdjustmentsConfig : GameModificationConfig<QuickPanelAdjustmentsConfig> {
    protected override string FileName => "QuickPanelAdjustments";

    public bool HideHighlighting = true;
    public bool HideFocusBorder = true;
    public bool HidePanelBackground = false;
    public bool HideEmptySlots = false;
    public bool MoveButtons = false;
    public Vector4 BackgroundColor = new(1.0f, 1.0f, 1.0f, 25.0f / 255.0f);
}
