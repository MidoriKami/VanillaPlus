using System;
using System.Linq;
using System.Numerics;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Addon;
using KamiToolKit.Addons;
using KamiToolKit.Addons.Parts;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using Lumina.Excel.Sheets;

namespace VanillaPlus.Features.CurrencyOverlay;

public unsafe class EditCurrencyAddon : NativeAddon {
    private IconImageNode? selectedCurrencyIconNode;
    private TextNode? selectedCurrencyTextNode;
    private TextButtonNode? selectCurrencyButtonNode;
    
    private CheckboxNode? minLimitCheckboxNode;
    private TextNode? minLimitLabelNode;
    private NumericInputNode? minLimitNumericInputNode;
    
    private CheckboxNode? maxLimitCheckboxNode;
    private TextNode? maxLimitLabelNode;
    private NumericInputNode? maxLimitNumericInputNode;

    private CheckboxNode? reverseDisplayCheckboxNode;
    
    private TextButtonNode? cancelButtonNode;
    private TextButtonNode? confirmButtonNode;

    private readonly SearchAddon<Item> selectItemWindow;
    
    public EditCurrencyAddon() {
        selectItemWindow = new SearchAddon<Item> {
            NativeController = System.NativeController,
            Size = new Vector2(400.0f, 750.0f),
            InternalName = "ItemSelect",
            Title = "Currency Selection",

            SortingOptions = [ "Alphabetically" ],

            SearchOptions = Services.DataManager.GetExcelSheet<Item>()
                .Where(item => item is { Name.IsEmpty: false, ItemUICategory.RowId: 100 } or { RowId: >= 1 and < 100, Name.IsEmpty: false })
                .ToList(),

            GetOptionInfo = option => new OptionInfo<Item> {
                Label = option.Name.ToString(),
                SubLabel = option.ItemSearchCategory.Value.Name.ToString().FirstCharToUpper(),
                IconId = option.Icon,
                Id = option.RowId,
                Option = option,
            },

            ItemComparison = (left, right, order) => order switch {
                "Alphabetically" => string.Compare(left.Name.ToString(), right.Name.ToString(), StringComparison.OrdinalIgnoreCase),
                _ => 0,
            },
            
            SelectionResult = result => {
                SelectedCurrency.ItemId = result.RowId;

                if (selectedCurrencyTextNode is not null) {
                    selectedCurrencyTextNode.String = result.Name.ToString();
                }

                if (selectedCurrencyIconNode is not null) {
                    selectedCurrencyIconNode.IconId = result.Icon;
                    selectedCurrencyIconNode.IsVisible = true;
                }

                if (confirmButtonNode is not null) {
                    confirmButtonNode.IsEnabled = SelectedCurrency.ItemId is not 0;
                }
            },
        };
    }

    public override void Dispose() {
        base.Dispose();

        selectItemWindow.Dispose();
    }

    protected override void OnHide(AtkUnitBase* addon) {
        selectItemWindow.Close();
    }

    protected override void OnSetup(AtkUnitBase* addon) {
        var itemInfo = Services.DataManager.GetExcelSheet<Item>().GetRow(SelectedCurrency.ItemId);

        selectedCurrencyIconNode = new IconImageNode {
            Size = new Vector2(24.0f, 24.0f),
            Position = ContentStartPosition + new Vector2(5.0f, 10.0f),
            IconId = 60042,
            IsVisible = true,
            FitTexture = true,
        };
        AttachNode(selectedCurrencyIconNode);
        
        selectedCurrencyTextNode = new TextNode {
            Size = new Vector2(175.0f, 24.0f),
            Position = ContentStartPosition + new Vector2(selectedCurrencyIconNode.X + selectedCurrencyIconNode.Width, 10.0f),
            FontSize = 14,
            AlignmentType = AlignmentType.Left,
            TextFlags = TextFlags.AutoAdjustNodeSize,
            IsVisible = true,
            String = SelectedCurrency.ItemId is 0 ? "Not Selected" : itemInfo.Name.ToString(),
        };
        AttachNode(selectedCurrencyTextNode);

        selectCurrencyButtonNode = new TextButtonNode {
            Size = new Vector2(150.0f, 24.0f),
            X = ContentStartPosition.X + ContentSize.X / 2.0f - 75.0f,
            Y = selectedCurrencyTextNode.Y + selectedCurrencyTextNode.Height + 5.0f,
            IsVisible = true,
            String = "Select Currency",
            OnClick = () => selectItemWindow.Open(),
        };
        AttachNode(selectCurrencyButtonNode);

        var verticalLayoutNode = new TabbedVerticalListNode {
            Size = new Vector2(ContentSize.X, ContentSize.Y - selectCurrencyButtonNode.Y - selectCurrencyButtonNode.Height),
            X = ContentStartPosition.X,
            Y = selectCurrencyButtonNode.Y + selectCurrencyButtonNode.Height + 10.0f,
            IsVisible = true,
            FitWidth = true,
        };
        AttachNode(verticalLayoutNode);
        
        minLimitCheckboxNode = new CheckboxNode {
            Size = new Vector2(verticalLayoutNode.Width, 24.0f),
            IsVisible = true,
            String = "Warn when below limit",
            IsChecked = SelectedCurrency.EnableLowLimit,
            OnClick = enabled => {
                SelectedCurrency.EnableLowLimit = enabled;
            },
        };
        verticalLayoutNode.AddNode(0, minLimitCheckboxNode);

        var minLimitLayoutNode = new HorizontalFlexNode {
            Width = verticalLayoutNode.Width,
            IsVisible = true,
            AlignmentFlags = FlexFlags.FitWidth | FlexFlags.FitContentHeight,
        };
        verticalLayoutNode.AddNode(1, minLimitLayoutNode);
        
        minLimitLabelNode = new TextNode {
            Size = new Vector2(125.0f, 24.0f),
            IsVisible = true,
            String = "Minimum",
            AlignmentType = AlignmentType.Right,
        };

        minLimitNumericInputNode = new NumericInputNode {
            Size = new Vector2(150.0f, 24.0f),
            IsVisible = true,
            OnValueUpdate = newValue => SelectedCurrency.LowLimit = newValue,
        };
        minLimitLayoutNode.AddNode(minLimitLabelNode, minLimitNumericInputNode);
        
        verticalLayoutNode.AddNode(new ResNode {
            Height = 10.0f,
            IsVisible = true,
        });
        
        maxLimitCheckboxNode = new CheckboxNode {
            Size = new Vector2(verticalLayoutNode.Width, 24.0f),
            IsVisible = true,
            String = "Warn when above limit",
            IsChecked = SelectedCurrency.EnableHighLimit,
            OnClick = enabled => {
                SelectedCurrency.EnableHighLimit = enabled;
            },
        };
        verticalLayoutNode.AddNode(0, maxLimitCheckboxNode);
        
        var maxLimitLayoutNode = new HorizontalFlexNode {
            Width = verticalLayoutNode.Width,
            IsVisible = true,
            AlignmentFlags = FlexFlags.FitWidth | FlexFlags.FitContentHeight,
        };
        verticalLayoutNode.AddNode(1, maxLimitLayoutNode);
        
        maxLimitLabelNode = new TextNode {
            Size = new Vector2(125.0f, 24.0f),
            IsVisible = true,
            String = "Maximum",
            AlignmentType = AlignmentType.Right,
        };

        maxLimitNumericInputNode = new NumericInputNode {
            Size = new Vector2(150.0f, 24.0f),
            IsVisible = true,
            OnValueUpdate = newValue => SelectedCurrency.HighLimit = newValue,
        };
        maxLimitLayoutNode.AddNode(maxLimitLabelNode, maxLimitNumericInputNode);
        
        verticalLayoutNode.AddNode(new ResNode {
            Height = 10.0f,
            IsVisible = true,
        });

        reverseDisplayCheckboxNode = new CheckboxNode {
            Size = new Vector2(verticalLayoutNode.Width, 24.0f),
            IsVisible = true,
            String = "Reverse Icon Position",
            IsChecked = SelectedCurrency.IconReversed,
            OnClick = enabled => {
                SelectedCurrency.IconReversed = enabled;
            },
        };
        verticalLayoutNode.AddNode(0, reverseDisplayCheckboxNode);
        
        cancelButtonNode = new TextButtonNode {
            Size = new Vector2(100.0f, 24.0f),
            Position = ContentStartPosition + new Vector2(0.0f, ContentSize.Y - 24.0f - 8.0f),
            IsVisible = true,
            String = "Cancel",
            OnClick = OnCancelClicked,
        };
        AttachNode(cancelButtonNode);
        
        confirmButtonNode = new TextButtonNode {
            Size = new Vector2(100.0f, 24.0f),
            Position = ContentStartPosition + new Vector2(ContentSize.X- 100.0f, ContentSize.Y - 24.0f - 8.0f),
            IsVisible = true,
            IsEnabled = SelectedCurrency.ItemId is not 0,
            String = "Confirm",
            OnClick = OnConfirmClicked,
        };
        AttachNode(confirmButtonNode);
    }

    public required Action<CurrencySettings> EditCancelled { get; init; }
    
    private void OnCancelClicked() {
        Close();
    }

    public required Action<CurrencySettings> EditComplete { get; init; }
    
    private void OnConfirmClicked() {
        EditComplete.Invoke(SelectedCurrency);
        Close();
    }

    public CurrencySettings SelectedCurrency { get; set; } = new();
}
