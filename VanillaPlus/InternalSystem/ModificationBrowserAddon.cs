using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using VanillaPlus.Enums;
using VanillaPlus.InternalSystem.Nodes;

namespace VanillaPlus.InternalSystem;

public class ModificationBrowserAddon : NativeAddon {

    private TextInputNode? textInputNode;
    private TabBarNode? enabledStateTabBar;
    private TabBarNode? categoryTabBar;
    private ListNode<LoadedModification, GameModificationListItemNode>? listNode;
    private GameModificationInfoNode? modificationInfoNode;
    private Regex? searchRegex;

    private BrowserSelectedTab selectedTab = BrowserSelectedTab.All;
    private BrowserSelectedCategory selectedCategory = BrowserSelectedCategory.All;

    protected override Task BuildUiAsync() {
        new VerticalListNode { // Main Container
            Position = ContentStartPosition,
            Size = ContentSize,
            FitWidth = true,
            InitialNodes = [
                new HorizontalListNode { // Search Container
                    Height = 28.0f,
                    FitHeight = true,
                    Alignment = HorizontalListAnchor.Right,
                    InitialNodes = [
                        new CheckboxNode { // Persist Search Button
                            Width = 28.0f,
                            TextTooltip = "Persist Search Between Sessions",
                            IsChecked = System.SystemConfig.PersistSearch,
                            OnClick = value => {
                                System.SystemConfig.PersistSearch = value;
                                Task.Run(System.SystemConfig.Save);
                            },
                        },
                        textInputNode = new TextInputNode { // Search Input
                            Width = ContentSize.X - 28.0f,
                            PlaceholderString = "Search . . .",
                            AutoSelectAll = true,
                            String = System.SystemConfig.PersistSearch ? System.SystemConfig.CurrentSearch : string.Empty,
                            OnInputReceived = input => {
                                try {
                                    searchRegex = new Regex(input.ToString(), RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
                                    System.SystemConfig.CurrentSearch = input.ToString();
                                    listNode?.OptionsList = GetModifications();
                                }
                                catch (RegexParseException) {
                                    searchRegex = null;
                                }
                            },
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
                                enabledStateTabBar = new TabBarNode { // Option Category Select
                                    Height = 28.0f,
                                },
                                categoryTabBar = new TabBarNode {
                                    Height = 28.0f,
                                },
                                new ResNode { Height = 12.0f },
                                listNode = new ListNode<LoadedModification, GameModificationListItemNode> { // Options
                                    Height = ContentSize.Y - 28.0f - 28.0f - 28.0f - 12.0f,
                                    OptionsList = GetModifications(),
                                    NoResultsString = "No Results Match Search",
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

        enabledStateTabBar.AddTab("All", () => {
            selectedTab = BrowserSelectedTab.All;
            Task.Run(UpdateListNode);
        });

        enabledStateTabBar.AddTab("Enabled", () => {
            selectedTab = BrowserSelectedTab.Enabled;
            Task.Run(UpdateListNode);
        });

        enabledStateTabBar.AddTab("Disabled", () => {
            selectedTab = BrowserSelectedTab.Disabled;
            Task.Run(UpdateListNode);
        });

        categoryTabBar.AddTab("All", () => {
            selectedCategory = BrowserSelectedCategory.All;
            Task.Run(UpdateListNode);
        });

        categoryTabBar.AddTab("Window", () => {
            selectedCategory = BrowserSelectedCategory.Window;
            Task.Run(UpdateListNode);
        }, "New Windows or Overlays");

        categoryTabBar.AddTab("UI", () => {
            selectedCategory = BrowserSelectedCategory.Ui;
            Task.Run(UpdateListNode);
        }, "Modifies existing game UI");

        categoryTabBar.AddTab("Behavior", () => {
            selectedCategory = BrowserSelectedCategory.Behavior;
            Task.Run(UpdateListNode);
        }, "Changes how parts of the game work");

        unsafe {
            modificationInfoNode.AddInteractionNode(InternalAddon);
        }

        return Task.CompletedTask;

        void UpdateListNode() {
            listNode.OptionsList = GetModifications();
            listNode.ResetScroll();
            listNode.ClearSelection();
            modificationInfoNode.SetDisplayedGameModification(null);
        }
    }

    protected override unsafe void OnSetup(AtkUnitBase* addon, Span<AtkValue> atkValueSpan) {
        base.OnSetup(addon, atkValueSpan);

        if (System.SystemConfig.PersistSearch) {
            textInputNode?.String = System.SystemConfig.CurrentSearch;
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
