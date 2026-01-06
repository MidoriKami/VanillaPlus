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
    private QuickPanelTweaksState? state;
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
            .AddColorEdit(Strings.QuickPanelTweaks_BackgroundColor, nameof(config.BackgroundColor), new(0f, 0f, 0f, 1f));

        OpenConfigAction = configWindow.Toggle;

        state = new QuickPanelTweaksState();

        quickPanelController = new AddonController("QuickPanel");
        quickPanelController.OnAttach += (addon) => {
            state.updateFromConfig(config);
            updateNodes(addon, state);
        };
        quickPanelController.OnUpdate += (addon) => {
            updateDragDropComponents(addon, state);
        };
        quickPanelController.OnDetach += (addon) => {
            state = new QuickPanelTweaksState();
            updateNodes(addon, state);
            updateDragDropComponents(addon, state);
        };
        quickPanelController.Enable();
    }

    public override void OnDisable() {
        quickPanelController?.Dispose();
        quickPanelController = null;

        config = null;
        state = null;

        configWindow?.Dispose();
        configWindow = null;
    }

    private void updateNodes(AtkUnitBase* addon, QuickPanelTweaksState state) {
        var windowComponentNode = addon->GetComponentNodeById(45);
        var windowComponent = windowComponentNode is null ? null : windowComponentNode->GetAsAtkComponentWindow();

        if (windowComponent is not null) {
            var focusBorderNode = windowComponent->GetNodeById(8);
            if (focusBorderNode is not null) {
                focusBorderNode->ToggleVisibility(!state.hideFocusBorder);
            }

            var backgroundNode = windowComponent->GetNodeById(9);
            if (backgroundNode is not null) {
                backgroundNode->SetColor(state.backgroundColor);
                backgroundNode->Position = state.backgroundPosition;
                backgroundNode->Size = state.backgroundSize;
            }

            var highlightNode = windowComponent->GetNodeById(10);
            if (highlightNode is not null) {
                highlightNode->ToggleVisibility(!state.hideHighlighting);
            }

            var closeButtonNode = windowComponent->GetNodeById(6);
            if (closeButtonNode is not null) {
                closeButtonNode->Position = state.closeButtonPosition;
            }
        }

        var settingsButtonNode = addon->GetNodeById(2);
        if (settingsButtonNode is not null) {
            settingsButtonNode->Position = state.settingsButtonPosition;
        }

        var panelBackgroundNode = addon->GetNodeById(44);
        if (panelBackgroundNode is not null) {
            panelBackgroundNode->ToggleVisibility(!state.hidePanelBackground);
        }
    }

    private void updateDragDropComponents(AtkUnitBase* addon, QuickPanelTweaksState state) {
        bool draggingOngoing = AtkStage.Instance()->DragDropManager.IsDragging;

        for (uint nodeId = 19; nodeId <= 43; nodeId++) {
            var skillComponentNode = addon->GetComponentNodeById(nodeId);
            var skillComponent = skillComponentNode is null ? null : skillComponentNode->GetAsAtkComponentDragDrop();
            if (skillComponent is not null) {
                var slotImageNode = skillComponent->GetNodeById(3);
                if (slotImageNode is not null) {
                    slotImageNode->ToggleVisibility(draggingOngoing || !state.hideEmptySlots);
                }
            }
        }
    }
}
