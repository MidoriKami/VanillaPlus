using System.Linq;
using Dalamud.Game.ClientState.Aetherytes;
using Dalamud.Game.Text;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Text.ReadOnly;

namespace VanillaPlus.Extensions;

public static unsafe class AetheryteEntryExtensions {
    extension(IAetheryteEntry entry) {
        public ReadOnlySeString AetheryteName => entry switch {
            { IsSharedHouse: true } => Services.SeStringEvaluator.EvaluateFromAddon(6724, [
                entry.AetheryteData.Value.PlaceName.RowId,
                (uint)entry.Ward,
                (uint)entry.Plot,
            ]),
            { IsApartment: true } => Services.DataManager.GetAddonText(6710),
            _ => entry.AetheryteData.ValueNullable?.PlaceName.ValueNullable?.Name ?? "Unable to read Name",
        };

        public ReadOnlySeString PlaceName
            => entry.AetheryteData.ValueNullable?.Map.ValueNullable?.PlaceName.ValueNullable?.Name ?? "Unable to read PlaceName";

        public ReadOnlySeString RegionName
            => entry.AetheryteData.ValueNullable?.Territory.ValueNullable?.PlaceNameRegion.Value.Name ?? "Unable to Parse Name.";

        public uint RegionId
            => entry.AetheryteData.ValueNullable?.Territory.ValueNullable?.PlaceNameRegion.RowId ?? 0;

        public string GilCostString
            => $"{entry.GilCost}{SeIconChar.Gil.ToIconString()}";

        public int EntryIndex
            => Services.AetheryteList.ToList().IndexOf(entry);

        public string MapTexturePath
            => entry.AetheryteData.ValueNullable is { Map.ValueNullable.Id: var mapId }
                   ? $"ui/map/{mapId}/{mapId.ToString().Replace("/", string.Empty)}_m.tex" : string.Empty;

        /// <summary>
        /// Performs teleport then closes any active "Teleport" window.
        /// </summary>
        public void Teleport() {
            Telepo.Instance()->Teleport(entry.AetheryteId, entry.SubIndex);
            AgentTeleport.Instance()->Hide();
        }
    }
}
