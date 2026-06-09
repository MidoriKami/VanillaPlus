using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Components.Configuration;
using KamiToolKit.Components.Search;
using KamiToolKit.Enums;
using KamiToolKit.UiOverlay;
using Lumina.Excel.Sheets;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Features.CurrencyOverlay.Nodes;

namespace VanillaPlus.Features.CurrencyOverlay;

public class CurrencyOverlay : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_CurrencyOverlay,
        Description = Strings.ModificationDescription_CurrencyOverlay,
        Type = ModificationType.NewOverlay,
        Authors = ["MidoriKami"],
    };

    public override string ImageName => "CurrencyOverlay.png";

    private CurrencyOverlayConfig? config;
    private ConfigurationAddon<CurrencySetting, CurrencyOverlayListItemNode, CurrencyOverlayConfigNode>? configAddon;
    private ItemSearchAddon? itemSearchAddon;

    private OverlayController? overlayController;
    private List<CurrencyOverlayNode>? currencyNodes;

    public override async Task OnEnableAsync() {
        currencyNodes = [];

        config = await CurrencyOverlayConfig.Load();

        itemSearchAddon = new ItemSearchAddon {
            InternalName = "CurrencySearch",
            Title = "Currency Search",
            Size = new Vector2(350.0f, 500.0f),
            AllowMultiselect = true,
            OptionsList = Services.DataManager.GetCurrencyItems().ToList(),
        };

        configAddon = new ConfigurationAddon<CurrencySetting, CurrencyOverlayListItemNode, CurrencyOverlayConfigNode> {
            Size = new Vector2(600.0f, 500.0f),
            InternalName = "CurrencyOverlayConfig",
            Title = Strings.CurrencyOverlay_ConfigTitle,
            OptionsList = config.Currencies,
            GetEntrySearchString = entry => Services.DataManager.GetExcelSheet<Item>().GetRow(entry.ItemId).Name.ToString(),
            AddClicked = OnAddButtonClicked,
            RemoveClicked = OnRemoveButtonClicked,
            SaveConfig = () => Task.Run(config.Save),
        };

        OpenConfigAction = configAddon.Toggle;

        await Services.Framework.Run(() => {
            overlayController = new OverlayController();

            foreach (var currencySetting in config.Currencies) {
                var newCurrencyNode = BuildCurrencyNode(currencySetting);
                currencyNodes.Add(newCurrencyNode);
                overlayController.AddNode(newCurrencyNode);
            }
        });
    }

    public override async Task OnDisableAsync() {
        await Services.Framework.Run(() => overlayController?.Dispose());
        overlayController = null;

        await Task.WhenAll(
            itemSearchAddon?.DisposeAsync().AsTask() ?? Task.CompletedTask,
            configAddon?.DisposeAsync().AsTask() ?? Task.CompletedTask
        );

        itemSearchAddon = null;
        configAddon = null;

        config = null;

        currencyNodes?.Clear();
        currencyNodes = null;
    }

    private void OnRemoveButtonClicked(CurrencySetting currencySetting) {
        if (currencyNodes is null) return;
        if (overlayController is null) return;
        if (config is null) return;

        var targetNode = currencyNodes.FirstOrDefault(node => node.Currency == currencySetting);
        if (targetNode is not null) {
            targetNode.DisableEditMode(NodeEditMode.Move | NodeEditMode.Resize);
            overlayController.RemoveNode(targetNode);
            currencyNodes.Remove(targetNode);
        }

        config.Currencies.Remove(currencySetting);
        Task.Run(config.Save);
    }

    private void OnAddButtonClicked() {
        if (config is null) return;
        if (itemSearchAddon is null) return;
        if (currencyNodes is null) return;
        if (overlayController is null) return;

        itemSearchAddon.ConfirmedSelections = searchResult => {
            foreach (var option in searchResult) {
                var newCurrencyOption = new CurrencySetting {
                    ItemId = option.RowId,
                };

                config.Currencies.Add(newCurrencyOption);
                var newCurrencyNode = BuildCurrencyNode(newCurrencyOption);
                currencyNodes.Add(newCurrencyNode);
                overlayController.AddNode(newCurrencyNode);
            }

            configAddon?.OptionsList = config.Currencies;
            Task.Run(config.Save);
        };
        itemSearchAddon.Toggle();
    }

    private unsafe CurrencyOverlayNode BuildCurrencyNode(CurrencySetting setting) {
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
