using System.Reflection;
using KamiToolKit;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;

namespace VanillaPlus.NativeElements.Config.ConfigEntries;

public abstract class BaseConfigEntry : IConfigEntry {
    public required string Label { get; init; }
    public required MemberInfo MemberInfo { get; init; }
    public required ISavable Config { get; init; }

    public abstract NodeBase BuildNode();

    public virtual void Dispose() { }

    protected TextNode GetLabelNode() => new CategoryTextNode {
        String = Label,
    };
}
