using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Overlay.UiOverlay;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.NativeElements.Config;

namespace VanillaPlus.Features.ClockOverlay;

public class ClockOverlay : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ClockOverlay_Title,
        Description = Strings.ClockOverlay_Description,
        Type = ModificationType.NewOverlay,
        Authors = [ "Zeffuro" ],
    };

    public override string ImageName => "ClockOverlay.png";

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

        configWindow.AddCategory(Strings.ClockOverlay_CategoryVisualStyle)
            .AddColorEdit(Strings.ClockOverlay_LabelTextColor, nameof(config.TextColor))
            .AddColorEdit(Strings.ClockOverlay_LabelTextOutline, nameof(config.TextOutlineColor))
            .AddIntSlider(Strings.ClockOverlay_LabelFontSize, 8, 32, nameof(config.FontSize))
            .AddDropdown<FontType>(Strings.ClockOverlay_LabelFont, nameof(config.FontType))
            .AddDropdown<AlignmentType>(Strings.ClockOverlay_LabelAlignment, nameof(config.AlignmentType))
            .AddDropdown<TextFlags>(Strings.ClockOverlay_TextRendering, nameof(config.TextFlags));
        
        OpenConfigAction = configWindow.Toggle;

        overlayController.CreateNode(() => {
            clockNode = new ClockOverlayNode(config) {
                Size = new Vector2(150.0f, 30.0f),
                Position = config.Position,
                OnMoveComplete = thisNode => {
                    config.Position = thisNode.Position;
                    config.Save();
                },
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
