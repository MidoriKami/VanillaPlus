using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Overlay;
using VanillaPlus.Classes;
using VanillaPlus.NativeElements.Config;

namespace VanillaPlus.Features.ClockOverlay;

public class ClockOverlay : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ClockOverlay_Title,
        Description = Strings.ClockOverlay_Description,
        Type = ModificationType.NewOverlay,
        Authors = [ "Zeffuro" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    private ClockOverlayConfig? config;
    private OverlayController? overlayController;
    private ClockOverlayNode? clockNode;

    private ConfigAddon? configWindow;

    public override void OnEnable() {
        config = ClockOverlayConfig.Load();
        overlayController = new OverlayController();

        configWindow = new ConfigAddon {
            InternalName = "ClockOverlayConfig",
            Title = Strings.ClockOverlay_ConfigurationTitle,
            Config = config,
        };

        configWindow.AddCategory(Strings.ClockOverlay_ClockSettings)
            .AddCheckbox(Strings.ClockOverlay_ShowSeconds, nameof(config.ShowSeconds))
            .AddCheckbox(Strings.ClockOverlay_ShowSourcePrefix, nameof(config.ShowPrefix))
            .AddCheckbox(Strings.ClockOverlay_EnableMoving, nameof(config.IsMoveable))
            .AddDropdown<ClockType>(Strings.ClockOverlay_TimeSource, nameof(config.Type));

        configWindow.AddCategory("Visual Style")
            .AddColorEdit("Text Color", nameof(config.TextColor))
            .AddColorEdit("Text Outline", nameof(config.TextOutlineColor))
            .AddIntSlider("Font Size", 8, 32, nameof(config.FontSize))
            .AddDropdown<FontType>("Font", nameof(config.FontType))
            .AddDropdown<AlignmentType>("Alignment", nameof(config.AlignmentType))
            .AddDropdown<TextFlags>(Strings.ClockOverlay_TextRendering, nameof(config.TextFlags));
        
        OpenConfigAction = configWindow.Toggle;

        overlayController.CreateNode(() => {
            clockNode = new ClockOverlayNode(config) {
                Size = new Vector2(150.0f, 30.0f),
                Position = config.Position,
            };

            clockNode.OnMoveComplete = () => {
                config.Position = clockNode.Position;
                config.Save();
            };
            
            return clockNode;
        });
    }

    public override void OnDisable() {
        configWindow?.Dispose();
        configWindow = null;
        
        overlayController?.Dispose();
        overlayController = null;

        clockNode?.Dispose();
        clockNode = null;

        config = null;
    }
}
