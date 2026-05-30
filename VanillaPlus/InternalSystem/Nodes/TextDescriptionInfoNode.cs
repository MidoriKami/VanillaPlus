using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;

namespace VanillaPlus.InternalSystem.Nodes;

public class TextDescriptionInfoNode : ResNode {

    private readonly TextNode description;

    public TextDescriptionInfoNode() {
        description = new TextNode {
            AlignmentType = AlignmentType.Center,
            TextFlags = TextFlags.WordWrap | TextFlags.MultiLine,
            FontSize = 14,
            LineSpacing = 22,
            FontType = FontType.Axis,
        };
        description.AttachNode(this);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        description.Size = new Vector2(Width, Height * 2.0f / 3.0f) - new Vector2(32.0f, 32.0f);
        description.Position = new Vector2(16.0f, 16.0f);
    }

    public void LoadModificationInfo(LoadedModification modification) {
        description.String = modification.Modification.ModificationInfo.Description;
    }
}
