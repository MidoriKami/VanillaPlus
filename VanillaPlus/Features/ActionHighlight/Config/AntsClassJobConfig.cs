using System.Collections.Generic;

namespace VanillaPlus.Features.ActionHighlight.Config;

/// <summary>
/// Data object representing a config entry for a specific ClassJob.
/// </summary>
public class AntsClassJobConfig {

    /// <summary>
    /// Gets or sets the ClassJob id for this config.
    /// </summary>
    public uint ClassJobId { get; set; }

    /// <summary>
    /// Gets or sets the list of ActionSettings.
    /// </summary>
    public List<AntsActionSetting> ActionSettings { get; set; } = [];
}
