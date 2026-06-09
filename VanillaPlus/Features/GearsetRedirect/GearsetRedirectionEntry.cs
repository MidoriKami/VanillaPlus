using System.Collections.Generic;
using VanillaPlus.Features.GearsetRedirect.Nodes;

namespace VanillaPlus.Features.GearsetRedirect;

/// <summary>
/// Configuration definition for use with <see cref="GearsetRedirect"/>.
/// </summary>
public class GearsetRedirectionEntry {

    /// <summary>
    /// Gets or sets the gearset id used for this entry.
    /// </summary>
    public int TargetGearsetId { get; set; }

    /// <summary>
    /// Gets or sets the redirections used for this gearset entry.
    /// </summary>
    public List<RedirectionConfig> Redirections { get; set; } = [];
}
