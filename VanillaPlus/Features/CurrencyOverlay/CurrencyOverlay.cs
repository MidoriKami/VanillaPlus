using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes.Controllers;
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

    private CurrencyOverlayConfig? config;
    private ListConfigAddon<CurrencySetting, CurrencyOverlayConfigNode>? configAddon;
    private LuminaSearchAddon<Item>? itemSearchAddon;

    private OverlayController? overlayController;
    private List<CurrencyNode>? currencyNodes;

    public override void OnEnable() {
        currencyNodes = [];

        overlayController = new OverlayController();
        
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

            OnAddClicked = listNode => {
                itemSearchAddon.SelectionResult = searchResult => {
                    var newCurrencyOption = new CurrencySetting {
                        ItemId = searchResult.RowId,
                    };

                    listNode.AddOption(newCurrencyOption);

                    var newCurrencyNode = BuildCurrencyNode(newCurrencyOption);
                    currencyNodes.Add(newCurrencyNode);
                    overlayController.AddNode(newCurrencyNode);
                    
                    config.Save();
                };
                itemSearchAddon.Toggle();
            },

            OnItemRemoved = setting => {
                var targetNode = currencyNodes.FirstOrDefault(node => node.Currency == setting);
                if (targetNode is not null) {
                    overlayController.RemoveNode(targetNode);
                    currencyNodes.Remove(targetNode);
                }

                config.Save();
            },
        };

        OpenConfigAction = configAddon.Toggle;

        Services.Framework.RunOnFrameworkThread(AddOverlayNodes);
    }

    private void AddOverlayNodes() {
        if (config is null) return;
        if (currencyNodes is null) return;
        if (overlayController is null) return;

        foreach (var currencySetting in config.Currencies) {
            var newCurrencyNode = BuildCurrencyNode(currencySetting);
            currencyNodes.Add(newCurrencyNode);
            overlayController.AddNode(newCurrencyNode);
        }
    }

    public override void OnDisable() {
        overlayController?.Dispose();
        overlayController = null;
        
        itemSearchAddon?.Dispose();
        itemSearchAddon = null;
        
        configAddon?.Dispose();
        configAddon = null;

        config = null;

        currencyNodes?.Clear();
        currencyNodes = null;
    }

    private CurrencyNode BuildCurrencyNode(CurrencySetting setting) {
        var newCurrencyNode = new CurrencyNode {
            Size = new Vector2(164.0f, 36.0f),
            Currency = setting,
        };

        newCurrencyNode.OnEditComplete = () => {
            setting.Position = newCurrencyNode.Position;
            config?.Save();
        };

        if (setting.Position == Vector2.Zero) {
            newCurrencyNode.Position = (Vector2)AtkStage.Instance()->ScreenSize / 2.0f - new Vector2(164.0f, 36.0f) / 2.0f;
        }
        else {
            newCurrencyNode.Position = setting.Position;
        }

        return newCurrencyNode;
    }
}
