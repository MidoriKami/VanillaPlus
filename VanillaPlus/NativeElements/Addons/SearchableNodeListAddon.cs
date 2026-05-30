using System;
using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;

namespace VanillaPlus.NativeElements.Addons;

public class SearchableNodeListAddon<T, TU> : NodeListAddon<T, TU> where TU : ListItemNode<T>, IListItemNode, new() {

    private bool reverseSort;
    private string searchText = string.Empty;
    private Enum filterOption = DefaultSortOptions.Unset;

    public required List<Enum> DropDownOptions { get; init; }

    protected override unsafe void OnSetup(AtkUnitBase* addon, Span<AtkValue> atkValueSpan) {
        base.OnSetup(addon, atkValueSpan);

        new VerticalListNode {
            Position = ContentStartPosition,
            Size = ContentSize,
            FitWidth = true,
            InitialNodes = [
                new HorizontalFlexNode {
                    Height = 28.0f,
                    AlignmentFlags = FlexFlags.FitHeight | FlexFlags.FitWidth,
                    InitialNodes = [
                        new TextInputNode {
                            PlaceholderString = Strings.SearchPlaceholder,
                            String = searchText,
                            OnInputReceived = newSearchString => {
                                searchText = newSearchString.ToString();
                                OnSearchUpdated?.Invoke(searchText);
                            },
                        },
                    ],
                },
                new HorizontalListNode {
                    Height = 28.0f,
                    Alignment = HorizontalListAnchor.Right,
                    FitHeight = true,
                    InitialNodes = [
                        new CircleButtonNode {
                            Width = 28.0f,
                            Icon = ButtonIcon.Sort,
                            OnClick = () => {
                                reverseSort = !reverseSort;
                                OnSortingUpdated?.Invoke(filterOption, reverseSort);
                            },
                            TextTooltip = Strings.Tooltip_ReverseSortDirection,
                        },
                        new EnumDropDownNode<Enum> {
                            Width = ContentSize.X - 28.0f,
                            MaxListOptions = DropDownOptions.Count,
                            Options = DropDownOptions,
                            OnOptionSelected = newOption => {
                                filterOption = newOption;
                                OnSortingUpdated?.Invoke(newOption, reverseSort);
                            },
                            SelectedOption = DropDownOptions.First(),
                        },
                    ],
                },
                ListNode = new ListNode<T, TU> {
                    Height = ContentSize.Y - 28.0f - 28.0f,
                    OptionsList = ListItems,
                    ItemSpacing = ItemSpacing,
                },
            ],
        }.AttachNode(this);
    }

    protected override unsafe void OnUpdate(AtkUnitBase* addon) {
        base.OnUpdate(addon);

        ListNode?.Update();
    }

    public delegate void SearchUpdated(string searchString);

    public delegate void FilterUpdated(Enum sortingMode, bool reversed);

    public SearchUpdated? OnSearchUpdated { get; set; }
    public FilterUpdated? OnSortingUpdated { get; set; }
}
