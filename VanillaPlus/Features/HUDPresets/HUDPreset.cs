using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;
using VanillaPlus.NativeElements.Addons;

namespace VanillaPlus.Features.HUDPresets;

public unsafe class HUDPresets : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "HUD Presets",
        Description = "Allows you to save and load an unlimited number of HUD Layouts.",
        Type = ModificationType.UserInterface,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "InitialChangelog"),
        ],
    };

    public override string ImageName => "HUDPresets.png";
    
    private AddonController? hudLayoutController;
    private TextDropDownNode? presetDropdownNode;

    private TextNode? labelNode;
    private TextButtonNode? loadButtonNode;
    private TextButtonNode? overwriteButtonNode;
    private TextButtonNode? deleteButtonNode;
    private TextButtonNode? saveButtonNode;
    
    private RenameAddon? renameAddon;

    public override void OnEnable() {
        renameAddon = new RenameAddon {
            NativeController = System.NativeController,
            Size = new Vector2(250.0f, 150.0f),
            InternalName = "PresetNameWindow",
            Title = "HUD Preset Name",
            DepthLayer = 6,
        };

        hudLayoutController = new AddonController("_HudLayoutWindow");

        hudLayoutController.OnAttach += addon => {
            addon->Resize(addon->GetSize() + new Vector2(0.0f, 95.0f));

            labelNode = new SimpleLabelNode {
                Position = new Vector2(16.0f, 215.0f),
                AlignmentType = AlignmentType.Left,
                FontSize = 12,
                FontType = FontType.Axis,
                TextFlags = TextFlags.Emboss | TextFlags.AutoAdjustNodeSize,
                TextColor = ColorHelper.GetColor(8),
                String = "[VanillaPlus] HUD Presets",
            };
            System.NativeController.AttachNode(labelNode, addon->RootNode);
            
            presetDropdownNode = new TextDropDownNode {
                Position = new Vector2(16.0f, 235.0f),
                Size = new Vector2(addon->GetSize().X - 32.0f, 24.0f),
                MaxListOptions = 10,
                Options = HUDPresetManager.GetPresetNames(),
                IsVisible = true,
                TooltipString = "Select a HUD Layout Preset",
                OnOptionSelected = UpdateButtonLocks,
            };
            System.NativeController.AttachNode(presetDropdownNode, addon->RootNode);

            loadButtonNode = new TextButtonNode {
                Position = new Vector2(32.0f, 269.0f),
                Size = new Vector2(100.0f, 28.0f),
                IsVisible = true,
                String = "Load",
                TooltipString = "Load selected preset",
                OnClick = LoadPreset,
                IsEnabled = false,
            };
            System.NativeController.AttachNode(loadButtonNode, addon->RootNode);
            
            overwriteButtonNode = new TextButtonNode {
                Position = new Vector2(144.0f, 269.0f),
                Size = new Vector2(100.0f, 28.0f),
                IsVisible = true,
                String = "Overwrite",
                TooltipString = "Overwrite selected preset",
                IsEnabled = false,
                OnClick = OverwriteSelectedPreset,
            };
            System.NativeController.AttachNode(overwriteButtonNode, addon->RootNode);
            
            deleteButtonNode = new TextButtonNode {
                Position = new Vector2(256.0f, 269.0f),
                Size = new Vector2(100.0f, 28.0f),
                IsVisible = true,
                String = "Delete",
                // TooltipString = "Delete selected preset",
                IsEnabled = false,
                // OnClick = DeleteSelectedPreset,
            };
            deleteButtonNode.CollisionNode.TooltipString = "Work in Progress\nManually delete preset files for now";
            System.NativeController.AttachNode(deleteButtonNode, addon->RootNode);
            
            saveButtonNode = new TextButtonNode {
                Position = new Vector2(368.0f, 269.0f),
                Size = new Vector2(100.0f, 28.0f),
                IsVisible = true,
                String = "Save",
                OnClick = SaveCurrentLayout,
            };
            System.NativeController.AttachNode(saveButtonNode, addon->RootNode);
        };

        hudLayoutController.OnDetach += addon => {
            addon->Resize(addon->GetSize() - new Vector2(0.0f, 95.0f));

            System.NativeController.DisposeNode(ref presetDropdownNode);
            System.NativeController.DisposeNode(ref loadButtonNode);
            System.NativeController.DisposeNode(ref overwriteButtonNode);
            System.NativeController.DisposeNode(ref deleteButtonNode);
            System.NativeController.DisposeNode(ref saveButtonNode);
        };

        hudLayoutController.OnUpdate += addon => {
            if (saveButtonNode is not null) {
                var mainSaveButton = addon->GetComponentButtonById(16);
                if (mainSaveButton is not null) {
                    if (saveButtonNode.IsEnabled == mainSaveButton->IsEnabled) {
                        saveButtonNode.IsEnabled = !mainSaveButton->IsEnabled;

                        if (mainSaveButton->IsEnabled) {
                            saveButtonNode.CollisionNode.TooltipString = "Click save above before saving a new preset";
                        }
                        else {
                            saveButtonNode.CollisionNode.TooltipString = "Save Current UI as a new preset";
                        }
                    }
                }
            }
        };

        hudLayoutController.Enable();
    }

    public override void OnDisable() {
        renameAddon?.Dispose();
        renameAddon = null;
        
        hudLayoutController?.Dispose();
        hudLayoutController = null;
    }

    private void LoadPreset() {
        if (presetDropdownNode is null) return;
        if (presetDropdownNode.SelectedOption is null) return;
        if (presetDropdownNode.SelectedOption == HUDPresetManager.DefaultOption) return;

        HUDPresetManager.LoadPreset(presetDropdownNode.SelectedOption);

        var screenAddon = Services.GameGui.GetAddonByName<AtkUnitBase>("_HudLayoutScreen");
        if (screenAddon is not null) {
            screenAddon->OnScreenSizeChange(AtkStage.Instance()->ScreenSize.Width, AtkStage.Instance()->ScreenSize.Height);
        }
    }

    private void SaveCurrentLayout() {
        if (renameAddon is null) return;

        renameAddon.PlaceholderString = "New Preset Name";
        renameAddon.DefaultString = string.Empty;
        renameAddon.ResultCallback = newName => {
            HUDPresetManager.SavePreset(newName);
            if (presetDropdownNode?.Options is not null) {
                presetDropdownNode.Options.Add(newName);
                presetDropdownNode.RecalculateScrollParams();
            }
        };
        
        renameAddon.Toggle();
    }
    
    private void OverwriteSelectedPreset() {
        if (presetDropdownNode is null) return;
        if (presetDropdownNode.SelectedOption is null) return;
        if (presetDropdownNode.SelectedOption == HUDPresetManager.DefaultOption) return;

        HUDPresetManager.SavePreset(presetDropdownNode.SelectedOption);
    }

    // Work in Progress. There are issues with dropdowns that make the user experience poor for now.
    // private void DeleteSelectedPreset() {
    //     if (presetDropdownNode is null) return;
    //     if (presetDropdownNode.SelectedOption is null) return;
    //     if (presetDropdownNode.SelectedOption == HUDPresetManager.DefaultOption) return;
    //     if (presetDropdownNode.Options is null) return;
    //
    //     HUDPresetManager.DeletePreset(presetDropdownNode.SelectedOption);
    //     presetDropdownNode.Options.Remove(presetDropdownNode.SelectedOption);
    //     presetDropdownNode.RecalculateScrollParams();
    //
    //     presetDropdownNode.SelectedOption = HUDPresetManager.DefaultOption;
    //     presetDropdownNode.LabelNode.String = HUDPresetManager.DefaultOption;
    // }

    private void UpdateButtonLocks(string selection) {
        if (loadButtonNode is null) return;
        if (overwriteButtonNode is null) return;
        if (deleteButtonNode is null) return;
        if (saveButtonNode is null) return;

        loadButtonNode.IsEnabled = selection != HUDPresetManager.DefaultOption;
        overwriteButtonNode.IsEnabled = selection != HUDPresetManager.DefaultOption;
        
        // Work in Progress. There are issues with dropdowns that make the user experience poor for now.
        // deleteButtonNode.IsEnabled = selection != HUDPresetManager.DefaultOption;
    }
}
