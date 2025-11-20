using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using KamiToolKit.Premade.Addons;
using Lumina.Excel.Sheets;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.CurrencyOverlay;

public unsafe class CurrencyOverlay : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Currency Overlay",
        Description = "Allows you to add additional currencies to your UI Overlay.\n\n" +
                      "Additionally allows you to set minimum and maximum values to trigger a warning.",
        Type = ModificationType.NewOverlay,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
            new ChangeLogInfo(2, "Reimplemented configuration system, now allows for changing scale"),
        ],
    };

    public override string ImageName => "CurrencyOverlay.png";

    private OverlayAddonController? overlayAddonController;

    private SimpleOverlayNode? overlayRootNode;
    private CurrencyOverlayConfig? config;

    private ListConfigAddon<CurrencySetting, CurrencyOverlayConfigNode>? configAddon;

    private List<CurrencyNode>? currencyNodes;

    private List<CurrencySetting>? addedCurrencySettings;
    private List<CurrencySetting>? removedCurrencySettings;
    
    private LuminaSearchAddon<Item>? itemSearchAddon;

    public override void OnEnable() {
        currencyNodes = [];
        addedCurrencySettings = [];
        removedCurrencySettings = [];
        
        itemSearchAddon = new LuminaSearchAddon<Item> {
            InternalName = "LuminaItemSearch",
            Title = "Item Search",
            Size = new Vector2(350.0f, 500.0f),

            GetLabelFunc = item => item.Name.ToString(),
            GetSubLabelFunc = item => item.ItemSearchCategory.Value.Name.ToString(),
            GetIconIdFunc = item => item.Icon,

            SortingOptions = [ "Alphabetical", "Id" ],
            SearchOptions = Services.DataManager.GetCurrencyItems().ToList(),
        };

        config = CurrencyOverlayConfig.Load();

        configAddon = new ListConfigAddon<CurrencySetting, CurrencyOverlayConfigNode> {
            Size = new Vector2(700.0f, 500.0f),
            InternalName = "CurrencyOverlayConfig",
            Title = "Currency Overlay Config",
            SortOptions = [ "Alphabetical" ],

            Options = config.Currencies,

            OnConfigChanged = changedSetting => {
                var nodes = currencyNodes.Where(node => node.Currency == changedSetting);

                foreach (var node in nodes) {
                    node.Currency = changedSetting;
                    node.OnMoveComplete = changedSetting.IsNodeMoveable ? config.Save : null;
                }
                config.Save();
            },

            OnAddClicked = item => {
                itemSearchAddon.SelectionResult = searchResult => {
                    var newCurrencyOption = new CurrencySetting {
                        ItemId = searchResult.RowId,
                    };

                    item.AddOption(newCurrencyOption);
                    addedCurrencySettings.Add(newCurrencyOption);
                    config.Save();
                };
                itemSearchAddon.Toggle();
            },
            
            OnItemRemoved = item => {
                removedCurrencySettings.Add(item);
                config.Save();
            },
        };

        OpenConfigAction = configAddon.Toggle;

        overlayAddonController = new OverlayAddonController();
        overlayAddonController.OnAttach += addon => {
            overlayRootNode = new SimpleOverlayNode {
                Size = addon->AtkUnitBase.Size(),
            };
            overlayRootNode.AttachNode((AtkUnitBase*)addon, NodePosition.AsFirstChild);

            var screenSize = new Vector2(AtkStage.Instance()->ScreenSize.Width, AtkStage.Instance()->ScreenSize.Height);

            foreach (var setting in config.Currencies) {
                var newCurrencyNode = BuildCurrencyNode(setting, screenSize);

                currencyNodes.Add(newCurrencyNode);
                newCurrencyNode.AttachNode((AtkUnitBase*)addon);
            }
        };

        overlayAddonController.OnUpdate += _ => {
            if (overlayRootNode is null) return;
            
            var screenSize = new Vector2(AtkStage.Instance()->ScreenSize.Width, AtkStage.Instance()->ScreenSize.Height);
            
            foreach (var toAdd in addedCurrencySettings) {
                var newCurrencyNode = BuildCurrencyNode(toAdd, screenSize);

                currencyNodes.Add(newCurrencyNode);
                newCurrencyNode.AttachNode(overlayRootNode);
            }
            addedCurrencySettings.Clear();

            foreach (var toRemove in removedCurrencySettings) {
                var node = currencyNodes.FirstOrDefault(node => node.Currency == toRemove);
                if (node is not null) {
                    currencyNodes.Remove(node);
                    node.Dispose();
                }
            }
            removedCurrencySettings.Clear();

            foreach (var currencyNode in currencyNodes) {
                currencyNode.UpdateValues();
            }
        };

        overlayAddonController.OnDetach += _ => {
            overlayRootNode?.Dispose();
            overlayRootNode = null;

            foreach (var currencyNode in currencyNodes) {
                currencyNode.Dispose();
            }
            
            currencyNodes.Clear();
        };
        
        overlayAddonController.Enable();
    }

    public override void OnDisable() {
        overlayAddonController?.Dispose();
        overlayAddonController = null;
        
        itemSearchAddon?.Dispose();
        itemSearchAddon = null;
        
        configAddon?.Dispose();
        configAddon = null;

        config = null;
        
        currencyNodes?.Clear();
        currencyNodes = null;
    }

    private CurrencyNode BuildCurrencyNode(CurrencySetting setting, Vector2 screenSize) {
        var newCurrencyNode = new CurrencyNode {
            Size = new Vector2(164.0f, 36.0f),
            Currency = setting,
        };

        newCurrencyNode.OnEditComplete = () => {
            setting.Position = newCurrencyNode.Position;
            config!.Save();
        };

        if (setting.Position == Vector2.Zero) {
            newCurrencyNode.Position = new Vector2(screenSize.X, screenSize.Y) / 2.0f - new Vector2(164.0f, 36.0f) / 2.0f;
        }
        else {
            newCurrencyNode.Position = setting.Position;
        }

        return newCurrencyNode;
    }
}
