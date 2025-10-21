using System.Numerics;
using System.Reflection;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using KamiToolKit.System;
using VanillaPlus.Classes;

namespace VanillaPlus.NativeElements.Config.ConfigEntries;

public abstract class BaseConfigEntry : IConfigEntry {
    public required string Label { get; init; }
    public required MemberInfo MemberInfo { get; init; }
    public required ISavable Config { get; init; }

    public abstract NodeBase BuildNode();

    public virtual void Dispose() { }
    
    protected TextNode GetLabelNode() => new() {
        IsVisible = true,
        Size = new Vector2(100.0f, 24.0f),
        AlignmentType = AlignmentType.Left,
        FontType = FontType.Axis,
        FontSize = 14,
        LineSpacing = 14,
        TextColor = ColorHelper.GetColor(8),
        TextOutlineColor = ColorHelper.GetColor(7),
        TextFlags = TextFlags.Edge | TextFlags.AutoAdjustNodeSize,
        String = Label,
    };
}
