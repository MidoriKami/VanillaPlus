using System;
using KamiToolKit.BaseTypes;
using KamiToolKit.Nodes;

namespace VanillaPlus.Classes;

public class ButtonConfig : IConfigEntry {
    public required string Label { get; init; }
    public required Action OnClick { get; init; }
    public string? Tooltip { get; set; }

    public NodeBase BuildNode() {
        return new TextButtonNode {
            String = Label,
            OnClick = OnClick,
            Height = 28.0f,
        };
    }

    public void Dispose() { }
}
