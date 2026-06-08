using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Components.Search;
using KamiToolKit.Enums;
using KamiToolKit.UiOverlay;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Features.CurrencyOverlay.Nodes;
using VanillaPlus.Native.Addons;

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
    private ListConfigAddon<CurrencySetting, CurrencyOverlayListItemNode, CurrencyOverlayConfigNode>? configAddon;
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

        configAddon = new ListConfigAddon<CurrencySetting, CurrencyOverlayListItemNode, CurrencyOverlayConfigNode> {
            Size = new Vector2(600.0f, 500.0f),
            InternalName = "CurrencyOverlayConfig",
            Title = Strings.CurrencyOverlay_ConfigTitle,
            SortOptions = [DefaultSortOptions.Alphabetical],
            Options = config.Currencies,
            ItemComparer = CurrencySetting.Comparison,
            IsSearchMatch = CurrencySetting.IsMatch,
            EditCompleted = _ => Task.Run(config.Save),
            AddClicked = OnAddButtonClicked,
            RemoveClicked = OnRemoveButtonClicked,
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

    private void OnRemoveButtonClicked(ListConfigAddon<CurrencySetting, CurrencyOverlayListItemNode, CurrencyOverlayConfigNode> _, CurrencySetting setting) {
        if (currencyNodes is null) return;
        if (overlayController is null) return;
        if (config is null) return;

        var targetNode = currencyNodes.FirstOrDefault(node => node.Currency == setting);
        if (targetNode is not null) {
            overlayController.RemoveNode(targetNode);
            currencyNodes.Remove(targetNode);
        }

        config.Currencies.Remove(setting);
        Task.Run(config.Save);
    }

    private void OnAddButtonClicked(ListConfigAddon<CurrencySetting, CurrencyOverlayListItemNode, CurrencyOverlayConfigNode> listNode) {
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

            Task.Run(config.Save);
            listNode.RefreshList();
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
