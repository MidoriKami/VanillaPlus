using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.BaseTypes;
using KamiToolKit.Classes;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using Lumina.Text.ReadOnly;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Native.Nodes;

namespace VanillaPlus.Native.Addons;

public class ModificationBrowserAddon : NativeAddon {

    private TextInputNode? textInputNode;
    private ListNode<LoadedModification, GameModificationListItemNode>? listNode;
    private GameModificationInfoNode? modificationInfoNode;
    private Regex? searchRegex;

    private BrowserSelectedTab selectedTab = BrowserSelectedTab.All;
    private BrowserSelectedCategory selectedCategory = BrowserSelectedCategory.All;

    protected override unsafe void OnSetup(AtkUnitBase* addon, Span<AtkValue> atkValueSpan) {
        base.OnSetup(addon, atkValueSpan);

        new VerticalListNode { // Main Container
            Position = ContentStartPosition,
            Size = ContentSize,
            FitWidth = true,
            InitialNodes = [
                new HorizontalListNode { // Search Container
                    Height = 28.0f,
                    FitHeight = true,
                    Alignment = HorizontalListAnchor.Right,
                    NavIndex = 1,
                    NavDown = 3,
                    InitialNodes = [
                        new CheckboxNode { // Persist Search Button
                            Width = 28.0f,
                            TextTooltip = Strings.ModificationBrowser_PersistSearchTooltip,
                            IsChecked = System.SystemConfig.PersistSearch,
                            OnClick = value => {
                                System.SystemConfig.PersistSearch = value;
                                Task.Run(System.SystemConfig.Save);
                            },
                        },
                        textInputNode = new TextInputNode { // Search Input
                            Width = ContentSize.X - 28.0f,
                            PlaceholderString = Strings.SearchPlaceholder,
                            AutoSelectAll = true,
                            String = System.SystemConfig.PersistSearch ? System.SystemConfig.CurrentSearch : string.Empty,
                            OnInputReceived = OnSearchInputReceived,
                            OnFocusLost = () => {
                                if (System.SystemConfig.PersistSearch) {
                                    Task.Run(System.SystemConfig.Save);
                                }
                            },
                        },
                    ],
                },
                new HorizontalListNode { // Body Container
                    Height = ContentSize.Y - 28.0f,
                    FitHeight = true,
                    InitialNodes = [
                        new VerticalListNode { // Option Select Container
                            Width = ContentSize.X * 4.5f / 10.0f,
                            FitWidth = true,
                            InitialNodes = [
                                new TabBarNode { // Option Category Select
                                    Height = 28.0f,
                                    NavIndex = 3,
                                    NavUp = 1,
                                    NavDown = 6,
                                    InitialEntries = [
                                        new TabBarEntry{ Label = Strings.ModificationBrowser_TabAll, OnClick = () => SwitchTab(BrowserSelectedTab.All) },
                                        new TabBarEntry{ Label = Strings.ModificationBrowser_TabEnabled, OnClick = () => SwitchTab(BrowserSelectedTab.Enabled) },
                                        new TabBarEntry{ Label = Strings.ModificationBrowser_TabDisabled, OnClick = () => SwitchTab(BrowserSelectedTab.Disabled) },
                                    ],
                                },
                                new TabBarNode {
                                    Height = 28.0f,
                                    NavIndex = 6,
                                    NavUp = 3,
                                    NavDown = 10,
                                    InitialEntries = [
                                        new TabBarEntry{ Label = Strings.ModificationBrowser_TabAll, OnClick = () => SwitchCategory(BrowserSelectedCategory.All) },
                                        new TabBarEntry{ Label = Strings.ModificationBrowser_CategoryWindow, OnClick = () => SwitchCategory(BrowserSelectedCategory.Window), Tooltip = Strings.ModificationBrowser_CategoryWindowTooltip },
                                        new TabBarEntry{ Label = Strings.ModificationBrowser_CategoryUi, OnClick = () => SwitchCategory(BrowserSelectedCategory.Ui), Tooltip = Strings.ModificationBrowser_CategoryUiTooltip },
                                        new TabBarEntry{ Label = Strings.ModificationBrowser_CategoryBehavior, OnClick = () => SwitchCategory(BrowserSelectedCategory.Behavior), Tooltip = Strings.ModificationBrowser_CategoryBehaviorTooltip },
                                    ],
                                },
                                new ResNode { Height = 12.0f },
                                listNode = new ListNode<LoadedModification, GameModificationListItemNode> { // Options
                                    Height = ContentSize.Y - 28.0f - 28.0f - 28.0f - 12.0f,
                                    NavIndex = 10,
                                    NavUp = 6,
                                    NavRight = 100,
                                    OptionsList = GetModifications(),
                                    NoResultsString = Strings.ModificationBrowser_NoResults,
                                    OnItemSelected = selectedItem
                                        => modificationInfoNode?.SetDisplayedGameModification(selectedItem),
                                },
                            ],
                        },
                        modificationInfoNode = new GameModificationInfoNode { // Option Info Container
                            Width = ContentSize.X * 5.5f / 10.0f,
                        },
                    ],
                },
            ],
        }.AttachNode(this);

        modificationInfoNode.AddInteractionNode(addon);

        addon->FocusNode = textInputNode;

        if (System.SystemConfig.PersistSearch) {
            textInputNode?.String = System.SystemConfig.CurrentSearch;
            OnSearchInputReceived(System.SystemConfig.CurrentSearch);
        }
    }

    private void SwitchTab(BrowserSelectedTab tab) {
        selectedTab = tab;
        Task.Run(UpdateListNode);
    }

    private void SwitchCategory(BrowserSelectedCategory category) {
        selectedCategory = category;
        Task.Run(UpdateListNode);
    }

    private void UpdateListNode() {
        listNode?.OptionsList = GetModifications();
        listNode?.ResetScroll();
        modificationInfoNode?.SetDisplayedGameModification(null);
    }

    private void OnSearchInputReceived(ReadOnlySeString input) {
        try {
            searchRegex = new Regex(input.ToString(), RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            System.SystemConfig.CurrentSearch = input.ToString();
            listNode?.OptionsList = GetModifications();
            listNode?.ResetScroll();
        }
        catch (RegexParseException) {
            searchRegex = null;
        }
    }

    private List<LoadedModification> GetModifications() =>
        System.ModificationManager.LoadedModifications
            .Where(ShouldShowByLoadedState)
            .Where(ShouldShowByCategory)
            .Where(loadedModification => searchRegex is null || loadedModification.Modification.ModificationInfo.IsMatch(searchRegex))
            .OrderByDescending(loadedModification => loadedModification.Modification.ModificationInfo.Type is ModificationType.Debug)
            .ThenBy(loadedModification => {
                var isSeasonalGameMod = loadedModification.Modification.ModificationInfo.Type is ModificationType.Seasonal;
                return DateTime.Now.IsSeasonalEvent ? !isSeasonalGameMod : isSeasonalGameMod;
            })
            .ThenBy(loadedModification => loadedModification.Modification.ModificationInfo.DisplayName)
            .ToList();

    private bool ShouldShowByCategory(LoadedModification loadedModification) => selectedCategory switch {
        BrowserSelectedCategory.All
            => true,

        BrowserSelectedCategory.Window when loadedModification is {
            Modification.ModificationInfo.Type: ModificationType.NewWindow or ModificationType.NewOverlay or ModificationType.Seasonal,
        } => true,

        BrowserSelectedCategory.Ui when loadedModification is {
            Modification.ModificationInfo.Type: ModificationType.UserInterface or ModificationType.Seasonal,
        } => true,

        BrowserSelectedCategory.Behavior when loadedModification is {
            Modification.ModificationInfo.Type: ModificationType.GameBehavior or ModificationType.Seasonal,
        } => true,

        _ => false,
    };

    private bool ShouldShowByLoadedState(LoadedModification loadedModification) => selectedTab switch {
        BrowserSelectedTab.All
            => true,

        BrowserSelectedTab.Enabled when loadedModification is {
            State: LoadedState.Enabled,
        } => true,

        BrowserSelectedTab.Disabled when loadedModification is {
            State: LoadedState.Disabled,
        } => true,

        _ => false,
    };

    public unsafe void UpdateDisabledState() {
        listNode?.Update();

        if (InternalAddon is not null) {
            InternalAddon->UpdateCollisionNodeList(false);
        }
    }
}
