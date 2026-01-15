using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using KamiToolKit.Overlay;

namespace VanillaPlus.Features.CurrencyWarning.Nodes;

public class CurrencyTooltipNode : OverlayNode {
    public override OverlayLayer OverlayLayer => OverlayLayer.AboveUserInterface;

    private readonly SimpleNineGridNode background;
    private readonly VerticalListNode listContainer;

    public required CurrencyWarningConfig Config { get; init; }

    public CurrencyTooltipNode() {
        background = new SimpleNineGridNode {
            TexturePath = "ui/uld/ToolTipS.tex",
            TextureCoordinates = new Vector2(0.0f, 0.0f),
            TextureSize = new Vector2(32.0f, 24.0f),
            TopOffset = 10,
            BottomOffset = 10,
            LeftOffset = 15,
            RightOffset = 15,
            Alpha = 0.95f,
        };
        background.AttachNode(this);

        listContainer = new VerticalListNode {
            Position = new Vector2(15.0f, 10.0f),
            ItemSpacing = 4.0f,
            FitContents = true,
        };
        listContainer.AttachNode(this);
    }

    protected override void OnUpdate() { }

    public void UpdateContents(List<WarningInfo> warnings) {
        listContainer.Clear();

        var maxRowWidth = 0.0f;

        foreach (var (iconId, name, count, isHigh, limit) in warnings) {
            var row = new HorizontalListNode {
                ItemSpacing = 8.0f, 
                Height = 24.0f,
            };

            row.AddNode(new IconImageNode {
                Size = new Vector2(24.0f, 24.0f),
                IconId = iconId,
                FitTexture = true,
            });

            var color = isHigh ? Config.HighColor : Config.LowColor;

            var text = new TextNode {
                String = $"{name} {(isHigh ? "Above Limit" : "Below Limit")}: {count:N0} / {limit:N0}",
                TextColor = color,
                FontSize = 14,
                TextFlags = TextFlags.Edge | TextFlags.AutoAdjustNodeSize,
                AlignmentType = AlignmentType.Left,
                Height = 24.0f,
            };
            row.AddNode(text);

            listContainer.AddNode(row);

            row.RecalculateLayout();
            var rowWidth = row.Nodes.Where(n => n.IsVisible).Sum(n => n.Width + row.ItemSpacing) - row.ItemSpacing;
            if (rowWidth > maxRowWidth) maxRowWidth = rowWidth;
        }

        listContainer.RecalculateLayout();

        var finalSize = new Vector2(maxRowWidth + 30, listContainer.Height + 20);
        background.Size = finalSize;
        Size = finalSize;
    }
}
