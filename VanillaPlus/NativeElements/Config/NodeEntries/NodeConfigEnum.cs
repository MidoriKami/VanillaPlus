using System;

namespace VanillaPlus.NativeElements.Config.NodeEntries;

[Flags]
public enum NodeConfigEnum {
    TextColor = 1,
    TextOutlineColor = 1 << 1,
    Position = 1 << 2,
    TextSize = 1 << 3,
    TextFont = 1 << 4,
    TextAlignment = 1 << 5,
}
