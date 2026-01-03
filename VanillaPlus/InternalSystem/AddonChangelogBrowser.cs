using System.Linq;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;

namespace VanillaPlus.InternalSystem;

public class AddonChangelogBrowser : NativeAddon {

    private ScrollingTreeNode? scrollingTreeNode;

    protected override unsafe void OnSetup(AtkUnitBase* addon) {
        scrollingTreeNode = new ScrollingTreeNode {
            Size = ContentSize,
            Position = ContentStartPosition,
            ScrollSpeed = 100,
            AutoHideScrollBar = true,
        };
        scrollingTreeNode.AttachNode(this);

        if (Modification is not null) {
            foreach (var changelog in Modification.ModificationInfo.ChangeLog.OrderByDescending(log => log.Version)) {
                var categoryNode = new TreeListCategoryNode {
                    String = Strings.VersionLabelFormat.Format(changelog.Version),
                    OnToggle = _ => scrollingTreeNode.RecalculateLayout(),
                };

                var newTextNode = new TextNode {
                    Width = scrollingTreeNode.TreeListNode.Width,
                    TextFlags = TextFlags.MultiLine | TextFlags.WordWrap,
                    FontSize = 14,
                    LineSpacing = 22,
                    TextColor = ColorHelper.GetColor(1),
                };

                newTextNode.String = changelog.Description;
                newTextNode.Height = newTextNode.GetTextDrawSize(newTextNode.String).Y;
                
                categoryNode.RecalculateLayout();
                
                categoryNode.AddNode(newTextNode);
                scrollingTreeNode.AddCategoryNode(categoryNode);
            }

            scrollingTreeNode.RecalculateLayout();
        }
    }

    public GameModification? Modification { get; set; }
}
