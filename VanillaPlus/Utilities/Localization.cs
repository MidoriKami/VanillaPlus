using System.Globalization;
using System.Resources;

namespace VanillaPlus.Utilities;

public static class Localization {
    private static readonly ResourceManager ResourceManager = new("VanillaPlus.Resources.Strings", typeof(Localization).Assembly);
    private static CultureInfo? overrideCulture;

    public static CultureInfo CurrentCulture => overrideCulture ?? MapLanguageToCulture(Services.PluginInterface.UiLanguage);

    public static void SetOverrideCulture(CultureInfo? culture) => overrideCulture = culture;

    public static string GetString(string resourceKey, params object[] formatArguments) {
        if (string.IsNullOrEmpty(resourceKey)) {
            return string.Empty;
        }

        var value = ResourceManager.GetString(resourceKey, CurrentCulture)
                    ?? ResourceManager.GetString(resourceKey, CultureInfo.InvariantCulture)
                    ?? resourceKey;

        return formatArguments.Length > 0
            ? string.Format(CurrentCulture, value, formatArguments)
            : value;
    }

    public static string Strings(string resourceKey, params object[] formatArguments)
        => GetString(resourceKey, formatArguments);

    private static CultureInfo MapLanguageToCulture(object? language) {
        var languageName = language?.ToString();

        return languageName switch {
            "ja" => CultureInfo.GetCultureInfo("ja-JP"),
            "zh" => CultureInfo.GetCultureInfo("zh-CN"),
            // "de" => CultureInfo.GetCultureInfo("de-DE"),
            // "fr" => CultureInfo.GetCultureInfo("fr-FR"),
            _ => CultureInfo.GetCultureInfo("en-US"),
        };
    }
}
