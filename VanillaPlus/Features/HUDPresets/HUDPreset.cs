using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Controllers;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.NativeElements.Addons;

namespace VanillaPlus.Features.HUDPresets;

public unsafe class HUDPresets : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_HUDPresets,
        Description = Strings.ModificationDescription_HUDPresets,
        Type = ModificationType.UserInterface,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
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
            Size = new Vector2(250.0f, 150.0f),
            InternalName = "PresetNameWindow",
            Title = Strings.HUDPresets_RenameTitle,
            DepthLayer = 6,
        };

        hudLayoutController = new AddonController("_HudLayoutWindow");

        hudLayoutController.OnAttach += addon => {
            addon->Resize(addon->Size + new Vector2(0.0f, 95.0f));

            labelNode = new CategoryTextNode {
                Position = new Vector2(16.0f, 215.0f),
                AlignmentType = AlignmentType.Left,
                FontSize = 12,
                FontType = FontType.Axis,
                TextFlags = TextFlags.Emboss | TextFlags.AutoAdjustNodeSize,
                TextColor = ColorHelper.GetColor(8),
                String = Strings.HUDPresets_Label,
            };
            labelNode.AttachNode(addon);
            
            presetDropdownNode = new TextDropDownNode {
                Position = new Vector2(16.0f, 235.0f),
                Size = new Vector2(addon->Size.X - 32.0f, 24.0f),
                MaxListOptions = 10,
                Options = HUDPresetManager.GetPresetNames(),
                TextTooltip = Strings.HUDPresets_DropdownTooltip,
                OnOptionSelected = UpdateButtonLocks,
            };
            presetDropdownNode.AttachNode(addon);

            loadButtonNode = new TextButtonNode {
                Position = new Vector2(32.0f, 269.0f),
                Size = new Vector2(100.0f, 28.0f),
                String = Strings.HUDPresets_ButtonLoad,
                TextTooltip = Strings.HUDPresets_ButtonLoadTooltip,
                OnClick = LoadPreset,
                IsEnabled = false,
            };
            loadButtonNode.AttachNode(addon);
            
            overwriteButtonNode = new TextButtonNode {
                Position = new Vector2(144.0f, 269.0f),
                Size = new Vector2(100.0f, 28.0f),
                String = Strings.HUDPresets_ButtonOverwrite,
                TextTooltip = Strings.HUDPresets_ButtonOverwriteTooltip,
                IsEnabled = false,
                OnClick = OverwriteSelectedPreset,
            };
            overwriteButtonNode.AttachNode(addon);
            
            deleteButtonNode = new TextButtonNode {
                Position = new Vector2(256.0f, 269.0f),
                Size = new Vector2(100.0f, 28.0f),
                String = Strings.HUDPresets_ButtonDelete,
                // TooltipString = "Delete selected preset",
                IsEnabled = false,
                // OnClick = DeleteSelectedPreset,
            };
            deleteButtonNode.CollisionNode.TextTooltip = Strings.HUDPresets_DeleteTooltip;
            deleteButtonNode.AttachNode(addon);
            
            saveButtonNode = new TextButtonNode {
                Position = new Vector2(368.0f, 269.0f),
                Size = new Vector2(100.0f, 28.0f),
                String = Strings.HUDPresets_ButtonSave,
                OnClick = SaveCurrentLayout,
            };
            saveButtonNode.CollisionNode.TextTooltip = Strings.HUDPresets_SaveTooltipEnabled;
            saveButtonNode.AttachNode(addon);
        };

        hudLayoutController.OnDetach += addon => {
            addon->Resize(addon->Size - new Vector2(0.0f, 95.0f));

            presetDropdownNode?.Dispose();
            presetDropdownNode = null;

            loadButtonNode?.Dispose();
            loadButtonNode = null;
            
            overwriteButtonNode?.Dispose();
            overwriteButtonNode = null;
            
            deleteButtonNode?.Dispose();
            deleteButtonNode = null;
            
            saveButtonNode?.Dispose();
            saveButtonNode = null;
            
            labelNode?.Dispose();
            labelNode = null;
        };

        hudLayoutController.OnUpdate += addon => {
            if (saveButtonNode is not null) {
                var mainSaveButton = addon->GetComponentButtonById(16);
                if (mainSaveButton is not null) {
                    if (saveButtonNode.IsEnabled == mainSaveButton->IsEnabled) {
                        saveButtonNode.IsEnabled = !mainSaveButton->IsEnabled;

                        if (mainSaveButton->IsEnabled) {
                            saveButtonNode.CollisionNode.TextTooltip = Strings.HUDPresets_SaveTooltipDisabled;
                        }
                        else {
                            saveButtonNode.CollisionNode.TextTooltip = Strings.HUDPresets_SaveTooltipEnabled;
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
        if (presetDropdownNode?.SelectedOption is null) return;
        if (presetDropdownNode.SelectedOption == HUDPresetManager.DefaultOption) return;

        HUDPresetManager.LoadPreset(presetDropdownNode.SelectedOption);

        var screenAddon = Services.GameGui.GetAddonByName<AtkUnitBase>("_HudLayoutScreen");
        if (screenAddon is not null) {
            screenAddon->OnScreenSizeChange(AtkStage.Instance()->ScreenSize.Width, AtkStage.Instance()->ScreenSize.Height);
        }
    }

    private void SaveCurrentLayout() {
        if (renameAddon is null) return;

        renameAddon.PlaceholderString = Strings.HUDPresets_PlaceholderNewPreset;
        renameAddon.DefaultString = string.Empty;
        renameAddon.OnRenameComplete = newName => {
            HUDPresetManager.SavePreset(newName.ToString());
            if (presetDropdownNode?.Options is not null) {
                presetDropdownNode.Options.Add(newName.ToString());
                presetDropdownNode.RecalculateScrollParams();
            }
        };
        
        renameAddon.Toggle();
    }
    
    private void OverwriteSelectedPreset() {
        if (presetDropdownNode?.SelectedOption is null) return;
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
