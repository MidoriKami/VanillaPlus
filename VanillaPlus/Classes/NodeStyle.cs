using System;
using System.Numerics;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using KamiToolKit.BaseTypes;

namespace VanillaPlus.Classes;

public class NodeStyle {
    public Vector2 Position { get; set; }

    [JsonIgnore] public Action<NodeStyle>? StyleChanged { get; set; }

    public async Task Save(string filePath)
        => await Utilities.Config.SaveConfig(this, filePath);
}

public class NodeStyle<T> : NodeStyle where T : NodeBase {
    public void ApplyStyle(params T?[]? nodes) {
        if (nodes is null) return;

        foreach (var node in nodes) {
            ApplyStyle(node);
        }
    }

    public virtual void ApplyStyle(T? node) {
        node?.Position = Position;
    }
}
