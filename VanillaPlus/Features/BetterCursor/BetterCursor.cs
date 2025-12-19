using System.Drawing;
using Dalamud.Interface;
using KamiToolKit.Overlay;
using VanillaPlus.Classes;
using VanillaPlus.NativeElements.Config;

namespace VanillaPlus.Features.BetterCursor;

public class BetterCursor : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Better Cursor",
        Description = "Draws a ring around the cursor to make it easier to see.",
        Authors = ["MidoriKami"],
        Type = ModificationType.UserInterface,
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
            new ChangeLogInfo(2, "Reduced Animation Speed to 1Hz"),
            new ChangeLogInfo(3, "Added options to only show in duties and/or combat"),
        ],
    };

    private OverlayController? overlayController;

    private BetterCursorConfig? config;
    private ConfigAddon? configWindow;

    public override string ImageName => "BetterCursor.png";

    public override void OnEnable() {
        config = BetterCursorConfig.Load();

        configWindow = new ConfigAddon {
            InternalName = "BetterCursorConfig",
            Title = "Better Cursor Config",
            Config = config,
        };

        configWindow.AddCategory("Style")
            .AddColorEdit("Color", nameof(config.Color), KnownColor.White.Vector())
            .AddInputFloat("Size", 16, 16..512, nameof(config.Size));

        configWindow.AddCategory("Functions")
            .AddCheckbox("Enable Animation", nameof(config.Animations))
            .AddCheckbox("Hide on Left-Hold or Right-Hold", nameof(config.HideOnCameraMove));
        
        configWindow.AddCategory("Visibility")
            .AddCheckbox("Only show in Combat", nameof(config.OnlyShowInCombat))
            .AddCheckbox("Only Show in Duties", nameof(config.OnlyShowInDuties));

        configWindow.AddCategory("Icon Selection")
            .AddSelectIcon("Icon", nameof(config.IconId));

        OpenConfigAction = configWindow.Toggle;

        overlayController = new OverlayController();
        overlayController.CreateNode(() => new CursorImageNode {
            Config = config,
        });
    }

    public override void OnDisable() {
        overlayController?.Dispose();
        overlayController = null;
        
        configWindow?.Dispose();
        configWindow = null;
        
        config = null;
    }
}
