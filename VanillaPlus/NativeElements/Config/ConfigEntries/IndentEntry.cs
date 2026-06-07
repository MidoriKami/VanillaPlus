using System;
using KamiToolKit.BaseTypes;

namespace VanillaPlus.NativeElements.Config.ConfigEntries;

public class IndentEntry : IConfigEntry {
    public string? Tooltip { get; set; }

    public NodeBase BuildNode()
        => throw new InvalidOperationException();

    public void Dispose() { }
}
