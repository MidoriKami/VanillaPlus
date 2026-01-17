using System.Numerics;
using KamiToolKit;
using KamiToolKit.Premade.Color;

namespace VanillaPlus.NativeElements.Config.ConfigEntries;

public class ColorConfig : BaseConfigEntry {
    public required Vector4 Color { get; set; }
    public Vector4? DefaultColor { get; init; }

    public override NodeBase BuildNode() => new ColorEditNode {
        Size = new Vector2(200.0f, 28.0f),
        String = Label,
        CurrentColor = Color,
        DefaultColor = DefaultColor,
        OnColorConfirmed = color => {
            Color = color;
            MemberInfo.SetValue(Config, color);
            Config.Save();
        },
    };
}
