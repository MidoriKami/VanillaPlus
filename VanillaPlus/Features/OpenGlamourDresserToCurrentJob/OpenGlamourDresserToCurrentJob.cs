using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
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

    private void OnGlamourDresserSetup(AddonEvent type, AddonArgs args) {
        if (IObjectTable.Get() is { LocalPlayer.ClassJob.RowId: var playerJob }) {
            Marshal.WriteByte(args.Addon, 0x1A8, (byte)playerJob);
        }
    }
}
