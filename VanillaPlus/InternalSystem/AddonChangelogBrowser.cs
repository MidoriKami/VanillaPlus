using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Addon;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;

namespace VanillaPlus.InternalSystem;

public class AddonChangelogBrowser : NativeAddon {

    private ScrollingAreaNode<TreeListNode>? scrollingAreaNode;
    
    private readonly List<TreeListCategoryNode> categoryNodes = [];

    protected override unsafe void OnSetup(AtkUnitBase* addon) {
        scrollingAreaNode = new ScrollingAreaNode<TreeListNode> {
            IsVisible = true,
            Size = ContentSize,
            Position = ContentStartPosition,
            ContentHeight = 1000.0f,
            ScrollSpeed = 100,
        };
        AttachNode(scrollingAreaNode);

        if (Modification is not null) {
            categoryNodes.Clear();

            foreach (var changelog in Modification.ModificationInfo.ChangeLog.OrderByDescending(log => log.Version)) {
                var categoryNode = new TreeListCategoryNode {
                    Label = $"Version {changelog.Version}",
                    Width = ContentSize.X,
                    IsVisible = true,
                    OnToggle = _ => scrollingAreaNode.ContentHeight = categoryNodes.Sum(node => node.Height),
                };

                var newTextNode = new TextNode {
                    Height = 32.0f,
                    AlignmentType = AlignmentType.TopLeft,
                    TextFlags = TextFlags.MultiLine | TextFlags.WordWrap | TextFlags.AutoAdjustNodeSize,
                    Position = new Vector2(6.0f, 2.0f),
                    Width = ContentSize.X - 32.0f,
                    IsVisible = true,
                    FontSize = 14,
                    LineSpacing = 22,
                    Text = changelog.Description,
                    TextColor = ColorHelper.GetColor(1),
                };

                newTextNode.Height = newTextNode.GetTextDrawSize(newTextNode.Text).Y;
                
                categoryNode.AddNode(newTextNode);

                scrollingAreaNode.ContentNode.AddCategoryNode(categoryNode);
                categoryNodes.Add(categoryNode);
            }

            scrollingAreaNode.ContentHeight = categoryNodes.Sum(node => node.Height);
        }
    }

    protected override unsafe void OnHide(AtkUnitBase* addon) {
        System.SystemConfig.ChangelogWindowPosition = Position;
        System.SystemConfig.Save();
    }

    public GameModification? Modification { get; set; }
}
