using System;
using KamiToolKit.Nodes;
using KamiToolKit;

namespace VanillaPlus.NativeElements.Config.ConfigEntries;

public class ButtonConfig : IConfigEntry {
    public required string Label { get; init; }
    public required Action OnClick { get; init; }

    public NodeBase BuildNode() {
        return new TextButtonNode {
            String = Label,
            OnClick = OnClick,
            Height = 28.0f,
        };
    }

    public void Dispose() { }
}
