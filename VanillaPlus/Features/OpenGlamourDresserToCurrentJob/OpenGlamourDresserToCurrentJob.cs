using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
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

    public override async Task OnEnableAsync() {
        await Services.Framework.Run(() => {
            Services.AddonLifecycle.RegisterListener(AddonEvent.PreSetup, "MiragePrismPrismBox", OnGlamourDresserSetup);
        });
    }

    public override async Task OnDisableAsync() {
        await Services.Framework.Run(() => {
            Services.AddonLifecycle.UnregisterListener(OnGlamourDresserSetup);
        });
    }

    private void OnGlamourDresserSetup(AddonEvent type, AddonArgs args) {
        if (Services.ObjectTable is { LocalPlayer.ClassJob.RowId: var playerJob }) {
            Marshal.WriteByte(args.Addon, 0x1A8, (byte)playerJob);
        }
    }
}
