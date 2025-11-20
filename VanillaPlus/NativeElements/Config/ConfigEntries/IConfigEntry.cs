using System;
using KamiToolKit;

namespace VanillaPlus.NativeElements.Config.ConfigEntries;

public interface IConfigEntry : IDisposable {
    NodeBase BuildNode();
}
