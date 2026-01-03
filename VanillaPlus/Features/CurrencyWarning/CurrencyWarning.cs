using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Overlay;
using VanillaPlus.Classes;
using VanillaPlus.Features.CurrencyOverlay;
using VanillaPlus.NativeElements.Config;

namespace VanillaPlus.Features.CurrencyWarning;

public unsafe class CurrencyWarning : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Currency Warning",
        Description = "Shows a pulsing notification icon when tracked currencies hit limits.",
        Type = ModificationType.NewOverlay,
        Authors = [ "Zeffuro" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    private CurrencyWarningConfig? config;
    private CurrencyOverlayConfig? currencyConfig;
    private OverlayController? overlayController;
    private CurrencyWarningNode? warningNode;
    private ConfigAddon? configWindow;
    private CurrencyItemMultiSelectWindow? multiSelectWindow;

    private List<uint> trackedIds = [];

    public override void OnEnable() {
        config = CurrencyWarningConfig.Load();
        currencyConfig = CurrencyOverlayConfig.Load();
        overlayController = new OverlayController();

        trackedIds = currencyConfig.Currencies.Select(c => c.ItemId).ToList();

        multiSelectWindow = new CurrencyItemMultiSelectWindow(trackedIds, SyncCurrencies) {
            InternalName = "CurrencyMultiSelect",
            Title = "Select Tracked Currencies"
        };

        configWindow = new ConfigAddon {
            InternalName = "CurrencyWarningConfig",
            Title = "Currency Warning Config",
            Config = config,
        };

        configWindow.AddCategory("General")
            .AddCheckbox("Enable Moving", nameof(config.IsMoveable))
            .AddFloatSlider("Icon Scale", 0.5f, 3.0f, 2, 0.1f, nameof(config.Scale));

        configWindow.AddCategory("Tracking")
            .AddButton("Select Currencies to Track", () => multiSelectWindow.Toggle());

        OpenConfigAction = configWindow.Toggle;

        Services.Framework.RunOnFrameworkThread(() => {
            if (config == null || currencyConfig == null || overlayController == null) return;

            warningNode = new CurrencyWarningNode(config, currencyConfig) {
                Size = new Vector2(48.0f, 48.0f),
            };

            warningNode.OnMoveComplete = () => {
                config.Position = warningNode.Position;
                config.Save();
            };

            if (config.Position == Vector2.Zero) {
                warningNode.Position = (Vector2)AtkStage.Instance()->ScreenSize / 2.0f;
            } else {
                warningNode.Position = config.Position;
            }

            overlayController.AddNode(warningNode);
        });
    }

    private void SyncCurrencies() {
        if (currencyConfig == null) return;

        currencyConfig.Currencies.RemoveAll(c => !trackedIds.Contains(c.ItemId));

        foreach (var id in trackedIds) {
            if (currencyConfig.Currencies.Any(c => c.ItemId == id)) continue;

            currencyConfig.Currencies.Add(new CurrencySetting {
                ItemId = id,
                EnableHighLimit = true,
                HighLimit = 99999,
            });
        }

        currencyConfig.Save();
    }

    public override void OnDisable() {
        overlayController?.Dispose();
        overlayController = null;
        configWindow?.Dispose();
        configWindow = null;
        multiSelectWindow?.Dispose();
        multiSelectWindow = null;
        warningNode = null;
        config = null;
        currencyConfig = null;
        trackedIds.Clear();
    }
}
