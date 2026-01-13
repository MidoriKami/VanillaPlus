using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Overlay;
using KamiToolKit.Premade.SearchAddons;
using VanillaPlus.Classes;
using VanillaPlus.NativeElements.Config;

namespace VanillaPlus.Features.CurrencyWarning;

public unsafe class CurrencyWarning : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Currency Warning",
        Description = "Shows a animated notification icon when tracked currencies approach or exceed set limits.",
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
    private ListConfigAddon<CurrencyWarningSetting, CurrencyWarningConfigNode>? listConfigWindow;
    private CurrencySearchAddon? itemSearchAddon;

    public override void OnEnable() {
        config = CurrencyWarningConfig.Load();

        if (!config.IsConfigured) {
            config.IsMoveable = true;
            config.IsConfigured = true;
            config.Save();
        }
        
        overlayController = new OverlayController();

        InitializeConfiguration();

        Services.Framework.RunOnFrameworkThread(LoadNodes);
    }

    private void InitializeConfiguration() {
        if (config is null) return;

        itemSearchAddon = new CurrencySearchAddon {
            InternalName = "CurrencyWarningSearch",
            Title = "Search Currencies",
            Size = new Vector2(350.0f, 500.0f),
            SortingOptions = [ "Name", "ID" ],
        };

        listConfigWindow = new ListConfigAddon<CurrencyWarningSetting, CurrencyWarningConfigNode> {
            InternalName = "CurrencyWarningList",
            Title = "Tracked Currencies",
            Size = new Vector2(700.0f, 500.0f),
            SortOptions = [ "Alphabetical" ],
            Options = config.WarningSettings,
            OnConfigChanged = _ => config.Save(),
            OnAddClicked = listNode => {
                itemSearchAddon.SelectionResult = item => {
                    var newSetting = new CurrencyWarningSetting {
                        ItemId = item.RowId,
                        Mode = WarningMode.Above,
                        Limit = (int)item.StackSize,
                    };
                    listNode.AddOption(newSetting);
                    config.Save();
                };
                itemSearchAddon.Toggle();
            },
            OnItemRemoved = _ => config.Save(),
        };

        configWindow = new ConfigAddon {
            InternalName = "CurrencyWarningConfig",
            Title = "Currency Warning Settings",
            Config = config,
        };

        configWindow.AddCategory("General")
            .AddCheckbox("Enable Moving", nameof(config.IsMoveable))
            .AddCheckbox("Play Animations", nameof(config.PlayAnimations))
            .AddFloatSlider("Icon Scale", 0.5f, 5.0f, 2, 0.1f, nameof(config.Scale))
            .AddColorEdit("Below Target Color", nameof(config.LowColor))
            .AddColorEdit("Above Target Color", nameof(config.HighColor));

        configWindow.AddCategory("Below Target Icon")
            .AddMultiSelectIcon(Strings.Icon, nameof(config.LowIcon), true, 60073u, 60357u, 230402u);

        configWindow.AddCategory("Above Target Icon")
            .AddMultiSelectIcon(Strings.Icon, nameof(config.HighIcon), true, 60074u, 63908u, 230403u);

        configWindow.AddCategory("Currency Selection")
            .AddButton("Configure Tracked Currencies", () => listConfigWindow.Toggle());

        OpenConfigAction = configWindow.Toggle;
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
}
