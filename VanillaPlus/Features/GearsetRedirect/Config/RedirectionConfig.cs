namespace VanillaPlus.Features.GearsetRedirect.Config;

/// <summary>
/// Data object representing a singular specific redirection target.
/// Defines which gearset to switch to, if we are currently in <see cref="TerritoryType"/>.
/// </summary>
public class RedirectionConfig {
    public int AlternateGearsetId { get; set; }
    public uint TerritoryType { get; set; }
}
