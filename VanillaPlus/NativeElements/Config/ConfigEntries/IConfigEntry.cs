using System;
using KamiToolKit.System;

namespace VanillaPlus.NativeElements.Config.ConfigEntries;

public interface IConfigEntry : IDisposable {
    NodeBase BuildNode();
}
