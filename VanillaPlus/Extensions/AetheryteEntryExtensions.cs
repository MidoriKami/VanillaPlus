using System.Text.RegularExpressions;
using Dalamud.Game.ClientState.Aetherytes;
using Dalamud.Game.Text;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Text.ReadOnly;

namespace VanillaPlus.Extensions;

public static unsafe class AetheryteEntryExtensions {
    extension(IAetheryteEntry entry) {
        public ReadOnlySeString AetheryteName
            => entry.AetheryteData.ValueNullable?.PlaceName.ValueNullable?.Name ?? "Unable to read Name";

        public ReadOnlySeString PlaceName
            => entry.AetheryteData.ValueNullable?.Map.ValueNullable?.PlaceName.ValueNullable?.Name ?? "Unable to read PlaceName";

        public ReadOnlySeString RegionName
            => entry.AetheryteData.ValueNullable?.Territory.ValueNullable?.PlaceNameRegion.Value.Name ?? "Unable to Parse Name.";

        public uint RegionId
            => entry.AetheryteData.ValueNullable?.Territory.ValueNullable?.PlaceNameRegion.RowId ?? 0;

        public string GilCostString
            => $"{entry.GilCost}{SeIconChar.Gil.ToIconString()}";

        public string TerritoryIdString
            => entry.AetheryteData.ValueNullable?.Territory.ValueNullable?.Name.ToString() ?? "f1f1";

        public string MapTexturePath
            => entry.AetheryteData.ValueNullable is { Map.ValueNullable.Id: var mapId }
                   ? $"ui/map/{mapId}/{mapId.ToString().Replace("/", string.Empty)}_m.tex" : string.Empty;

        /// <summary>
        /// Performs teleport then closes any active "Teleport" window.
        /// </summary>
        public void Teleport() {
            Telepo.Instance()->Teleport(entry.AetheryteId, 0);
            AgentTeleport.Instance()->Hide();
        }

        public bool IsMatch(ReadOnlySeString searchString) {
            var regex = new Regex(searchString.ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            if (regex.IsMatch(entry.AetheryteName.ToString())) return true;
            if (regex.IsMatch(entry.PlaceName.ToString())) return true;

            return false;
        }
    }
}
