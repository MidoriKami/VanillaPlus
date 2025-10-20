using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Addon;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;

namespace VanillaPlus.NativeElements.Config;

public unsafe class ConfigAddon : NativeAddon {
    private ScrollingAreaNode<VerticalListNode>? configurationListNode;

    private readonly List<ConfigCategory> configCategories = [];
    
    public required ISavable Config { get; init; }

    private const float MaximumHeight = 400.0f;
    private const float Width = 400.0f;

    protected override void OnSetup(AtkUnitBase* addon) {
        configurationListNode = new ScrollingAreaNode<VerticalListNode> {
            IsVisible = true,
            ContentHeight = ContentSize.Y,
            AutoHideScrollBar = true,
        };
        AttachNode(configurationListNode);

        foreach (var category in configCategories) {
            var listNode = category.BuildNode();
            configurationListNode.ContentNode.AddNode(listNode);

            listNode.Width = configurationListNode.ContentNode.Width;
            listNode.RecalculateLayout();
        }

        configurationListNode.ContentHeight = configurationListNode.ContentNode.Nodes.Sum(node => node.Height);

        if (configurationListNode.ContentHeight < MaximumHeight) {
            Size = new Vector2(Width, configurationListNode.ContentHeight + ContentStartPosition.Y + 24.0f);
        }
        else {
            Size = new Vector2(Width, MaximumHeight + ContentStartPosition.Y + 24.0f);
        }

        addon->SetSize((ushort)Size.X, (ushort)Size.Y);
        WindowNode.Size = Size;
        configurationListNode.Size = ContentSize + new Vector2(0.0f, ContentPadding.Y);
        configurationListNode.Position = ContentStartPosition - new Vector2(0.0f, ContentPadding.Y);
    }

    public ConfigCategory AddCategory(string label) {
        var newCategory = new ConfigCategory {
            CategoryLabel = label,
            ConfigObject = Config,
        };
        
        configCategories.Add(newCategory);
        return newCategory;
    }
}
