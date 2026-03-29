using KamiToolKit;
using KamiToolKit.Nodes;

namespace VanillaPlus.NativeElements.Config.ConfigEntries;

public class LabelEntry : IConfigEntry {
    public string? Tooltip { get; set; }
    public required string Text { get; set; }
    
    public void Dispose() {
    }

    public NodeBase BuildNode() => new TextNode {
        String = Text,
    };
}
