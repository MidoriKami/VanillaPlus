using System.Collections.Generic;
using System.Linq;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Common.Math;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Addons;
using KamiToolKit.Addons.Parts;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using Lumina.Excel.Sheets;
using VanillaPlus.Classes;
using Vector2 = System.Numerics.Vector2;

namespace VanillaPlus.Features.CurrencyOverlay;

public unsafe class CurrencyOverlay : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Currency Overlay",
        Description = "Allows you to add additional currencies to your UI Overlay.\n\n" +
                      "Additionally allows you to set minimum and maximum values to trigger a warning.",
        Type = ModificationType.NewOverlay,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "InitialChangelog"),
        ],
    };

    public override string ImageName => "CurrencyOverlay.png";

    private OverlayAddonController? overlayAddonController;

    private SimpleOverlayNode? overlayRootNode;
    private CurrencyOverlayConfig? config;

    private ModifyListAddon<CurrencySettings>? addRemoveAddon;
    private EditCurrencyAddon? editAddon;
    private CurrencyOverlayConfigAddon? configAddon;

    private List<CurrencyNode>? currencyNodes;

    private List<CurrencySettings>? addedCurrencySettings;
    private List<CurrencySettings>? removedCurrencySettings;

    private bool enableMoving;
    
    public override void OnEnable() {
        currencyNodes = [];
        addedCurrencySettings = [];
        removedCurrencySettings = [];
        
        config = CurrencyOverlayConfig.Load();

        configAddon = new CurrencyOverlayConfigAddon {
            NativeController = System.NativeController,
            Size = new Vector2(275.0f, 75.0f),
            InternalName = "CurrencyOverlayConfig",
            Title = "Currency Overlay Config",
            OnEnableMoving = toggleButtonNode => {
                enableMoving = !enableMoving;

                if (toggleButtonNode is not null) {
                    toggleButtonNode.String = enableMoving ? "Disable Moving" : "Enable Moving";
                }
                
                foreach (var node in currencyNodes) {
                    node.EnableMoving = enableMoving;
                }
            },
            OnEditEntriesClicked = () => {
                addRemoveAddon?.Open();
            },
        };

        editAddon = new EditCurrencyAddon {
            NativeController = System.NativeController,
            Size = new Vector2(300.0f, 315.0f),
            InternalName = "EditCurrency",
            Title = "Edit Currency",
            EditComplete = OnEditComplete,
            EditCancelled = OnEditCancelled,
        };
        
        addRemoveAddon = new ModifyListAddon<CurrencySettings> {
            NativeController = System.NativeController,
            Size = new Vector2(300.0f, 450.0f),
            InternalName = "CurrencySelect",
            Title = "Currency Configuration",
            Options = config.Currencies,
            AddNewEntry = OnAddClicked,
            RemoveEntry = OnRemoveClicked,
            GetOptionInfo = BuildOptionInfo,
        };

        OpenConfigAction = configAddon.Toggle;

        overlayAddonController = new OverlayAddonController();
        overlayAddonController.OnAttach += addon => {
            overlayRootNode = new SimpleOverlayNode {
                Size = addon->AtkUnitBase.Size(),
                IsVisible = true,
            };
            System.NativeController.AttachNode(overlayRootNode, addon->RootNode, NodePosition.AsFirstChild);

            var screenSize = AtkStage.Instance()->ScreenSize;

            foreach (var setting in config.Currencies) {
                var newCurrencyNode = BuildCurrencyNode(setting, screenSize);

                currencyNodes.Add(newCurrencyNode);
                System.NativeController.AttachNode(newCurrencyNode, overlayRootNode);
            }
        };

        overlayAddonController.OnUpdate += _ => {
            if (overlayRootNode is null) return;
            
            var screenSize = AtkStage.Instance()->ScreenSize;
            
            foreach (var toAdd in addedCurrencySettings) {
                var newCurrencyNode = BuildCurrencyNode(toAdd, screenSize);

                currencyNodes.Add(newCurrencyNode);
                System.NativeController.AttachNode(newCurrencyNode, overlayRootNode);
            }
            addedCurrencySettings.Clear();

            foreach (var toRemove in removedCurrencySettings) {
                var node = currencyNodes.FirstOrDefault(node => node.Currency == toRemove);
                if (node is not null) {
                    currencyNodes.Remove(node);
                    System.NativeController.DetachNode(node);
                    node.Dispose();
                }
            }
            removedCurrencySettings.Clear();

            foreach (var currencyNode in currencyNodes) {
                currencyNode.UpdateValues();
            }
        };

        overlayAddonController.OnDetach += _ => {
            System.NativeController.DisposeNode(ref overlayRootNode);

            foreach (var currencyNode in currencyNodes) {
                System.NativeController.DetachNode(currencyNode);
                currencyNode.Dispose();
            }
            
            currencyNodes.Clear();
        };
        
        overlayAddonController.Enable();
    }

    public override void OnDisable() {
        overlayAddonController?.Dispose();
        overlayAddonController = null;
        
        addRemoveAddon?.Dispose();
        addRemoveAddon = null;
        
        editAddon?.Dispose();
        editAddon = null;
        
        config = null;
        
        currencyNodes?.Clear();
        currencyNodes = null;
    }

    private CurrencyNode BuildCurrencyNode(CurrencySettings setting, Size screenSize) {
        var newCurrencyNode = new CurrencyNode {
            Size = new Vector2(164.0f, 36.0f),
            IsVisible = true,
            Currency = setting,
        };

        newCurrencyNode.OnEditComplete = () => {
            setting.Position = newCurrencyNode.Position;
            config!.Save();
        };

        if (setting.Position == Vector2.Zero) {
            newCurrencyNode.Position = new Vector2(screenSize.Width, screenSize.Height) / 2.0f - new Vector2(164.0f, 36.0f) / 2.0f;
        }
        else {
            newCurrencyNode.Position = setting.Position;
        }

        return newCurrencyNode;
    }

    private void OnAddClicked() {
        if (editAddon is null) return;

        editAddon.SelectedCurrency = new CurrencySettings();
        editAddon.Open();
    }

    private void OnRemoveClicked(CurrencySettings entry) {
        if (config is null) return;
        if (removedCurrencySettings is null) return;
        
        config.Currencies.Remove(entry);
        addRemoveAddon?.ResyncOptions(config.Currencies);
        config.Save();
        
        removedCurrencySettings.Add(entry);
    }

    private void OnEditComplete(CurrencySettings newOption) {
        if (config is null) return;
        if (addedCurrencySettings is null) return;
        
        config.Currencies.Add(newOption);
        config.Save();
        
        addedCurrencySettings.Add(newOption);

        addRemoveAddon?.ResyncOptions(config.Currencies);
    }

    private void OnEditCancelled(CurrencySettings result) {
        if (config is null) return;

        config.Save();
        addRemoveAddon?.ResyncOptions(config.Currencies);
    }

    private static OptionInfo<CurrencySettings> BuildOptionInfo(CurrencySettings option) => new() {
        Label = Services.DataManager.GetExcelSheet<Item>().GetRow(option.ItemId).Name.ToString(),
        SubLabel = Services.DataManager.GetExcelSheet<Item>().GetRow(option.ItemId).ItemSearchCategory.Value.Name.ToString().FirstCharToUpper(),
        IconId = Services.DataManager.GetExcelSheet<Item>().GetRow(option.ItemId).Icon,
        Id = option.ItemId,
        Option = option, 
    };
}
