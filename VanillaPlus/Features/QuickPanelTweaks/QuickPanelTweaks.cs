using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes.Controllers;
using VanillaPlus.Classes;
using VanillaPlus.NativeElements.Config;

namespace VanillaPlus.Features.QuickPanelTweaks;

public unsafe class QuickPanelTweaks : GameModification {

    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.QuickPanelTweaks_DisplayName,
        Description = Strings.QuickPanelTweaks_Description,
        Type = ModificationType.UserInterface,
        Authors = ["Pixis Lepus"],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    public override string ImageName => "QuickPanelTweaks.png";

    private AddonController? quickPanelController;
    private QuickPanelTweaksConfig? config;
    private QuickPanelData data = new QuickPanelData();
    private ConfigAddon? configWindow;

    public override void OnEnable() {
        config = QuickPanelTweaksConfig.Load();

        configWindow = new ConfigAddon {
            Size = new Vector2(600.0f, 125.0f),
            InternalName = "QuickPanelTweaksConfig",
            Title = Strings.QuickPanelTweaks_ConfigTitle,
            Config = config,
        };

        configWindow.AddCategory("")
            .AddCheckbox(Strings.QuickPanelTweaks_LabelHideHighlighting, nameof(config.HideHighlighting))
            .AddCheckbox(Strings.QuickPanelTweaks_LabelHideFocusBorder, nameof(config.HideFocusBorder))
            .AddCheckbox(Strings.QuickPanelTweaks_LabelHidePanelBackground, nameof(config.HidePanelBackground))
            .AddCheckbox(Strings.QuickPanelTweaks_LabelHideEmptySlots, nameof(config.HideEmptySlots))
            .AddCheckbox(Strings.QuickPanelTweaks_LabelMoveButtons, nameof(config.MoveButtons))
            .AddColorEdit(Strings.QuickPanelTweaks_BackgroundColor, nameof(config.BackgroundColor), new Vector4(0f, 0f, 0f, 1f));

        OpenConfigAction = configWindow.Toggle;

        quickPanelController = new AddonController("QuickPanel");
        quickPanelController.OnAttach += (addon) => {
            if (config is not null) data.updateFromConfig(config);
            updateNodes(addon);
        };
        quickPanelController.OnUpdate += (addon) => {
            updateDragDropComponents(addon);
        };
        quickPanelController.OnDetach += (addon) => {
            data.reset();
            updateNodes(addon);
            updateDragDropComponents(addon);
        };
        quickPanelController.Enable();
    }

    public override void OnDisable() {
        quickPanelController?.Dispose();
        quickPanelController = null;

        config = null;

        configWindow?.Dispose();
        configWindow = null;
    }

    private void updateNodes(AtkUnitBase* addon) {
        var windowNode = addon->GetComponentByNodeId(QuickPanelData.WindowComponentNodeId);

        if (windowNode is not null) {
            var focusBorderNode = windowNode->GetNodeById(QuickPanelData.FocusBorderNodeId);
            if (focusBorderNode is not null) {
                focusBorderNode->ToggleVisibility(!data.hideFocusBorder);
            }

            var highlightNode = windowNode->GetNodeById(QuickPanelData.HighlightNodeId);
            if (highlightNode is not null) {
                highlightNode->ToggleVisibility(!data.hideHighlighting);
            }

            var backgroundNode = windowNode->GetNodeById(QuickPanelData.BackgroundNodeId);
            if (backgroundNode is not null) {
                backgroundNode->AddRed = data.backgroundColor.red;
                backgroundNode->AddGreen = data.backgroundColor.green;
                backgroundNode->AddBlue = data.backgroundColor.blue;

                backgroundNode->X = data.backgroundPosition.x;
                backgroundNode->Y = data.backgroundPosition.y;
                backgroundNode->Width = data.backgroundSize.width;
                backgroundNode->Height = data.backgroundSize.height;
            }

            var closeButtonNode = windowNode->GetNodeById(QuickPanelData.CloseButtonNodeId);
            if (closeButtonNode is not null) {
                closeButtonNode->X = data.closeButtonPosition.x;
                closeButtonNode->Y = data.closeButtonPosition.y;
            }
        }

        var settingsButtonNode = addon->GetNodeById(QuickPanelData.SettingsButtonNodeId);
        if (settingsButtonNode is not null) {
            settingsButtonNode->X = data.settingsButtonPosition.x;
            settingsButtonNode->Y = data.settingsButtonPosition.y;
        }

        var skillBackgroundNode = addon->GetNodeById(QuickPanelData.PanelBackgroundNodeId);
        if (skillBackgroundNode is not null) {
            skillBackgroundNode->ToggleVisibility(!data.hideHighlighting);
        }
    }

    private void updateDragDropComponents(AtkUnitBase* addon, bool reset = false) {
        bool draggingOngoing = AtkStage.Instance()->DragDropManager.IsDragging;

        for (uint nodeId = QuickPanelData.CommandsStartNodeId; nodeId <= QuickPanelData.CommandsEndNodeId; nodeId++) {
            var skillNode = addon->GetComponentByNodeId(nodeId);
            if (skillNode is not null) {
                if (config?.HideEmptySlots ?? false) {
                    var skillIconNode = skillNode->GetNodeById(2);
                    if (skillIconNode->IsVisible() == false) {
                        skillNode->GetNodeById(3)->ToggleVisibility(draggingOngoing || data.hideEmptySlots);
                    }
                }
            }
        }
    }
}
