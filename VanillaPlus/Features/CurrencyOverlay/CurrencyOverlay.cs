using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Enums;
using KamiToolKit.Overlay.UiOverlay;
using KamiToolKit.Premade.Addon;
using KamiToolKit.Premade.Addon.Search;
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
    private ListConfigAddon<CurrencySetting, CurrencyOverlayListItemNode, CurrencyOverlayConfigNode>? configAddon;
    private CurrencySearchAddon? itemSearchAddon;

    private OverlayController? overlayController;
    private List<CurrencyOverlayNode>? currencyNodes;

    public override async Task OnEnableAsync() {
        currencyNodes = [];

        config = await CurrencyOverlayConfig.Load();

        overlayController = new OverlayController();

        itemSearchAddon = new CurrencySearchAddon {
            InternalName = "CurrencySearch",
            Title = "Currency Search",
            Size = new Vector2(350.0f, 500.0f),
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
            foreach (var currencySetting in config.Currencies) {
                var newCurrencyNode = BuildCurrencyNode(currencySetting);
                currencyNodes.Add(newCurrencyNode);
                overlayController.AddNode(newCurrencyNode);
            }

            overlayController.Initialize();
        });
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

    public override async Task OnDisableAsync() {
        if (overlayController is not null) {
            await overlayController.DisposeAsync();
            overlayController = null;
        }

        if (itemSearchAddon is not null) {
            await itemSearchAddon.DisposeAsync();
            itemSearchAddon = null;
        }

        if (configAddon is not null) {
            await configAddon.DisposeAsync();
            configAddon = null;
        }

        config = null;

        currencyNodes?.Clear();
        currencyNodes = null;
    }

    private void OnAddButtonClicked(ListConfigAddon<CurrencySetting, CurrencyOverlayListItemNode, CurrencyOverlayConfigNode> listNode) {
        if (config is null) return;
        if (itemSearchAddon is null) return;
        if (currencyNodes is null) return;
        if (overlayController is null) return;

        itemSearchAddon.SelectionResult = searchResult => {
            var newCurrencyOption = new CurrencySetting {
                ItemId = searchResult.RowId,
            };

            config.Currencies.Add(newCurrencyOption);
            Task.Run(config.Save);
            listNode.RefreshList();
            listNode.SelectItem(newCurrencyOption);

            var newCurrencyNode = BuildCurrencyNode(newCurrencyOption);
            currencyNodes.Add(newCurrencyNode);
            overlayController.AddNode(newCurrencyNode);
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
