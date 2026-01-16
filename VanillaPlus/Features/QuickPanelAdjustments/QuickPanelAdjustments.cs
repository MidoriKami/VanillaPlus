using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.NativeElements.Config;

namespace VanillaPlus.Features.QuickPanelAdjustments;

public unsafe class QuickPanelAdjustments : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.QuickPanelAdjustments_DisplayName,
        Description = Strings.QuickPanelAdjustments_Description,
        Type = ModificationType.UserInterface,
        Authors = ["Pixis Lepus"],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    public override string ImageName => "QuickPanelAdjustments.png";

    private AddonController? quickPanelController;
    private QuickPanelAdjustmentsConfig? config;
    private ConfigAddon? configWindow;

    public override void OnEnable() {
        config = QuickPanelAdjustmentsConfig.Load();

        configWindow = new ConfigAddon {
            Size = new Vector2(600.0f, 125.0f),
            InternalName = "QuickPanelAdjustmentsConfig",
            Title = Strings.QuickPanelAdjustments_ConfigTitle,
            Config = config,
        };

        configWindow.AddCategory("")
            .AddCheckbox(Strings.QuickPanelAdjustments_LabelHideHighlighting, nameof(config.HideHighlighting))
            .AddCheckbox(Strings.QuickPanelAdjustments_LabelHideFocusBorder, nameof(config.HideFocusBorder))
            .AddCheckbox(Strings.QuickPanelAdjustments_LabelHidePanelBackground, nameof(config.HidePanelBackground))
            .AddCheckbox(Strings.QuickPanelAdjustments_LabelHideEmptySlots, nameof(config.HideEmptySlots))
            .AddCheckbox(Strings.QuickPanelAdjustments_LabelMoveButtons, nameof(config.MoveButtons))
            .AddColorEdit(Strings.QuickPanelAdjustments_BackgroundColor, nameof(config.BackgroundColor), new Vector4(1.0f, 1.0f, 1.0f, 25.0f / 255.0f));

        OpenConfigAction = configWindow.Toggle;

        quickPanelController = new AddonController("QuickPanel");
        quickPanelController.OnUpdate += UpdateQuickPanelStyle;
        quickPanelController.Enable();
    }

    public override void OnDisable() {
        quickPanelController?.Dispose();
        quickPanelController = null;

        config = null;

        configWindow?.Dispose();
        configWindow = null;
    }

    private void UpdateQuickPanelStyle(AtkUnitBase* addon) {
        if (config is null) return;

        var windowComponent = addon->GetComponentById<AtkComponentWindow>(45);
        if (windowComponent is null) return;

        var highlightNode = windowComponent->GetNodeById(10);
        if (highlightNode is not null) {
            highlightNode->ToggleVisibility(!config.HideHighlighting);
        }

        var focusBorderNode = windowComponent->GetNodeById(8);
        if (focusBorderNode is not null) {
            focusBorderNode->ToggleVisibility(addon->IsFocused && !config.HideFocusBorder);
        }

        var backgroundNode = windowComponent->GetNodeById(9);
        if (backgroundNode is not null) {
            backgroundNode->ToggleVisibility(!config.HidePanelBackground);
            backgroundNode->AddColor = Vector3.Zero;
            backgroundNode->ColorVector = config.BackgroundColor;
        }
        
        var panelBackgroundNode = addon->GetNodeById(44);
        if (panelBackgroundNode is not null) {
            panelBackgroundNode->ToggleVisibility(!config.HidePanelBackground);
        }

        foreach (uint index in Enumerable.Range(19, 25)) {
            var componentDragDrop = addon->GetComponentById<AtkComponentDragDrop>(index);
            if (componentDragDrop is null) continue;

            var iconComponentNode = componentDragDrop->GetNodeById(2);
            if (iconComponentNode is null) continue;

            var slotFrameNode = componentDragDrop->GetNodeById(3);
            if (slotFrameNode is null) continue;
        
            slotFrameNode->ToggleVisibility(!config.HideEmptySlots && !iconComponentNode->IsVisible());
        }

        var closeButtonNode = windowComponent->GetNodeById(6);
        var settingsButtonNode = addon->GetNodeById(2);

        if (closeButtonNode is not null && settingsButtonNode is not null) {
            if (config.MoveButtons) {
                closeButtonNode->Position = new Vector2(234.0f, 37.0f);
                settingsButtonNode->Position = new Vector2(206.0f, 32.0f);
            }
            else {
                closeButtonNode->Position = new Vector2(258.0f, 10.0f);
                settingsButtonNode->Position = new Vector2(232.0f, 6.0f);
            }
        }
    }
}
