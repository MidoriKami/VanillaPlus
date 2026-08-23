using System.Threading.Tasks;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Extensions;

namespace VanillaPlus.Features.BetterEmoteWindow;

public class BetterEmoteWindow : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_BetterEmoteWindow,
        Description = Strings.ModificationDescription_BetterEmoteWindow,
        Type = ModificationType.UserInterface,
        Authors = ["MapleRecall"],
    };

    private EmoteWindowLayoutController? layoutController;
    private EmoteTooltipController? tooltipController;

    public override async Task OnEnableAsync() {
        layoutController = new EmoteWindowLayoutController();
        await layoutController.EnableAsync();

        tooltipController = new EmoteTooltipController();
        tooltipController.Enable();
    }

    public override async Task OnDisableAsync() {
        await tooltipController.DisposeAsyncSafe();
        tooltipController = null;

        await layoutController.DisposeAsyncSafe();
        layoutController = null;
    }
}
