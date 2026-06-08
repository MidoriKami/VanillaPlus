using System.Numerics;
using KamiToolKit.Interfaces;
using KamiToolKit.Nodes;

namespace VanillaPlus.Features.WindowBackground.Nodes;

/// <summary>
/// Represents a <see cref="WindowBackgroundSetting"/>'s data for display in a <see cref="ListNode{T,TU}"/>.
/// </summary>
public class WindowBackgroundSettingListItemNode : ListItemWithFocusNav<WindowBackgroundSetting>, IListItemNode {

    /// <inheritdoc/>
    public static float ItemHeight => 24.0f;

    /// <summary>
    /// Gets the <see cref="TextNode"/> used to display the addons name.
    /// </summary>
    protected TextNode AddonNameTextNode { get; }

    /// <inheritdoc/>
    protected override void SetNodeData(WindowBackgroundSetting itemData) {
        AddonNameTextNode.String = itemData.AddonName;
    }

    public WindowBackgroundSettingListItemNode() {
        AddonNameTextNode = new TextNode();
        AddonNameTextNode.AttachNode(this);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        AddonNameTextNode.Size = new Vector2(Width - 4.0f, Height);
        AddonNameTextNode.Position = new Vector2(4.0f, 0.0f);
    }

}
