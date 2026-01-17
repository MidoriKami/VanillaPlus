using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;

namespace VanillaPlus.NativeElements.Addons;

public unsafe class SearchableNodeListAddon<T, TU> : NodeListAddon<T, TU> where TU : ListItemNode<T>, new() {

    private TextInputNode? textInputNode;
    private TextDropDownNode? sortDropdownNode;
    
    private VerticalListNode? mainContainerNode;
    private HorizontalFlexNode? searchContainerNode;
    private HorizontalListNode? widgetsContainerNode;
    private CircleButtonNode? reverseButtonNode;
    
    private bool reverseSort;
    private string searchText = string.Empty;
    private string filterOption = string.Empty;
    
    public required List<string> DropDownOptions { get; init; }
    
    protected override void OnSetup(AtkUnitBase* addon) {
        const float dropDownWidth = 175.0f;

        mainContainerNode = new VerticalListNode {
            Position = ContentStartPosition,
            Size = ContentSize,
        };

        searchContainerNode = new HorizontalFlexNode {
            Size = new Vector2(ContentSize.X, 28.0f),
            AlignmentFlags = FlexFlags.FitHeight | FlexFlags.FitWidth,
        };

        widgetsContainerNode = new HorizontalListNode {
            Size = new Vector2(ContentSize.X, 28.0f),
            Alignment = HorizontalListAnchor.Right,
        };

        sortDropdownNode = new TextDropDownNode {
            Size = new Vector2(dropDownWidth, 28.0f),
            MaxListOptions = DropDownOptions.Count,
            Options = DropDownOptions,
            OnOptionSelected = newOption => {
                filterOption = newOption;
                OnSortingUpdated?.Invoke(newOption, reverseSort);
            },
        };
        sortDropdownNode.SelectedOption = DropDownOptions.First();

        reverseButtonNode = new CircleButtonNode {
            Size = new Vector2(28.0f, 28.0f),
            Icon = ButtonIcon.Sort,
            OnClick = () => {
                reverseSort = !reverseSort;
                OnSortingUpdated?.Invoke(filterOption, reverseSort);
            },
            TextTooltip = Strings.Tooltip_ReverseSortDirection,
        };

        textInputNode = new TextInputNode {
            PlaceholderString = Strings.SearchPlaceholder,
        };
        textInputNode.String = searchText;

        textInputNode.OnInputReceived += newSearchString => {
            searchText = newSearchString.ToString();
            OnSearchUpdated?.Invoke(searchText);
        };
        
        const float listPadding = 4.0f;
        
        ListNode = new ListNode<T, TU> {
            Size = ContentSize - new Vector2(0.0f, searchContainerNode.Height + widgetsContainerNode.Height + listPadding),
            Position = new Vector2(0.0f, listPadding),
            OptionsList = ListItems,
            ItemSpacing = ItemSpacing,
        };
        
        mainContainerNode.AttachNode(this);
        mainContainerNode.AddNode(searchContainerNode);
        searchContainerNode.AddNode(textInputNode);
        mainContainerNode.AddNode(widgetsContainerNode);
        widgetsContainerNode.AddNode(reverseButtonNode);

        sortDropdownNode.Width = widgetsContainerNode.AreaRemaining;
        widgetsContainerNode.AddNode(sortDropdownNode);

        mainContainerNode.AddDummy(4.0f);
        mainContainerNode.AddNode(ListNode);
    }

    protected override void OnUpdate(AtkUnitBase* addon) {
        base.OnUpdate(addon);

        ListNode?.Update();
    }

    public delegate void SearchUpdated(string searchString);
    public delegate void FilterUpdated(string filterString, bool reversed);

    public SearchUpdated? OnSearchUpdated { get; set; }
    public FilterUpdated? OnSortingUpdated { get; set; }
}
