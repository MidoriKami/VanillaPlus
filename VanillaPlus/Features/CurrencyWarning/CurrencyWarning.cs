using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Overlay;
using KamiToolKit.Premade.Addons;
using KamiToolKit.Premade.SearchAddons;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Features.CurrencyWarning.Nodes;
using VanillaPlus.NativeElements.Config;

namespace VanillaPlus.Features.CurrencyWarning;

public unsafe class CurrencyWarning : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.CurrencyWarning_DisplayName,
        Description = Strings.CurrencyWarning_Description,
        Type = ModificationType.NewOverlay,
        Authors = [ "Zeffuro" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    private CurrencyWarningConfig? config;
    private OverlayController? overlayController;
    private CurrencyWarningOverlayNode? warningNode;
    private CurrencyTooltipNode? tooltipNode;

    private ConfigAddon? configWindow;
    private ListConfigAddon<CurrencyWarningSetting, CurrencyWarningSettingListItemNode, CurrencyWarningConfigNode>? listConfigWindow;
    private CurrencySearchAddon? itemSearchAddon;

    public override string ImageName => "CurrencyWarning.png";

    public override void OnEnable() {
        config = CurrencyWarningConfig.Load();

        if (!config.IsConfigured) {
            config.IsMoveable = true;
            config.IsConfigured = true;
            config.Save();
        }
        
        overlayController = new OverlayController();

        itemSearchAddon = new CurrencySearchAddon {
            InternalName = "CurrencyWarningSearch",
            Title = Strings.CurrencyWarning_ItemSearchTitle,
            Size = new Vector2(350.0f, 500.0f),
            SortingOptions = [ Strings.CurrencyWarning_SortOptionName, Strings.CurrencyWarning_SortOptionId ],
        };

        listConfigWindow = new ListConfigAddon<CurrencyWarningSetting, CurrencyWarningSettingListItemNode, CurrencyWarningConfigNode> {
            InternalName = "CurrencyWarningList",
            Title = Strings.CurrencyWarning_ListTitle,
            Size = new Vector2(700.0f, 500.0f),
            SortOptions = [ Strings.SortOptionAlphabetical ],
            Options = config.WarningSettings,
            ItemComparer = CurrencyWarningSetting.ItemComparer,
            IsSearchMatch = CurrencyWarningSetting.IsSearchMatch,
            AddClicked = OnAddClicked,
            RemoveClicked = OnRemoveClicked,
            EditCompleted = _ => config.Save(),
        };

        configWindow = new ConfigAddon {
            InternalName = "CurrencyWarningConfig",
            Title = Strings.CurrencyWarning_ConfigTitle,
            Config = config,
        };

        configWindow.AddCategory(Strings.CurrencyWarning_CategoryGeneral)
            .AddCheckbox(Strings.CurrencyWarning_EnableMoving, nameof(config.IsMoveable))
            .AddCheckbox(Strings.CurrencyWarning_PlayAnimations, nameof(config.PlayAnimations))
            .AddFloatSlider(Strings.CurrencyWarning_IconScale, 0.5f, 5.0f, 2, 0.1f, nameof(config.Scale))
            .AddColorEdit(Strings.CurrencyWarning_BelowColor, nameof(config.LowColor))
            .AddColorEdit(Strings.CurrencyWarning_AboveColor, nameof(config.HighColor));

        configWindow.AddCategory(Strings.CurrencyWarning_CategoryBelowIcon)
            .AddMultiSelectIcon(Strings.Icon, nameof(config.LowIcon), true, 60073u, 60357u, 230402u);

        configWindow.AddCategory(Strings.CurrencyWarning_CategoryAboveIcon)
            .AddMultiSelectIcon(Strings.Icon, nameof(config.HighIcon), true, 60074u, 63908u, 230403u);

        configWindow.AddCategory(Strings.CurrencyWarning_CategorySelection)
            .AddButton(Strings.CurrencyWarning_ConfigureButton, () => listConfigWindow.Toggle());

        OpenConfigAction = configWindow.Toggle;

        Services.Framework.RunOnFrameworkThread(LoadNodes);
    }

    private void LoadNodes() {
        if (config is null) return;
        
        tooltipNode = new CurrencyTooltipNode {
            Config = config,
            IsVisible = false,
        };
        overlayController?.AddNode(tooltipNode);
        
        warningNode = new CurrencyWarningOverlayNode {
            Config = config,
            Size = new Vector2(48.0f, 48.0f),
            TooltipNode = tooltipNode,
            OnMoveComplete = thisNode => {
                config.Position = thisNode.Position;
                config.Save();
            },
        };

        var screenCenter = (Vector2)AtkStage.Instance()->ScreenSize / 2.0f;
        warningNode.Position = config.Position != Vector2.Zero ? config.Position : screenCenter;
        
        overlayController?.AddNode(warningNode);
    }
    
    public override void OnDisable() {
        overlayController?.Dispose();
        overlayController = null;
        
        configWindow?.Dispose();
        configWindow = null;
        
        listConfigWindow?.Dispose();
        listConfigWindow = null;
        
        itemSearchAddon?.Dispose();
        itemSearchAddon = null;
        
        tooltipNode?.Dispose();
        tooltipNode = null;
        
        warningNode?.Dispose();
        warningNode = null;

        config = null;
    }

    private void OnAddClicked(ListConfigAddon<CurrencyWarningSetting, CurrencyWarningSettingListItemNode, CurrencyWarningConfigNode> listNode) {
        itemSearchAddon?.SelectionResult = item => {
            var newSetting = new CurrencyWarningSetting {
                ItemId = item.RowId, Mode = WarningMode.Above, Limit = (int)item.StackSize,
            };

            config?.WarningSettings.Add(newSetting);
            config?.Save();

            listNode.RefreshList();
            listNode.SelectItem(newSetting);
        };
        itemSearchAddon?.Toggle();
    }

    private void OnRemoveClicked(ListConfigAddon<CurrencyWarningSetting, CurrencyWarningSettingListItemNode, CurrencyWarningConfigNode> _, CurrencyWarningSetting setting) {
        config?.WarningSettings.Remove(setting);
        config?.Save();
    }
}
