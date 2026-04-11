using System.Numerics;
using KamiToolKit.Nodes;

namespace VanillaPlus.Features.LockChatButton;

public class PadlockButtonNode : TextureButtonNode {
    private readonly Vector2 lockedCoordinate = new(88.0f, 0.0f);
    private readonly Vector2 unlockedCoordinate = new(48.0f, 0.0f);

    public PadlockButtonNode() {
        TexturePath = "ui/uld/ActionBar.tex";
        TextureCoordinates = lockedCoordinate;
        TextureSize = new Vector2(20.0f, 24.0f);
        
        ImageNode.Scale = new Vector2(0.90f, 0.90f);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();
        
        ImageNode.Origin = ImageNode.Size / 2.0f;
    }

    public bool IsLocked {
        get;
        set {
            field = value;
            TextureCoordinates = value ? lockedCoordinate : unlockedCoordinate;
        }
    }

    protected override void ClickHandler() {
        IsLocked = !IsLocked;
        base.ClickHandler();
    }
}
