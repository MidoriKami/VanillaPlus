﻿using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Addon;
using KamiToolKit.Nodes;

namespace VanillaPlus.Core.Windows;

public class ChangelogBrowser : NativeAddon {

    private ScrollingAreaNode<TreeListNode>? scrollingAreaNode;
    
    private List<TreeListCategoryNode> categoryNodes = [];

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
                    TextFlags = TextFlags.MultiLine | TextFlags.WordWrap,
                    Width = ContentSize.X,
                    IsVisible = true,
                    FontSize = 14,
                    LineSpacing = 22,
                    Text = changelog.Description,
                };

                newTextNode.Height = newTextNode.GetTextDrawSize(newTextNode.Text).Y;
                
                categoryNode.AddNode(newTextNode);

                scrollingAreaNode.ContentNode.AddCategoryNode(categoryNode);
                categoryNodes.Add(categoryNode);
            }

            scrollingAreaNode.ContentHeight = categoryNodes.Sum(node => node.Height);
        }
    }
    
    public GameModification? Modification { get; set; }
}
