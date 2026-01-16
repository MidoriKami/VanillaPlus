using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;

namespace VanillaPlus.Features.HideUnwantedBanners;

public class BannerConfigListItemNode : ListItemNode<BannerConfig> {

    public override float ItemHeight => 96.0f;
    
    private readonly CheckboxNode checkboxNode;
    private readonly SimpleComponentNode imageContainerNode;
    private readonly IconImageNode iconImageNode;

    public BannerConfigListItemNode() {
        EnableHighlight = false;
        EnableSelection = false;
        
        checkboxNode = new CheckboxNode {
            OnClick = OnCheckboxClicked,
        };
        checkboxNode.AttachNode(this);

        imageContainerNode = new SimpleComponentNode();
        imageContainerNode.AttachNode(this);

        iconImageNode = new IconImageNode {
            FitTexture = true,
            ShowClickableCursor = true,
        };

        iconImageNode.AddEvent(AtkEventType.MouseClick, () => {
            checkboxNode.IsChecked = !checkboxNode.IsChecked;
        });
        iconImageNode.AttachNode(imageContainerNode);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        checkboxNode.Size = new Vector2(32.0f, 32.0f);
        checkboxNode.Position = new Vector2(Height, Height) / 2.0f - checkboxNode.Size / 2.0f;

        imageContainerNode.Size = new Vector2(Width - Height - 2.0f, Height);
        imageContainerNode.Position = new Vector2(Height + 2.0f, 0.0f);
    }

    private void RecalculateLayout() {
        var actualTextureSize = iconImageNode.ActualTextureSize;

        var widthRatio = actualTextureSize.Y / Height;

        var resultingWidth = actualTextureSize.X / widthRatio;
        var remainingArea = imageContainerNode.Width - resultingWidth;
        var remainingHalf = remainingArea / 2.0f;
        
        iconImageNode.Size = new Vector2(resultingWidth, Height);
        iconImageNode.Position = new Vector2(remainingHalf, 0.0f);
    }

    private bool textureResized;
    
    protected override void SetNodeData(BannerConfig itemData) {
        iconImageNode.IconId = (uint) itemData.BannerId;
        checkboxNode.IsChecked = itemData.IsSuppressed;

        textureResized = false;
    }
    
    private void OnCheckboxClicked(bool newValue) {
        if (IsSettingNodeData) return;
        
        ItemData?.IsSuppressed = newValue;
    }
    
    public override void Update() {
        base.Update();
    
        if (textureResized) return;
        if (iconImageNode.IsTextureReady) {
            RecalculateLayout();
            textureResized = true;
        }
    }
}
