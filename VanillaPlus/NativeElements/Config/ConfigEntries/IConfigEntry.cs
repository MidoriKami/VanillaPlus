using System;
using KamiToolKit.NodeBaseClasses;

namespace VanillaPlus.NativeElements.Config.ConfigEntries;

public interface IConfigEntry : IDisposable {
    NodeBase BuildNode();
}
