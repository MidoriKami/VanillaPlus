using System.Drawing;
using System.Numerics;
using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Components.ConfigurationNodes;
using KamiToolKit.Nodes;
using VanillaPlus.NativeElements.Nodes;

namespace VanillaPlus.Features.WindowBackground.Nodes;

public class WindowBackgroundConfigNode : EntryConfigurationNode<WindowBackgroundSetting> {

    private readonly TabbedVerticalListNode verticalListNode;
    private readonly TextNode windowNameTextNode;
    private readonly ColorEditNode colorEditNode;
    private readonly Vector2EditWidget sizeEditWidget;

    public WindowBackgroundConfigNode() {
        windowNameTextNode = new TextNode {
            AlignmentType = AlignmentType.Center,
            FontSize = 18,
        };
        windowNameTextNode.AttachNode(ConfigurationContentNode);

        verticalListNode = new TabbedVerticalListNode {
            ItemSpacing = 20.0f,
            FitWidth = true,
            InitialTabbedNodes = [
                new TabbedListEntry(0, new CategoryTextNode {
                    String = Strings.WindowBackground_CategoryBackgroundColor,
                }),
                new TabbedListEntry(1, colorEditNode = new ColorEditNode {
                    Size = new Vector2(150.0f, 32.0f),
                    String = Strings.Color,
                }),
                new TabbedListEntry(0, new CategoryTextNode {
                    String = Strings.WindowBackground_CategoryPaddingSize,
                }),
                new TabbedListEntry(1, sizeEditWidget = new Vector2EditWidget {
                    Height = 50.0f,
                }),
            ],
        };
        verticalListNode.AttachNode(ConfigurationContentNode);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        windowNameTextNode.Size = new Vector2(Width, 24.0f);
        windowNameTextNode.Position = new Vector2(0.0f, 50.0f);

        verticalListNode.Width = Width;
        verticalListNode.Position = new Vector2(0.0f, Height / 3.0f);
        verticalListNode.RecalculateLayout();
    }

    protected override void PopulateEntryData(WindowBackgroundSetting entry) {
        windowNameTextNode.String = entry.AddonName;

        colorEditNode.CurrentColor = entry.Color;
        colorEditNode.DefaultColor = KnownColor.Black.Vector() with { W = 50.0f };
        colorEditNode.OnColorConfirmed = newColor => OnNewColorConfirmed(entry, newColor);

        sizeEditWidget.Value = entry.Padding;
        sizeEditWidget.OnValueChanged = newSize => OnSizeChanged(entry, newSize);

    }

    private void OnNewColorConfirmed(WindowBackgroundSetting entry, Vector4 newColor) {
        entry.Color = newColor;
        SaveConfig?.Invoke();
    }

    private void OnSizeChanged(WindowBackgroundSetting entry, Vector2 newValue) {
        entry.Padding = newValue;
        SaveConfig?.Invoke();
    }
}
