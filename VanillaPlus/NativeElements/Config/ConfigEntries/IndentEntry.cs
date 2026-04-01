using System;
using KamiToolKit;

namespace VanillaPlus.NativeElements.Config.ConfigEntries;

public class IndentEntry : IConfigEntry {
    public string? Tooltip { get; set; }
    
    public NodeBase BuildNode()
        => throw new InvalidOperationException();

    public void Dispose() { }
}
