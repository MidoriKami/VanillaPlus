using KamiToolKit.Addons.Parts;

namespace VanillaPlus.Classes;

public class AddonStringInfoNode : StringInfoNode {
    public override string GetSubLabel()
        => Services.GameGui.GetAddonByName(Label).IsVisible ? "Visible" : "Hidden";

    public override uint? GetId()
        => Services.GameGui.GetAddonByName(Label).Id;

    public override uint? GetIconId()
        => Services.GameGui.GetAddonByName(Label).IsVisible ? (uint) 60071 : 60072;

    public override string? GetTexturePath()
        => null;
}
