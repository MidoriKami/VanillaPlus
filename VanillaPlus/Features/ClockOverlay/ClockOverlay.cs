using System.Collections.Generic;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Overlay;
using VanillaPlus.Classes;
using VanillaPlus.NativeElements.Config;
using VanillaPlus.NativeElements.Config.NodeEntries;
using VanillaPlus.Utilities;

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
    private ClockNode? clockNode;

    private ConfigAddon? configWindow;
    private TextNodeStyle? clockStyle;

    public override void OnEnable() {
        config = ClockOverlayConfig.Load();
        overlayController = new OverlayController();

        const string stylePath = "ClockOverlay.style.json";
        var defaultStyle = new TextNodeStyle {
            FontSize = 20,
            TextColor = ColorHelper.GetColor(1),
            TextOutlineColor = ColorHelper.GetColor(54),
            Position = config.Position,
        };

        clockStyle = Config.LoadConfig(stylePath, defaultStyle);

        if (clockStyle.Position == Vector2.Zero && config.Position != Vector2.Zero) {
            clockStyle.Position = config.Position;
        }

        clockStyle.StyleChanged = () => {
            clockStyle.Save(stylePath);
            clockNode?.Position = clockStyle.Position;
        };

        configWindow = new ConfigAddon {
            InternalName = "ClockOverlayConfig",
            Title = Strings.ClockOverlay_ConfigurationTitle,
            Config = config,
        };

        configWindow.AddCategory(Strings.ClockOverlay_ClockSettings)
            .AddCheckbox(Strings.ClockOverlay_ShowSeconds, nameof(config.ShowSeconds))
            .AddCheckbox(Strings.ClockOverlay_ShowSourcePrefix, nameof(config.ShowPrefix))
            .AddCheckbox(Strings.ClockOverlay_EnableMoving, nameof(config.IsMoveable))
            .AddDropdown(Strings.ClockOverlay_TimeSource, nameof(config.Type), new Dictionary<string, object> {
                [Strings.ClockOverlay_LocalTime] = ClockType.Local,
                [Strings.ClockOverlay_ServerTime] = ClockType.Server,
                [Strings.ClockOverlay_EorzeaTime] = ClockType.Eorzea,
            })
            .AddDropdown(Strings.ClockOverlay_TextRendering, nameof(config.Flags), new Dictionary<string, object> {
                [Strings.ClockOverlay_Edge] = TextFlags.Edge,
                [Strings.ClockOverlay_Glare] = TextFlags.Glare,
                [Strings.ClockOverlay_Emboss] = TextFlags.Emboss,
            });

        configWindow.AddCategory(Strings.ClockOverlay_Visual_Style)
            .AddNodeConfig(clockStyle);

        OpenConfigAction = configWindow.Toggle;

        overlayController.CreateNode(() => {
            clockNode = new ClockNode(config, clockStyle) {
                Size = new Vector2(150.0f, 30.0f),
                Position = clockStyle.Position,
            };

            clockNode.OnMoveComplete = () => {
                config.Position = clockNode.Position;
                clockStyle.Position = clockNode.Position;

                config.Save();
                clockStyle.Save(stylePath);
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
