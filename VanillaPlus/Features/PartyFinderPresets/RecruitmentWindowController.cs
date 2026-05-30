using System;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using VanillaPlus.Features.PartyFinderPresets.Nodes;

namespace VanillaPlus.Features.PartyFinderPresets;

public unsafe class RecruitmentWindowController : IDisposable {
    private readonly PartyFinderPresetConfig config;

    private AddonController? addonController;
    private PresetControlNode? presetControlNode;

    public RecruitmentWindowController(PartyFinderPresetConfig config) {
        this.config = config;

        addonController = new AddonController {
            AddonName = "LookingForGroupCondition",
            OnSetup = OnAddonSetup,
            OnFinalize = OnAddonFinalize,
        };
        addonController.Enable(); // todo: make this a passthrough enable
    }

    public void Dispose() {
        addonController?.Dispose();
        addonController = null;
    }

    private void OnAddonSetup(AtkUnitBase* addon) {
        presetControlNode = new PresetControlNode {
            Position = new Vector2(472.0f, 472.0f),
            Size = new Vector2(426.0f, 122.0f),
            PresetEntry = GetSelectedPresetEntry(),
            Config = config,
        };
        presetControlNode.AttachNode(addon);
    }

    private void OnAddonFinalize(AtkUnitBase* addon) {
        presetControlNode?.Dispose();
        presetControlNode = null;
    }

    private PresetEntry? GetSelectedPresetEntry() {
        var selectedOption = MainWindowController.PresetDropDownNode?.SelectedOption;
        if (selectedOption is null) return null;
        if (selectedOption == Strings.Preset_DontUseOption) return null;

        return config.Presets.FirstOrDefault(preset => preset.Name == selectedOption);
    }
}
