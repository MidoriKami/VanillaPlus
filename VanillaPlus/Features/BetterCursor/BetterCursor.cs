using System.Drawing;
using System.Threading.Tasks;
using Dalamud.Interface;
using KamiToolKit.Overlay.UiOverlay;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.NativeElements.Config;

namespace VanillaPlus.Features.BetterCursor;

public class BetterCursor : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_BetterCursor,
        Description = Strings.ModificationDescription_BetterCursor,
        Authors = ["MidoriKami"],
        Type = ModificationType.UserInterface,
    };

    private OverlayController? overlayController;

    private BetterCursorConfig? config;
    private ConfigAddon? configWindow;

    public override string ImageName => "BetterCursor.png";

    public override async Task OnEnableAsync() {
        config = await BetterCursorConfig.Load();

        configWindow = new ConfigAddon {
            InternalName = "BetterCursorConfig",
            Title = Strings.BetterCursor_ConfigTitle,
            Config = config,
        };

        configWindow.AddCategory(Strings.BetterCursor_CategoryStyle)
            .AddColorEdit(Strings.Color, nameof(config.Color), KnownColor.White.Vector())
            .AddInputFloat(Strings.BetterCursor_LabelSize, 16, 16..512, nameof(config.Size));

        configWindow.AddCategory(Strings.BetterCursor_CategoryFunctions)
            .AddCheckbox(Strings.BetterCursor_EnableAnimation, nameof(config.Animations))
            .AddCheckbox(Strings.BetterCursor_HideOnCameraMove, nameof(config.HideOnCameraMove));

        configWindow.AddCategory(Strings.Visibility)
            .AddCheckbox(Strings.BetterCursor_OnlyShowInCombat, nameof(config.OnlyShowInCombat))
            .AddCheckbox(Strings.BetterCursor_OnlyShowInDuties, nameof(config.OnlyShowInDuties));

        configWindow.AddCategory(Strings.BetterCursor_CategoryIconSelection)
            .AddSelectIcon(Strings.Icon, nameof(config.IconId));

        OpenConfigAction = configWindow.Toggle;

        overlayController = new OverlayController();

        await Services.Framework.Run(() => {
            overlayController.Initialize();

            overlayController.AddNode(new CursorImageNode {
                Config = config,
            });
        });
    }

    public override async Task OnDisableAsync() {
        if (overlayController is not null) {
            await overlayController.DisposeAsync();
            overlayController = null;
        }

        if (configWindow is not null) {
            await configWindow.DisposeAsync();
            configWindow = null;
        }

        config = null;
    }
}
