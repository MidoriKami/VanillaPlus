using System.Collections.Generic;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;

namespace VanillaPlus.NativeElements.Config;

public unsafe class ConfigAddon : NativeAddon {
    private ScrollingListNode? configurationListNode;

    private readonly List<ConfigCategory> configCategories = [];
    
    public required ISavable Config { get; init; }

    private const float MaximumHeight = 400.0f;
    private const float Width = 400.0f;

    protected override void OnSetup(AtkUnitBase* addon) {
        configurationListNode = new ScrollingListNode {
            AutoHideScrollBar = true,
            FitContents = true,
        };
        configurationListNode.AttachNode(this);

        foreach (var category in configCategories) {
            configurationListNode.AddNode(category.BuildNode());
        }
        RecalculateWindowSize();
    }

    private void RecalculateWindowSize() {
        if (configurationListNode is null) return;

        configurationListNode.RecalculateLayout();

        if (configurationListNode.VerticalListNode.Height < MaximumHeight) {
            Size = new Vector2(Width, configurationListNode.VerticalListNode.Height + ContentStartPosition.Y + 24.0f);
        }
        else {
            Size = new Vector2(Width, MaximumHeight + ContentStartPosition.Y + 24.0f);
        }
        
        SetWindowSize(Size);
        
        configurationListNode.Size = ContentSize + new Vector2(0.0f, ContentPadding.Y);
        configurationListNode.Position = ContentStartPosition - new Vector2(0.0f, ContentPadding.Y);
        configurationListNode.RecalculateLayout();

        foreach (var node in configurationListNode.GetNodes<TabbedVerticalListNode>()) {
            node.Width = configurationListNode.ContentWidth;
            node.RecalculateLayout();
        }
    }

    public ConfigCategory AddCategory(string label) {
        var newCategory = new ConfigCategory {
            CategoryLabel = label,
            ConfigObject = Config,
        };
        
        configCategories.Add(newCategory);
        return newCategory;
    }

    public override void Dispose() {
        base.Dispose();

        foreach (var category in configCategories) {
            category.Dispose();
        }
    }
}
