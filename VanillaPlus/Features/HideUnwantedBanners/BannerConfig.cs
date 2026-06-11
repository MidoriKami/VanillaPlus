namespace VanillaPlus.Features.HideUnwantedBanners;

/// <summary>
/// Data object representing a single banner that may or may not be suppressed.
/// For use with <see cref="HideUnwantedBanners"/>.
/// </summary>
public class BannerConfig {

    /// <summary>
    /// Gets the banner icon id for this entry.
    /// </summary>
    public int BannerId { get; set; }

    /// <summary>
    /// Gets whether this banner should be hidden.
    /// </summary>
    public bool IsSuppressed { get; set; }
}
