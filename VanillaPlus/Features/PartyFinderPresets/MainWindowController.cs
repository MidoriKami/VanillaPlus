using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Addon.Events;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using KamiToolKit.Nodes;

namespace VanillaPlus.Features.PartyFinderPresets;

public unsafe class MainWindowController : IDisposable {
    private readonly PartyFinderPresetConfig config;

    private AddonController<AddonLookingForGroup>? mainWindowController;
    internal static TextDropDownNode? PresetDropDownNode;

    public MainWindowController(PartyFinderPresetConfig config) {
        this.config = config;
        mainWindowController = new AddonController<AddonLookingForGroup> {
            AddonName = "LookingForGroup",
            OnSetup = OnAddonSetup,
            OnFinalize = OnAddonFinalize,
        };
        mainWindowController.Enable(); // todo: make this a passthrough enable

        Services.AddonLifecycle.RegisterListener(AddonEvent.PreReceiveEvent, "LookingForGroup", OnLookingForGroupEvent);

        config.OnSave += UpdatePresets;
    }

    public void Dispose() {
        Services.AddonLifecycle.UnregisterListener(OnLookingForGroupEvent);

        mainWindowController?.Dispose();
        mainWindowController = null;

        config.OnSave -= UpdatePresets;
    }

    private void OnAddonSetup(AddonLookingForGroup* addon) {
        PresetDropDownNode?.Dispose();
        PresetDropDownNode = new TextDropDownNode {
            Position = new Vector2(185.0f, 636.0f),
            Size = new Vector2(200.0f, 25.0f),
            MaxListOptions = 10,
            Options = GetPresetDropDownOptions(),
            TextTooltip = "[VanillaPlus] Party Finder Presets",
            PlaceholderString = "Select a Preset",
        };
        PresetDropDownNode.AttachNode((AtkUnitBase*)addon);
    }

    private void OnAddonFinalize(AddonLookingForGroup* addon) {
        PresetDropDownNode?.Dispose();
        PresetDropDownNode = null;
    }

    private void UpdatePresets() {
        PresetDropDownNode?.Options = GetPresetDropDownOptions();
    }

    private List<string> GetPresetDropDownOptions()
        => config.Presets.Select(preset => preset.Name).Prepend(Strings.Preset_DontUseOption).ToList();

    private void OnLookingForGroupEvent(AddonEvent type, AddonArgs args) {
        // If we are not opening the Recruitment Window, exit.
        if (args is not AddonReceiveEventArgs { AtkEventType: AddonEventType.ButtonClick, EventParam: 2 }) return;

        GetSelectedPresetEntry()?.ApplyPreset();
    }

    private PresetEntry? GetSelectedPresetEntry() {
        var selectedOption = PresetDropDownNode?.SelectedOption;
        if (selectedOption is null) return null;
        if (selectedOption == Strings.Preset_DontUseOption) return null;

        return config.Presets.FirstOrDefault(preset => preset.Name == selectedOption);
    }
}
