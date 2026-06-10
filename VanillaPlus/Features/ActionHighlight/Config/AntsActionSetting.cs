
namespace VanillaPlus.Features.ActionHighlight.Config;

/// <summary>
/// Data object representing a specific action's settings.
/// </summary>
public class AntsActionSetting {

    /// <summary>
    /// Gets or sets this actions ID.
    /// </summary>
    public uint ActionId { get; set; }

    /// <summary>
    /// Gets or sets whether this action should be modified.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the threshold value for this action.
    /// </summary>
    public int ThresholdMs { get; set; } = 3000;
}
