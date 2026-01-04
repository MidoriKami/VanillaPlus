using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Overlay;
using KamiToolKit.Premade.Addons;
using Lumina.Excel.Sheets;
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
    private CurrencyWarningNode? warningNode;
    private CurrencyTooltipNode? tooltipNode;

    private ConfigAddon? configWindow;
    private ListConfigAddon<CurrencyWarningSetting, CurrencyWarningConfigNode>? listConfigWindow;
    private LuminaSearchAddon<Item>? itemSearchAddon;

    public override void OnEnable() {
        config = CurrencyWarningConfig.Load();

        if (!config.IsConfigured) {
            config.IsMoveable = true;
            config.IsConfigured = true;
            config.Save();
        }
        
        overlayController = new OverlayController();

        InitializeConfiguration();
        CreateTooltipNode();
        CreateWarningNode();
    }

    private void InitializeConfiguration() {
        if (config is null) return;

        itemSearchAddon = new LuminaSearchAddon<Item> {
            InternalName = "CurrencyWarningSearch",
            Title = "Search Currencies",
            Size = new Vector2(350.0f, 500.0f),
            SearchOptions = Services.DataManager.GetCurrencyItems().ToList(),
            SortingOptions = [ "Name", "ID" ],
            GetLabelFunc = item => item.Name.ToString(),
            GetIconIdFunc = item => item.Icon,
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

    private void CreateTooltipNode() {
        if (config is null) return;

        overlayController?.CreateNode(() => tooltipNode = new CurrencyTooltipNode {
            Config = config,
        });
    }

    private void CreateWarningNode() {
        if (config is null) return;

        overlayController?.CreateNode(() => {
            warningNode = new CurrencyWarningNode {
                Config = config,
                Size = new Vector2(48.0f, 48.0f),
            };

            warningNode.OnUpdate = HandleWarningUpdate;

            var screenCenter = (Vector2)AtkStage.Instance()->ScreenSize / 2.0f;
            warningNode.Position = config.Position != Vector2.Zero ? config.Position : screenCenter;

            warningNode.OnMoveComplete = () => {
                config.Position = warningNode.Position;
                config.Save();
            };

            return warningNode;
        });
    }

    private void HandleWarningUpdate() {
        if (tooltipNode is null) return;
        if (warningNode is null) return;

        if (warningNode.IsHovered && warningNode.ActiveWarnings.Count > 0) {
            tooltipNode.UpdateContents(warningNode.ActiveWarnings);
            tooltipNode.IsVisible = true;
            UpdateTooltipPosition();
        } else {
            tooltipNode.IsVisible = false;
        }
    }

    private void UpdateTooltipPosition() {
        if (tooltipNode is null) return;
        if (warningNode is null) return;

        var screenSize = (Vector2)AtkStage.Instance()->ScreenSize;
        var iconScale = warningNode.Scale.X;
        var iconSize = warningNode.Size * iconScale;
        var tooltipSize = tooltipNode.Size;

        var targetX = warningNode.Position.X + iconSize.X + 10.0f;
        var targetY = warningNode.Position.Y;

        if (targetX + tooltipSize.X > screenSize.X) {
            targetX = warningNode.Position.X - tooltipSize.X - 10.0f;
        }

        if (targetY + tooltipSize.Y > screenSize.Y) {
            targetY = screenSize.Y - tooltipSize.Y - 10.0f;
        }

        if (targetY < 0) targetY = 10.0f;

        tooltipNode.Position = new Vector2(targetX, targetY);
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
