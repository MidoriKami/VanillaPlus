using System;
using KamiToolKit.BaseTypes;

namespace VanillaPlus.Classes;

public interface IConfigEntry : IDisposable {
    NodeBase BuildNode();

    string? Tooltip { get; set; }
}
