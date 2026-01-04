using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using KamiToolKit;
using KamiToolKit.Nodes;

namespace VanillaPlus.NativeElements.Config.ConfigEntries;

public class DropDownConfig : BaseConfigEntry {
    public required Dictionary<string, object> Options { get; init; }
    public required object InitialValue { get; set; }

    public override NodeBase BuildNode() {
        var layoutNode = new HorizontalListNode {
            Height = 28.0f,
            ItemSpacing = 10.0f,
        };

        var dropdown = new TextDropDownNode {
            Size = new Vector2(175.0f, 28.0f),
            Options = Options.Keys.ToList(),
            SelectedOption = Options.FirstOrDefault(x => x.Value.Equals(InitialValue)).Key ?? string.Empty,
            OnOptionSelected = newValue => {
                if (Options.TryGetValue(newValue, out var result)) {
                    InitialValue = result;
                    MemberInfo.SetValue(Config, result);
                    Config.Save();
                }
            },
            MaxListOptions = 20,
        };

        layoutNode.AddNode(dropdown);
        layoutNode.AddNode(GetLabelNode());

        return layoutNode;
    }
}
