using System.Numerics;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.QuickPanelTweaks;

public class QuickPanelTweaksConfig : GameModificationConfig<QuickPanelTweaksConfig> {
    protected override string FileName => "QuickPanelTweaks";

    public bool HideFocusBorder = true;
    public bool HideHighlighting = true;
    public bool HidePanelBackground = false;
    public bool HideEmptySlots = false;
    public bool MoveButtons = false;
    public Vector4 BackgroundColor = new Vector4(0f, 0f, 0f, 1f);
}
