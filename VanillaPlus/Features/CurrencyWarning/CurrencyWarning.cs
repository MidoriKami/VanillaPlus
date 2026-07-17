using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Components.Search;
using KamiToolKit.UiOverlay;
using Lumina.Excel.Sheets;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Features.CurrencyWarning.Nodes;

using CurrencyWarningConfigAddon = KamiToolKit.Components.Configuration.TabbedConfigurationAddon<
    VanillaPlus.Features.CurrencyWarning.CurrencyWarningSetting,
    VanillaPlus.Features.CurrencyWarning.Nodes.CurrencyWarningSettingListItemNode,
    VanillaPlus.Features.CurrencyWarning.Nodes.CurrencyWarningConfigNode,
    VanillaPlus.Features.CurrencyWarning.Nodes.CurrencyWarningGeneralConfigNode>;

namespace VanillaPlus.Features.CurrencyWarning;

public class CurrencyWarning : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.CurrencyWarning_DisplayName,
        Description = Strings.CurrencyWarning_Description,
        Type = ModificationType.NewOverlay,
        Authors = ["Zeffuro"],
    };

    public static CurrencyWarningConfig? Config { get; private set; }

    private OverlayController? overlayController;

    private CurrencyWarningConfigAddon? configAddon;
    private ItemSearchAddon? itemSearchAddon;

    public override string ImageName => "CurrencyWarning.png";

    public override async Task OnEnableAsync() {
        Config = await CurrencyWarningConfig.Load();

        if (!Config.IsConfigured) {
            Config.IsMoveable = true;
            Config.IsConfigured = true;
            await Task.Run(Config.Save);
        }

        itemSearchAddon = new ItemSearchAddon {
            InternalName = "CurrencyWarningSearch",
            Title = Strings.CurrencyWarning_ItemSearchTitle,
            Size = new Vector2(350.0f, 500.0f),
            AllowMultiselect = true,
            OptionsList = IDataManager.Get().GetCurrencyItems().ToList(),
        };

        configAddon = new CurrencyWarningConfigAddon {
            InternalName = "CurrencyWarningList",
            Title = Strings.CurrencyWarning_ListTitle,
            Size = new Vector2(700.0f, 500.0f),
            OptionsList = Config.WarningSettings,
            GetEntrySearchString = entry => IDataManager.Get().GetExcelSheet<Item>().GetRow(entry.ItemId).Name.ToString(),
            AddClicked = OnAddClicked,
            RemoveClicked = OnRemoveClicked,
            SaveConfig = () => Task.Run(Config.Save),
        };

        OpenConfigAction = configAddon.Toggle;

        await IFramework.Get().RunSafely(() => {
            overlayController = new OverlayController();

            var tooltipNode = new CurrencyTooltipNode {
                Config = Config,
                IsVisible = false,
            };
            overlayController.AddNode(tooltipNode);

            unsafe {
                var warningNode = new CurrencyWarningOverlayNode {
                    Config = Config,
                    Size = new Vector2(48.0f, 48.0f),
                    TooltipNode = tooltipNode,
                    OnMoveComplete = thisNode => {
                        Config.Position = thisNode.Position;
                        Task.Run(Config.Save);
                    },
                    Position = Config.Position != Vector2.Zero ? Config.Position : (Vector2)AtkStage.Instance()->ScreenSize / 2.0f,
                };

                overlayController.AddNode(warningNode);
            }
        });
    }

    public override async Task OnDisableAsync() {
        await IFramework.Get().RunSafely(() => {
            overlayController?.Dispose();
        });
        overlayController = null;

        await Task.WhenAll(
            configAddon?.DisposeAsync().AsTask() ?? Task.CompletedTask,
            itemSearchAddon?.DisposeAsync().AsTask() ?? Task.CompletedTask
        );

        configAddon = null;
        itemSearchAddon = null;

        Config = null;
    }

    private void OnAddClicked() {
        if (Config is null) return;

        itemSearchAddon?.ConfirmedSelections = selectedItems => {
            foreach (var option in selectedItems) {
                var newSetting = new CurrencyWarningSetting {
                    ItemId = option.RowId,
                    Mode = WarningMode.Above,
                    Limit = (int)option.StackSize,
                };

                Config.WarningSettings.Add(newSetting);
            }

            configAddon?.OptionsList = Config.WarningSettings;
            Task.Run(Config.Save);
        };
        itemSearchAddon?.Toggle();
    }

    private static void OnRemoveClicked(CurrencyWarningSetting currencyWarningSetting) {
        if (Config is null) return;

        Config.WarningSettings.Remove(currencyWarningSetting);
        Task.Run(Config.Save);
    }
}
