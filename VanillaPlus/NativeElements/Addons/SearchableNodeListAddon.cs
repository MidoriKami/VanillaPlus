using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;

namespace VanillaPlus.NativeElements.Addons;

public unsafe class SearchableNodeListAddon : NodeListAddon {

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
                OnFilterUpdated(newOption, reverseSort);
            },
        };
        sortDropdownNode.SelectedOption = DropDownOptions.First();

        reverseButtonNode = new CircleButtonNode {
            Size = new Vector2(28.0f, 28.0f),
            Icon = ButtonIcon.Sort,
            OnClick = () => {
                reverseSort = !reverseSort;
                OnFilterUpdated(filterOption, reverseSort);
            },
            TextTooltip = Strings("Tooltip_ReverseSortDirection"),
        };

        textInputNode = new TextInputNode {
            PlaceholderString = Strings("SearchPlaceholder"),
        };
        textInputNode.SeString = searchText;

        textInputNode.OnInputReceived += newSearchString => {
            searchText = newSearchString.ToString();
            OnSearchUpdated(searchText);
        };
        
        const float listPadding = 4.0f;
        
        ScrollingAreaNode = new ScrollingAreaNode<VerticalListNode> {
            Size = ContentSize - new Vector2(0.0f, searchContainerNode.Height + widgetsContainerNode.Height + listPadding),
            Position = new Vector2(0.0f, listPadding),
            ContentHeight = 1000.0f,
        };
        
        mainContainerNode.AttachNode(this);
        mainContainerNode.AddNode(searchContainerNode);
        searchContainerNode.AddNode(textInputNode);
        mainContainerNode.AddNode(widgetsContainerNode);
        widgetsContainerNode.AddNode(reverseButtonNode);

        sortDropdownNode.Width = widgetsContainerNode.AreaRemaining;
        widgetsContainerNode.AddNode(sortDropdownNode);

        mainContainerNode.AddDummy(4.0f);
        mainContainerNode.AddNode(ScrollingAreaNode);
        
        DoListUpdate(true);
    }

    public delegate void SearchUpdated(string searchString);
    public delegate void FilterUpdated(string filterString, bool reversed);

    public required SearchUpdated OnSearchUpdated { get; init; }
    public required FilterUpdated OnFilterUpdated { get; init; }
}
