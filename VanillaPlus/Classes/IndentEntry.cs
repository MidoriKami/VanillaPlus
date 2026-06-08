using System;
using KamiToolKit.BaseTypes;

namespace VanillaPlus.Classes;

public class IndentEntry : IConfigEntry {
    public string? Tooltip { get; set; }

    public NodeBase BuildNode()
        => throw new InvalidOperationException();

    public void Dispose() { }
}
