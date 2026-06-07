using System;
using KamiToolKit.BaseTypes;

namespace VanillaPlus.NativeElements.Config.ConfigEntries;

public interface IConfigEntry : IDisposable {
    NodeBase BuildNode();

    string? Tooltip { get; set; }
}
