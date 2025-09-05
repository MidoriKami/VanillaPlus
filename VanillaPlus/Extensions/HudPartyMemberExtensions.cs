using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;

namespace VanillaPlus.Extensions;

public static class HudPartyMemberExtensions
{
    public record HealthValues(int Current, int Max);

    public static unsafe HealthValues GetHealth(this HudPartyMember hudMember)
        => hudMember.Object != null
               ? new HealthValues((int)hudMember.Object->Health, (int)hudMember.Object->MaxHealth)
               : new HealthValues(0, 0);

    public static unsafe ClassJob GetClassJob(this HudPartyMember hudMember)
        => hudMember.Object != null
               ? Services.DataManager.GetClassJobById(hudMember.Object->ClassJob)
               : new ClassJob();
}
