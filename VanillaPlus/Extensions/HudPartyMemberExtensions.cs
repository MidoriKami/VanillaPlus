using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;

namespace VanillaPlus.Extensions;

public record HealthValues(int Current, int Max);

public static class HudPartyMemberExtensions {
    extension(HudPartyMember hudMember) {
        public HealthValues? HealthValues => hudMember.GetHealth();
        public ClassJob? ClassJob => hudMember.GetClassJob();
        
        private unsafe HealthValues? GetHealth() {
            if (hudMember.Object is null) return null;

            return new HealthValues((int)hudMember.Object->Health, (int)hudMember.Object->MaxHealth);
        }

        private unsafe ClassJob? GetClassJob() {
            if (hudMember.Object is null) return null;

            return Services.DataManager.GetClassJobById(hudMember.Object->ClassJob);
        }
    }
}
