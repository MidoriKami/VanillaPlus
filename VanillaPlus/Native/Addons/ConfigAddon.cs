using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.BaseTypes;
using KamiToolKit.BaseTypes.ComponentNode;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;

namespace VanillaPlus.Native.Addons;

public class ConfigAddon : NativeAddon {
    private ScrollingNode<TabbedVerticalListNode>? configurationListNode;

    private readonly List<ConfigCategory> configCategories = [];

    public required ISavable Config { get; init; }

    private const float MaximumHeight = 400.0f;
    private const float Width = 400.0f;

    protected override unsafe void OnSetup(AtkUnitBase* addon, Span<AtkValue> atkValueSpan) {
        base.OnSetup(addon, atkValueSpan);

        configurationListNode = new ScrollingNode<TabbedVerticalListNode> {
            ContentNode = {
                FitContents = true,
                FitWidth = true,
                NavIndex = 1,
            },
            AutoHideScrollBar = true,
        };
        configurationListNode.AttachNode(this);

        foreach (var category in configCategories) {
            var builtCategory = category.BuildNode();
            foreach (var (node, tab) in builtCategory) {
                configurationListNode.ContentNode.AddNode(tab, node);
            }
        }

        var firstComponentNode  = configurationListNode.ContentNode.GetNodes<ComponentNode>().FirstOrDefault();
        if (firstComponentNode?.FocusNode is not null) {
            addon->FocusNode = firstComponentNode.FocusNode;
        }

        RecalculateWindowSize();
    }

    private void RecalculateWindowSize() {
        if (configurationListNode is null) return;

        configurationListNode.RecalculateSizes();

        if (configurationListNode.ContentNode.Height < MaximumHeight) {
            Size = new Vector2(Width, configurationListNode.ContentNode.Height + ContentStartPosition.Y + 24.0f);
        }
        else {
            Size = new Vector2(Width, MaximumHeight + ContentStartPosition.Y + 24.0f);
        }

        SetWindowSize(Size);

        configurationListNode.Size = ContentSize + new Vector2(0.0f, ContentPadding.Y);
        configurationListNode.Position = ContentStartPosition - new Vector2(0.0f, ContentPadding.Y);
        configurationListNode.RecalculateSizes();

        foreach (var node in configurationListNode.ContentNode.GetNodes<TabbedVerticalListNode>()) {
            node.Width = configurationListNode.ContentNode.Width;
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
