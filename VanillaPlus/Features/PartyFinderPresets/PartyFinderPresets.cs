using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Addons;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;
using VanillaPlus.NativeElements.Addons;

namespace VanillaPlus.Features.PartyFinderPresets;

public unsafe class PartyFinderPresets : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Party Finder Presets",
        Description = "Allows you to save an use presets for the Party Finder Recruitment window",
        Type = ModificationType.GameBehavior,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "InitialChangelog"),
            new ChangeLogInfo(2, "Reworked configuration systems, allows for easier renaming of existing presets"),
        ],
    };

    private AddonController<AtkUnitBase>? recruitmentCriteriaController;
    private AddonController<AtkUnitBase>? lookingForGroupController;

    private TextButtonNode? savePresetButton;
    private TextDropDownNode? presetDropDown;
    
    private RenameAddon? savePresetWindow;

    private ListConfigAddon<PresetInfo, PartyFinderPresetConfigNode>? presetEditorAddon;

    public override string ImageName => "PartyFinderPresets.png";

    public override void OnEnable() {
        presetEditorAddon = new ListConfigAddon<PresetInfo, PartyFinderPresetConfigNode> {
            NativeController = System.NativeController,
            Size = new Vector2(600.0f, 400.0f),
            InternalName = "PresetEditorConfig",
            Title = "Preset Editor Config Manager",

            Options = GetPresetInfos(),

            OnConfigChanged = _ => {
                UpdateDropDownOptions();
            },

            OnItemRemoved = toRemove => {
                PresetManager.DeletePreset(toRemove.Name);
                UpdateDropDownOptions();
            },
        };

        savePresetWindow = new RenameAddon {
            NativeController = System.NativeController,
            InternalName = "PartyFinderPresetRename",
            Title = "Party Finder Preset",
            IsInputValid = PresetManager.IsValidFileName,
            OnRenameComplete = newOption => {
                PresetManager.SavePreset(newOption);
                presetEditorAddon.Options = GetPresetInfos();
                UpdateDropDownOptions();
            },
        };

        OpenConfigAction = presetEditorAddon.Toggle;
        
        Services.AddonLifecycle.RegisterListener(AddonEvent.PreReceiveEvent, "LookingForGroup", OnLookingForGroupEvent);
        Services.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "LookingForGroupCondition", OnLookingForGroupConditionFinalize);

        recruitmentCriteriaController = new AddonController<AtkUnitBase>("LookingForGroupCondition");
        recruitmentCriteriaController.OnAttach += addon => {
            savePresetButton = new TextButtonNode {
                Position = new Vector2(406.0f, 605.0f),
                Size = new Vector2(160.0f, 28.0f),
                IsVisible = true,
                String = "Save Preset",
                Tooltip = "[VanillaPlus]: Save current settings to a preset",
                OnClick = savePresetWindow.Open,
            };
            System.NativeController.AttachNode(savePresetButton, addon->RootNode);
        };

        recruitmentCriteriaController.OnDetach += _ => {
            System.NativeController.DisposeNode(ref savePresetButton);
        };

        recruitmentCriteriaController.Enable();

        lookingForGroupController = new AddonController<AtkUnitBase>("LookingForGroup");

        lookingForGroupController.OnAttach += addon => {
            if (presetDropDown is not null) {
                System.NativeController.DisposeNode(ref presetDropDown);
            }

            presetDropDown = new TextDropDownNode {
                Position = new Vector2(185.0f, 636.0f),
                Size = new Vector2(200.0f, 25.0f),
                MaxListOptions = 10,
                Options = PresetManager.GetPresetNames(),
                IsVisible = true,
            };

            UpdateDropDownOptions();

            System.NativeController.AttachNode(presetDropDown, addon->RootNode);
        };
        
        lookingForGroupController.OnDetach += _ => {
            System.NativeController.DetachNode(presetDropDown);
        };
        
        lookingForGroupController.Enable();
    }

    public override void OnDisable() {
        Services.AddonLifecycle.UnregisterListener(OnLookingForGroupEvent, OnLookingForGroupConditionFinalize);

        recruitmentCriteriaController?.Dispose();
        recruitmentCriteriaController = null;
        
        lookingForGroupController?.Dispose();
        lookingForGroupController = null;
        
        savePresetWindow?.Dispose();
        savePresetWindow = null;
        
        presetEditorAddon?.Dispose();
        presetEditorAddon = null;

        System.NativeController.DisposeNode(ref presetDropDown);
        presetDropDown = null;
    }

    private void OnLookingForGroupConditionFinalize(AddonEvent type, AddonArgs args)
        => UpdateDropDownOptions();

    private void OnLookingForGroupEvent(AddonEvent type, AddonArgs args) {
        if (args is not AddonReceiveEventArgs eventArgs) return;
        if ((AtkEventType)eventArgs.AtkEventType is not AtkEventType.ButtonClick) return;
        if (eventArgs.EventParam is not 2) return;
        if (presetDropDown?.SelectedOption is not { } selectedOption) return;
        if (selectedOption is PresetManager.DefaultString) return;
        if (selectedOption is PresetManager.DontUseString) return;
        
        PresetManager.LoadPreset(selectedOption);
    }

    private void UpdateDropDownOptions() {
        if (presetDropDown is not null) {
            var presets = PresetManager.GetPresetNames();
            var anyPresets = presets.All(presetName => presetName != PresetManager.DefaultString);
            
            presetDropDown.Options = presets;
            presetDropDown.IsEnabled = anyPresets;

            if (anyPresets) {
                presetDropDown.Tooltip = "[VanillaPlus]: Select a preset";
            }
            else {
                presetDropDown.Tooltip = "[VanillaPlus]: No presets saved";
            }
        }
    }

    private static List<PresetInfo> GetPresetInfos() => PresetManager.GetPresetNames()
        .Where(name => name != PresetManager.DefaultString && name != PresetManager.DontUseString)
        .Select(name => new PresetInfo {
            Name = name,
        }).ToList();
}
