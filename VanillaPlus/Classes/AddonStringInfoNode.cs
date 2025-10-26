using KamiToolKit.Addons.Interfaces;
using KamiToolKit.Addons.Parts;

namespace VanillaPlus.Classes;

public class AddonStringInfoNode : StringInfoNode {
    public override string GetSubLabel()
        => IsVisible() ? "Visible" : "Hidden";

    public override uint? GetId()
        => Services.GameGui.GetAddonByName(Label).Id;

    public override uint? GetIconId()
        => IsVisible() ? (uint) 60071 : 60072;

    public override string? GetTexturePath()
        => null;

    public override int Compare(IInfoNodeData other, string sortingMode) {
        switch (sortingMode) {
            case "Alphabetical":
                return string.CompareOrdinal(Label, (other as AddonStringInfoNode)?.Label);

            case "Visibility":
                var visibilityComparison = (other as AddonStringInfoNode)?.IsVisible().CompareTo(IsVisible()) ?? 0;
                if (visibilityComparison is 0) {
                    visibilityComparison = string.CompareOrdinal(Label, (other as AddonStringInfoNode)?.Label);
                }

                return visibilityComparison;

            default:
                return base.Compare(other, sortingMode);
        }
    }

    private bool IsVisible() => Services.GameGui.GetAddonByName(Label).IsVisible;
}
