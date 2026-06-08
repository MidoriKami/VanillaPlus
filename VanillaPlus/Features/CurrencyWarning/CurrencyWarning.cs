using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Components.Search;
using KamiToolKit.Enums;
using KamiToolKit.UiOverlay;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Features.CurrencyWarning.Nodes;
using VanillaPlus.Native.Addons;

namespace VanillaPlus.Features.CurrencyWarning;

public class CurrencyWarning : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.CurrencyWarning_DisplayName,
        Description = Strings.CurrencyWarning_Description,
        Type = ModificationType.NewOverlay,
        Authors = ["Zeffuro"],
    };

    private CurrencyWarningConfig? config;
    private OverlayController? overlayController;
    private CurrencyWarningOverlayNode? warningNode;
    private CurrencyTooltipNode? tooltipNode;

    private ConfigAddon? configWindow;
    private ListConfigAddon<CurrencyWarningSetting, CurrencyWarningSettingListItemNode, CurrencyWarningConfigNode>? listConfigWindow;
    private ItemSearchAddon? itemSearchAddon;

    public override string ImageName => "CurrencyWarning.png";

    public override async Task OnEnableAsync() {
        config = await CurrencyWarningConfig.Load();

        if (!config.IsConfigured) {
            config.IsMoveable = true;
            config.IsConfigured = true;
            await Task.Run(config.Save);
        }

        itemSearchAddon = new ItemSearchAddon {
            InternalName = "CurrencyWarningSearch",
            Title = Strings.CurrencyWarning_ItemSearchTitle,
            Size = new Vector2(350.0f, 500.0f),
            AllowMultiselect = true,
            OptionsList = Services.DataManager.GetCurrencyItems().ToList(),
        };

        listConfigWindow = new ListConfigAddon<CurrencyWarningSetting, CurrencyWarningSettingListItemNode, CurrencyWarningConfigNode> {
            InternalName = "CurrencyWarningList",
            Title = Strings.CurrencyWarning_ListTitle,
            Size = new Vector2(700.0f, 500.0f),
            SortOptions = [DefaultSortOptions.Alphabetical],
            Options = config.WarningSettings,
            ItemComparer = CurrencyWarningSetting.ItemComparer,
            IsSearchMatch = CurrencyWarningSetting.IsSearchMatch,
            AddClicked = OnAddClicked,
            RemoveClicked = OnRemoveClicked,
            EditCompleted = _ => Task.Run(config.Save),
        };

        configWindow = new ConfigAddon {
            InternalName = "CurrencyWarningConfig",
            Title = Strings.CurrencyWarning_ConfigTitle,
            Config = config,
        };

        configWindow.AddCategory(Strings.CurrencyWarning_CategoryGeneral)
            .AddCheckbox(Strings.CurrencyWarning_EnableMoving, nameof(config.IsMoveable))
            .AddCheckbox(Strings.CurrencyWarning_PlayAnimations, nameof(config.PlayAnimations))
            .AddCheckbox("Hide in Duties", nameof(config.HideInDuties))
            .AddFloatSlider(Strings.CurrencyWarning_IconScale, 0.5f, 5.0f, 0.1f, nameof(config.Scale))
            .AddColorEdit(Strings.CurrencyWarning_BelowColor, nameof(config.LowColor))
            .AddColorEdit(Strings.CurrencyWarning_AboveColor, nameof(config.HighColor));

        configWindow.AddCategory(Strings.CurrencyWarning_CategoryBelowIcon)
            .AddMultiSelectIcon(Strings.Icon, nameof(config.LowIcon), true, 60073u, 60357u, 230402u);

        configWindow.AddCategory(Strings.CurrencyWarning_CategoryAboveIcon)
            .AddMultiSelectIcon(Strings.Icon, nameof(config.HighIcon), true, 60074u, 63908u, 230403u);

        configWindow.AddCategory(Strings.CurrencyWarning_CategorySelection)
            .AddButton(Strings.CurrencyWarning_ConfigureButton, () => listConfigWindow.Toggle());

        OpenConfigAction = configWindow.Toggle;

        await Services.Framework.Run(() => {
            overlayController = new OverlayController();

            tooltipNode = new CurrencyTooltipNode {
                Config = config,
                IsVisible = false,
            };
            overlayController.AddNode(tooltipNode);

            unsafe {
                warningNode = new CurrencyWarningOverlayNode {
                    Config = config,
                    Size = new Vector2(48.0f, 48.0f),
                    TooltipNode = tooltipNode,
                    OnMoveComplete = thisNode => {
                        config.Position = thisNode.Position;
                        Task.Run(config.Save);
                    },
                    Position = config.Position != Vector2.Zero ? config.Position : (Vector2)AtkStage.Instance()->ScreenSize / 2.0f,
                };
            }

            overlayController.AddNode(warningNode);
        });
    }

    public override async Task OnDisableAsync() {
        await Services.Framework.Run(() => {
            overlayController?.Dispose();
            tooltipNode?.Dispose();
            warningNode?.Dispose();
        });

        overlayController = null;
        tooltipNode = null;
        warningNode = null;

        await Task.WhenAll(
            configWindow?.DisposeAsync().AsTask() ?? Task.CompletedTask,
            listConfigWindow?.DisposeAsync().AsTask() ?? Task.CompletedTask,
            itemSearchAddon?.DisposeAsync().AsTask() ?? Task.CompletedTask
        );

        configWindow = null;
        listConfigWindow = null;
        itemSearchAddon = null;

        config = null;
    }

    private void OnAddClicked(ListConfigAddon<CurrencyWarningSetting, CurrencyWarningSettingListItemNode, CurrencyWarningConfigNode> listNode) {
        itemSearchAddon?.ConfirmedSelections = selectedItems => {
            foreach (var option in selectedItems) {
                var newSetting = new CurrencyWarningSetting {
                    ItemId = option.RowId,
                    Mode = WarningMode.Above,
                    Limit = (int)option.StackSize,
                };

                config?.WarningSettings.Add(newSetting);
            }

            config?.Save();
            listNode.RefreshList();
        };
        itemSearchAddon?.Toggle();
    }

    private void OnRemoveClicked(ListConfigAddon<CurrencyWarningSetting, CurrencyWarningSettingListItemNode, CurrencyWarningConfigNode> _, CurrencyWarningSetting setting) {
        config?.WarningSettings.Remove(setting);
        config?.Save();
    }
}
