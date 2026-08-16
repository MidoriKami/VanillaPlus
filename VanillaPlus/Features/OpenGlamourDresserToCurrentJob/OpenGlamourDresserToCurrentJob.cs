using System.Threading.Tasks;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.OpenGlamourDresserToCurrentJob;

public class OpenGlamourDresserToCurrentJob : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_OpenGlamourDresserToCurrentJob,
        Description = Strings.ModificationDescription_OpenGlamourDresserToCurrentJob,
        Type = ModificationType.GameBehavior,
        Authors = ["MidoriKami"],
        CompatibilityModule = new SimpleTweaksCompatibilityModule("UiAdjustments@OpenGlamourDresserToCurrentJob"),
    };

    public override Task OnEnableAsync() {
        IAddonLifecycle.Get().RegisterListener(AddonEvent.PreSetup, "MiragePrismPrismBox", OnGlamourDresserSetup);

        return Task.CompletedTask;
    }

    public override Task OnDisableAsync() {
        IAddonLifecycle.Get().UnregisterListener(OnGlamourDresserSetup);

        return Task.CompletedTask;
    }

    private static unsafe void OnGlamourDresserSetup(AddonEvent type, AddonArgs args) {
        if (IObjectTable.Get() is { LocalPlayer.ClassJob.RowId: var playerJob }) {
            args.GetAddon<AddonMiragePrismPrismBox>()->Param = (int) playerJob;
        }
    }
}
