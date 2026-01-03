using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Nodes;

namespace VanillaPlus.Features.CurrencyWarning;

public unsafe class CurrencyItemMultiSelectWindow : NativeAddon {
    private readonly ICollection<uint> options;
    private readonly Action onSelectionChanged;

    public CurrencyItemMultiSelectWindow(ICollection<uint> options, Action onSelectionChanged) {
        this.options = options;
        this.onSelectionChanged = onSelectionChanged;

        Size = new Vector2(400.0f, 500.0f);
    }

    protected override void OnSetup(AtkUnitBase* addon) {
        var scrollable = new ScrollingAreaNode<VerticalListNode> {
            ContentHeight = ContentSize.Y,
            AutoHideScrollBar = true,
            Size = ContentSize,
            Position = ContentStartPosition,
        };

        scrollable.ContentNode.FitWidth = true;
        scrollable.ContentNode.FitContents = true;

        var currencies = Services.DataManager.GetCurrencyItems().ToList();

        foreach (var item in currencies) {
            var row = new HorizontalListNode {
                Height = 32.0f,
                ItemSpacing = 8.0f,
                Size = ContentSize with { Y = 32.0f },
            };

            var iconNode = new IconImageNode {
                Size = new Vector2(32.0f, 32.0f),
                IconId = item.Icon,
                FitTexture = true,
            };
            row.AddNode(iconNode);

            var checkbox = new CheckboxNode {
                Height = 24.0f,
                String = item.Name.ToString(),
                IsChecked = options.Contains(item.RowId),
                OnClick = newValue => {
                    if (newValue) {
                        if (!options.Contains(item.RowId)) options.Add(item.RowId);
                    }
                    else {
                        options.Remove(item.RowId);
                    }
                    onSelectionChanged?.Invoke();
                },
            };
            row.AddNode(checkbox);

            scrollable.ContentNode.AddNode(row);
        }

        scrollable.ContentNode.RecalculateLayout();
        scrollable.ContentHeight = scrollable.ContentNode.Height;
        scrollable.AttachNode(this);
    }
}
