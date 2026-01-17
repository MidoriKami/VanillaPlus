using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using KamiToolKit.Nodes;
using KamiToolKit.Premade.Addons;
using KamiToolKit.Premade.ListItemNodes;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.NativeElements.Addons;

namespace VanillaPlus.Features.PartyFinderPresets;

public unsafe class PartyFinderPresets : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_PartyFinderPresets,
        Description = Strings.ModificationDescription_PartyFinderPresets,
        Type = ModificationType.GameBehavior,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
            new ChangeLogInfo(2, "Reworked configuration systems, allows for easier renaming of existing presets"),
        ],
    };

    private AddonController<AtkUnitBase>? recruitmentCriteriaController;
    private AddonController<AtkUnitBase>? lookingForGroupController;

    private TextButtonNode? savePresetButton;
    private TextDropDownNode? presetDropDown;
    
    private RenameAddon? savePresetWindow;

    private ListConfigAddon<string, StringListItemNode, PartyFinderPresetConfigNode>? presetEditorAddon;

    public override string ImageName => "PartyFinderPresets.png";

    public override void OnEnable() {
        presetEditorAddon = new ListConfigAddon<string, StringListItemNode, PartyFinderPresetConfigNode> {
            Size = new Vector2(600.0f, 400.0f),
            InternalName = "PresetConfigManager",
            Title = Strings.Title_PresetConfigManager,
            Options = GetPresetInfos(),
            ItemSpacing = 2.0f,
            RemoveClicked = (_, toRemove) => {
                PresetManager.DeletePreset(toRemove);
                UpdateDropDownOptions();
            },
            EditCompleted = _ => {
                presetEditorAddon?.RefreshList();
                presetEditorAddon?.SelectItem(null);
            },
            ItemComparer = (left, right, _) => string.Compare(left, right, StringComparison.OrdinalIgnoreCase),
            IsSearchMatch = (item, searchString) => {
                var regex = new Regex(searchString, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
                return regex.IsMatch(item);
            },
        };

        savePresetWindow = new RenameAddon {
            InternalName = "PartyFinderPresetRename",
            Title = Strings.Title_PartyFinderPreset,
            IsInputValid = PresetManager.IsValidFileName,
            OnRenameComplete = newOption => {
                PresetManager.SavePreset(newOption.ToString());
                presetEditorAddon.Options = GetPresetInfos();
                UpdateDropDownOptions();
            },
        };

        OpenConfigAction = presetEditorAddon.Toggle;
        
        Services.AddonLifecycle.RegisterListener(AddonEvent.PreReceiveEvent, "LookingForGroup", OnLookingForGroupEvent);

        recruitmentCriteriaController = new AddonController<AtkUnitBase>("LookingForGroupCondition");
        recruitmentCriteriaController.OnAttach += addon => {
            savePresetButton = new TextButtonNode {
                Position = new Vector2(406.0f, 605.0f),
                Size = new Vector2(160.0f, 28.0f),
                String = Strings.Button_SavePreset,
                TextTooltip = Strings.Tooltip_SavePreset,
                OnClick = savePresetWindow.Open,
            };
            savePresetButton.AttachNode(addon);
        };

        recruitmentCriteriaController.OnDetach += _ => {
            savePresetButton?.DetachNode();
            savePresetButton = null;
        };

        recruitmentCriteriaController.Enable();

        lookingForGroupController = new AddonController<AtkUnitBase>("LookingForGroup");

        lookingForGroupController.OnAttach += addon => {
            if (presetDropDown is not null) {
                presetDropDown?.Dispose();
                presetDropDown = null;
            }

            presetDropDown = new TextDropDownNode {
                Position = new Vector2(185.0f, 636.0f),
                Size = new Vector2(200.0f, 25.0f),
                MaxListOptions = 10,
                Options = PresetManager.GetPresetNames(),
            };
            UpdateDropDownOptions();

            presetDropDown.AttachNode(addon);
        };
        
        lookingForGroupController.OnDetach += _ => {
            presetDropDown?.Dispose();
            presetDropDown = null;
        };
        
        lookingForGroupController.Enable();
    }

    public override void OnDisable() {
        Services.AddonLifecycle.UnregisterListener(OnLookingForGroupEvent);

        recruitmentCriteriaController?.Dispose();
        recruitmentCriteriaController = null;
        
        lookingForGroupController?.Dispose();
        lookingForGroupController = null;
        
        savePresetWindow?.Dispose();
        savePresetWindow = null;
        
        presetEditorAddon?.Dispose();
        presetEditorAddon = null;

        presetDropDown?.Dispose();
        presetDropDown = null;
    }

    private void OnLookingForGroupEvent(AddonEvent type, AddonArgs args) {
        if (args is not AddonReceiveEventArgs eventArgs) return;
        if ((AtkEventType)eventArgs.AtkEventType is not AtkEventType.ButtonClick) return;
        if (eventArgs.EventParam is not 2) return;
        if (presetDropDown?.SelectedOption is not { } selectedOption) return;
        if (selectedOption == PresetManager.DefaultString) return;
        if (selectedOption == PresetManager.DontUseString) return;
        
        PresetManager.LoadPreset(selectedOption);
    }

    private void UpdateDropDownOptions() {
        if (presetDropDown is not null) {
            var presets = PresetManager.GetPresetNames();
            var anyPresets = presets.All(presetName => presetName != PresetManager.DefaultString);
            
            presetDropDown.Options = presets;
            presetDropDown.IsEnabled = anyPresets;

            presetDropDown.TextTooltip = anyPresets ? Strings.Tooltip_SelectPreset : Strings.Tooltip_NoPresets;
        }
    }

    private static List<string> GetPresetInfos() => PresetManager.GetPresetNames()
        .Where(name => name != PresetManager.DefaultString && name != PresetManager.DontUseString)
        .ToList();
}
