using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Overlay;
using KamiToolKit.Premade.Addons;
using KamiToolKit.Premade.SearchAddons;
using VanillaPlus.Classes;
using VanillaPlus.Features.CurrencyOverlay.Nodes;

namespace VanillaPlus.Features.CurrencyOverlay;

public unsafe class CurrencyOverlay : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_CurrencyOverlay,
        Description = Strings.ModificationDescription_CurrencyOverlay,
        Type = ModificationType.NewOverlay,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
            new ChangeLogInfo(2, "Reimplemented configuration system, now allows for changing scale"),
        ],
    };

    public override string ImageName => "CurrencyOverlay.png";

    private CurrencyOverlayConfig? config;
    private ListConfigAddon<CurrencySetting, CurrencyOverlayListItemNode, CurrencyOverlayConfigNode>? configAddon;
    private CurrencySearchAddon? itemSearchAddon;

    private OverlayController? overlayController;
    private List<CurrencyOverlayNode>? currencyNodes;

    public override void OnEnable() {
        currencyNodes = [];

        config = CurrencyOverlayConfig.Load();

        overlayController = new OverlayController();
        
        itemSearchAddon = new CurrencySearchAddon {
            InternalName = "CurrencySearch",
            Title = "Currency Search",
            Size = new Vector2(350.0f, 500.0f),
            SortingOptions = [ Strings.SortOptionAlphabetical, Strings.CurrencyOverlay_SortOptionId ],
        };

        configAddon = new ListConfigAddon<CurrencySetting, CurrencyOverlayListItemNode, CurrencyOverlayConfigNode> {
            Size = new Vector2(600.0f, 500.0f),
            InternalName = "CurrencyOverlayConfig",
            Title = Strings.CurrencyOverlay_ConfigTitle,
            SortOptions = [ Strings.SortOptionAlphabetical ],
            Options = config.Currencies,
            ItemComparer = (left, right, _) => left.CompareTo(right),
            IsSearchMatch = (item, search) => item.IsMatch(search),

            EditCompleted = _ => config.Save(),

            AddClicked = listNode => {
                itemSearchAddon.SelectionResult = searchResult => {
                    var newCurrencyOption = new CurrencySetting {
                        ItemId = searchResult.RowId,
                    };
            
                    config.Currencies.Add(newCurrencyOption);
                    config.Save();
                    listNode.RefreshList();
                    listNode.SelectItem(newCurrencyOption);
                    
                    var newCurrencyNode = BuildCurrencyNode(newCurrencyOption);
                    currencyNodes.Add(newCurrencyNode);
                    overlayController.AddNode(newCurrencyNode);
                };
                itemSearchAddon.Toggle();
            },
            
            RemoveClicked = (_, setting) => {
                var targetNode = currencyNodes.FirstOrDefault(node => node.Currency == setting);
                if (targetNode is not null) {
                    overlayController.RemoveNode(targetNode);
                    currencyNodes.Remove(targetNode);
                }

                config.Currencies.Remove(setting);
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

    private CurrencyOverlayNode BuildCurrencyNode(CurrencySetting setting) {
        var newCurrencyNode = new CurrencyOverlayNode {
            Size = new Vector2(164.0f, 36.0f),
            Currency = setting,
            OnMoveComplete = thisNode => {
                setting.Position = thisNode.Position;
                config?.Save();
            },
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
