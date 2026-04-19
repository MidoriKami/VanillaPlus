using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.NativeElements.Config;

namespace VanillaPlus.Features.ConfigSearchBar;

public unsafe class SystemConfigSearchBar : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_SystemConfigSearchBar,
        Description = Strings.ModificationDescription_SystemConfigSearchBar,
        Type = ModificationType.UserInterface,
        Authors = [ "MidoriKami" ],
    };

    public override string ImageName => "SystemConfigSearchBar.png";

    private AddonController? systemConfigController;
    private TextInputNode? systemConfigInput;
    private List<TabEntry>? systemConfigTabs;

    private ConfigSearchBarConfig? config;
    private ConfigAddon? configAddon;

    public override void OnEnable() {
        config = ConfigSearchBarConfig.Load();

        configAddon = new ConfigAddon {
            Title = Strings.SystemConfigSearchBar_ConfigTitle,
            InternalName = "ConfigSearchBarConfig",
            Config = config,
        };

        configAddon.AddCategory(Strings.SystemConfigSearchBar_CategoryGeneral)
            .AddColorEdit(Strings.SystemConfigSearchBar_LabelTabHighlight, nameof(config.TabColor), KnownColor.LimeGreen.Vector())
            .AddColorEdit(Strings.SystemConfigSearchBar_LabelTextHighlight, nameof(config.HighlightColor), KnownColor.Red.Vector());

        OpenConfigAction = configAddon.Toggle;

        systemConfigController = new AddonController {
            AddonName = "ConfigSystem",
            OnSetup = SetupConfigSystem,
            OnFinalize = FinalizeConfigSystem,
        };
        systemConfigController.Enable();
    }

    public override void OnDisable() {
        configAddon?.Dispose();
        configAddon = null;
        
        config = null;
        
        systemConfigController?.Dispose();
        systemConfigController = null;
    }

    private void SetupConfigSystem(AtkUnitBase* addon) {
        if (config is null) return;
        
        systemConfigTabs = [
            new TabEntry(addon, 7, 16, config),
            new TabEntry(addon, 8, 88, config),
            new TabEntry(addon, 9, 280, config),
            new TabEntry(addon, 10, 444, config),
            new TabEntry(addon, 11, 464, config),
            new TabEntry(addon, 12, 495, config),
            new TabEntry(addon, 13, 506, config),
            new TabEntry(addon, 14, 553, config),
        ];

        var size = new Vector2(addon->Size.X / 2.0f, 28.0f);

        var headerSize = new Vector2(addon->WindowHeaderCollisionNode->Width, addon->WindowHeaderCollisionNode->Height);
        systemConfigInput = new TextInputNode {
            Position = headerSize / 2.0f - size / 2.0f + new Vector2(25.0f, 5.0f), 
            Size = size, 
            OnInputReceived = searchString => {
                foreach (var entry in systemConfigTabs) {
                    entry.TryMatchString(searchString.ToString());
                }
            }, 
            PlaceholderString = Strings.SearchPlaceholder,
        };

        systemConfigInput.AttachNode(addon);
    }

    private void FinalizeConfigSystem(AtkUnitBase* _) {
        foreach (var entry in systemConfigTabs ?? []) {
            entry.Dispose();
        }
        systemConfigTabs?.Clear();
        systemConfigTabs = null;
        
        systemConfigInput?.Dispose();
        systemConfigInput = null;
    }
}
