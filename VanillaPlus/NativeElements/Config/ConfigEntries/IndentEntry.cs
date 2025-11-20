using System;
using KamiToolKit.NodeBaseClasses;

namespace VanillaPlus.NativeElements.Config.ConfigEntries;

public class IndentEntry : IConfigEntry {
    public NodeBase BuildNode()
        => throw new InvalidOperationException();

    public void Dispose() { }
}
