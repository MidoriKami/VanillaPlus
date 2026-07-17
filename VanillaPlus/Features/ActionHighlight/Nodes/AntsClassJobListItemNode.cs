using System.Numerics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Interfaces;
using KamiToolKit.Nodes;
using VanillaPlus.Features.ActionHighlight.Config;

namespace VanillaPlus.Features.ActionHighlight.Nodes;

public class AntsClassJobListItemNode : ListItemWithFocusNav<AntsClassJobConfig>, IListItemNode {

    /// <inheritdoc/>
    public static float ItemHeight => 48.0f;

    /// <inheritdoc/>
    protected override void SetNodeData(AntsClassJobConfig itemData) {
        classJobIconNode.IconId = 62000 + itemData.ClassJobId;
        classJobNameTextNode.String = ISeStringEvaluator.Get().EvaluateFromAddon(698, [itemData.ClassJobId]);
    }

    public AntsClassJobListItemNode() {
        classJobIconNode = new IconImageNode {
            FitTexture = true,
        };
        classJobIconNode.AttachNode(this);

        classJobNameTextNode = new TextNode {
            TextFlags = TextFlags.Ellipsis | TextFlags.Emboss,
        };
        classJobNameTextNode.AttachNode(this);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        classJobIconNode.Size = new Vector2(Height - 4.0f, Height - 4.0f);
        classJobIconNode.Position = new Vector2(2.0f, 2.0f);

        classJobNameTextNode.Size = new Vector2(Width - classJobIconNode.Width - 4.0f, Height);
        classJobNameTextNode.Position = new Vector2(classJobIconNode.Bounds.Right + 2.0f, 0.0f);
    }

    private readonly IconImageNode classJobIconNode;
    private readonly TextNode classJobNameTextNode;
}
