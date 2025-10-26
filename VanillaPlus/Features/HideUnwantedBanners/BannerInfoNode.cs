using System;
using System.Numerics;
using Dalamud.Game.Addon.Events;
using KamiToolKit.Nodes;

namespace VanillaPlus.Features.HideUnwantedBanners;

public class BannerInfoNode : SimpleComponentNode {

    private CheckboxNode checkboxNode;
    private SimpleComponentNode imageContainerNode;
    private IconImageNode iconImageNode;

    public BannerInfoNode() {
        checkboxNode = new CheckboxNode {
            IsVisible = true,
        };
        System.NativeController.AttachNode(checkboxNode, this);

        imageContainerNode = new SimpleComponentNode {
            IsVisible = true,
        };
        System.NativeController.AttachNode(imageContainerNode, this);

        iconImageNode = new IconImageNode {
            IsVisible = true,
            EventFlagsSet = true,
            FitTexture = true,
        };

        iconImageNode.AddEvent(AddonEventType.MouseClick, data => {
            if (data.IsLeftClick()) {
                checkboxNode.IsChecked = !checkboxNode.IsChecked;
                OnChecked?.Invoke(checkboxNode.IsChecked);
            }
        });
        
        System.NativeController.AttachNode(iconImageNode, imageContainerNode);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        checkboxNode.Size = new Vector2(32.0f, 32.0f);
        checkboxNode.Position = new Vector2(Height, Height) / 2.0f - checkboxNode.Size / 2.0f;

        imageContainerNode.Size = new Vector2(Width - Height - 2.0f, Height);
        imageContainerNode.Position = new Vector2(Height + 2.0f, 0.0f);
    }

    public required uint ImageIconId {
        get;
        set {
            field = value;

            iconImageNode.IconId = value;
        }
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

    public void Update() {
        if (textureResized) return;

        if (iconImageNode.IsTextureReady) {
            RecalculateLayout();
            textureResized = true;
        }
    }

    public Action<bool>? OnChecked {
        get;
        set {
            field = value;
            checkboxNode.OnClick = value;
        }
    }

    public bool IsChecked {
        get => checkboxNode.IsChecked;
        set => checkboxNode.IsChecked = value;
    }
}
