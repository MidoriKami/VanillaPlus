using KamiToolKit.Addons.Parts;

namespace VanillaPlus.Classes;

public class AddonStringInfoNode : StringInfoNode {
    public override string GetSubLabel()
        => Services.GameGui.GetAddonByName(Label).IsVisible ? "Visible" : "Hidden";

    public override uint? GetId()
        => Services.GameGui.GetAddonByName(Label).Id;

    public override uint? GetIconId()
        => null;

    public override string? GetTexturePath()
        => null;
}
