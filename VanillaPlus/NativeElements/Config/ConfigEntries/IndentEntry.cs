using System;
using KamiToolKit.System;

namespace VanillaPlus.NativeElements.Config.ConfigEntries;

public class IndentEntry : IConfigEntry {
    public NodeBase BuildNode()
        => throw new InvalidOperationException();

    public void Dispose() { }
}
