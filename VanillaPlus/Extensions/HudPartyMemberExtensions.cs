using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;

namespace VanillaPlus.Extensions;

public record HealthValues(int Current, int Max);

public static class HudPartyMemberExtensions {
    public static unsafe HealthValues? GetHealth(this HudPartyMember hudMember) {
        if (hudMember.Object is null) return null;

        return new HealthValues((int)hudMember.Object->Health, (int)hudMember.Object->MaxHealth);
    }

    public static unsafe ClassJob? GetClassJob(this HudPartyMember hudMember) {
        if (hudMember.Object is null) return null;

        return Services.DataManager.GetClassJobById(hudMember.Object->ClassJob);
    }
}
