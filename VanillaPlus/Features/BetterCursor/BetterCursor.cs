using System.Drawing;
using Dalamud.Interface;
using KamiToolKit.Overlay;
using VanillaPlus.Classes;
using VanillaPlus.NativeElements.Config;

namespace VanillaPlus.Features.BetterCursor;

public class BetterCursor : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings("ModificationDisplay_BetterCursor"),
        Description = Strings("ModificationDescription_BetterCursor"),
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
            Title = Strings("BetterCursor_ConfigTitle"),
            Config = config,
        };

        configWindow.AddCategory(Strings("BetterCursor_CategoryStyle"))
            .AddColorEdit(Strings("BetterCursor_LabelColor"), nameof(config.Color), KnownColor.White.Vector())
            .AddInputFloat(Strings("BetterCursor_LabelSize"), 16, 16..512, nameof(config.Size));

        configWindow.AddCategory(Strings("BetterCursor_CategoryFunctions"))
            .AddCheckbox(Strings("BetterCursor_EnableAnimation"), nameof(config.Animations))
            .AddCheckbox(Strings("BetterCursor_HideOnCameraMove"), nameof(config.HideOnCameraMove));
        
        configWindow.AddCategory(Strings("BetterCursor_CategoryVisibility"))
            .AddCheckbox(Strings("BetterCursor_OnlyShowInCombat"), nameof(config.OnlyShowInCombat))
            .AddCheckbox(Strings("BetterCursor_OnlyShowInDuties"), nameof(config.OnlyShowInDuties));

        configWindow.AddCategory(Strings("BetterCursor_CategoryIconSelection"))
            .AddSelectIcon(Strings("BetterCursor_LabelIcon"), nameof(config.IconId));

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
