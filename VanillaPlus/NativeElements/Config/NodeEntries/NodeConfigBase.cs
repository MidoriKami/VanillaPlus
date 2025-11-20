using System;
using KamiToolKit.NodeBaseClasses;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;
using VanillaPlus.NativeElements.Config.ConfigEntries;

namespace VanillaPlus.NativeElements.Config.NodeEntries;

public abstract class NodeConfigBase<T> : IConfigEntry where T : NodeBase, new() {
    protected T? StyleObject;

    public required string FilePath { get; init; }
    public required NodeConfigEnum ConfigOptions { get; init; }
    public required ISavable Config { get; init; }

    public NodeBase BuildNode() {
        StyleObject = new T();
        StyleObject.Load(FilePath);

        var layoutNode = new VerticalListNode {
            Height = 24.0f,
            FitContents = true,
            FitWidth = true,
        };

        foreach (var option in Enum.GetValues<NodeConfigEnum>()) {
            if (!ConfigOptions.HasFlag(option)) continue;

            var newOptionNode = BuildOption(option);
            if (newOptionNode is null) continue;

            layoutNode.AddNode(newOptionNode);
        }

        return layoutNode;
    }

    protected abstract SimpleComponentNode? BuildOption(NodeConfigEnum configOption);

    protected void SaveStyleObject() {
        if (StyleObject is null) return;

        StyleObject.Save(FilePath);

        // Little janky, this is only called to trigger IConfig.OnSave in the callers module code, so it can update any live objects.
        Config.Save();
    }

    public virtual void Dispose() {
        StyleObject?.Dispose();
        StyleObject = null;
    }
}
