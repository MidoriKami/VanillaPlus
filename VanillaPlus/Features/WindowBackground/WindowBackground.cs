using System.Collections.Generic;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.UI;
using KamiToolKit.Premade.Addons;
using KamiToolKit.Premade.Nodes;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.WindowBackground;

public unsafe class WindowBackground : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Window Backgrounds",
        Description = "Allows you to add a background to any native window.\n\n" +
                      "Examples: Cast Bar, Target Health Bar, Inventory Widget, Todo List.",
        Authors = ["MidoriKami"],
        Type = ModificationType.UserInterface,
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
            new ChangeLogInfo(2, "Added search bar to search 'All Windows' in config"),
            new ChangeLogInfo(3, "Fixed incorrectly cleaning up removed backgrounds"),
            new ChangeLogInfo(4, "Rewrote module to be more stable"),
            new ChangeLogInfo(5, "Reimplemented system to allow configuring the color and size per window"),
        ],
        CompatibilityModule = new SimpleTweaksCompatibilityModule("UiAdjustments@DutyListBackground"),
    };

    public override string ImageName => "WindowBackgrounds.png";

    private WindowBackgroundConfig? config;
    private ListConfigAddon<WindowBackgroundSetting, WindowBackgroundConfigNode>? configWindow;
    private SearchAddon<StringInfoNode>? addonSearchAddon;

    private WindowBackgroundController? backgroundController;

    public override void OnEnable() {
        addonSearchAddon = new SearchAddon<StringInfoNode> {
            InternalName = "AddonSearch",
            Title = "Window Search",
            Size = new Vector2(350.0f, 600.0f),
            SortingOptions = [ "Visibility", "Alphabetical" ],
            SearchOptions = GetOptions(),
        };

        config = WindowBackgroundConfig.Load();

        configWindow = new ListConfigAddon<WindowBackgroundSetting, WindowBackgroundConfigNode> {
            InternalName = "WindowBackgroundConfig",
            Title = "Window Backgrounds Config",
            Size = new Vector2(600.0f, 500.0f),
            Options = config.Settings,

            OnConfigChanged = _ => {
                backgroundController?.UpdateNodeStyles();
                config.Save();
            },

            OnAddClicked = listNode => {
                addonSearchAddon.SelectionResult = searchResult => {
                    var newOption = new WindowBackgroundSetting {
                        AddonName = searchResult.Label,
                    };

                    listNode.AddOption(newOption);
                    backgroundController?.AddAddon(newOption.AddonName);
                    config.Save();
                };

                addonSearchAddon.Toggle();
            },

            OnItemRemoved = oldItem => backgroundController?.RemoveAddon(oldItem.AddonName),
        };

        OpenConfigAction = configWindow.Toggle;

        backgroundController = new WindowBackgroundController(config);
    }

    public override void OnDisable() {
        addonSearchAddon?.Dispose();
        addonSearchAddon = null;

        configWindow?.Dispose();
        configWindow = null;

        backgroundController?.Dispose();
        backgroundController = null;

        config = null;
    }

    private static List<StringInfoNode> GetOptions() {
        List<StringInfoNode> results = [];

        foreach (var unit in RaptureAtkUnitManager.Instance()->AllLoadedUnitsList.Entries) {
            if (unit.Value is null) continue;
            if (!unit.Value->IsReady) continue;
            
            results.Add(new AddonStringInfoNode {
                Label = unit.Value->NameString,
            });
        }

        return results;
    }
}
