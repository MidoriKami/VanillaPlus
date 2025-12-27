using System.Globalization;

namespace VanillaPlus.Utilities;

public static class Localization {
    public static void SetCultureInfo(object? language) {
        var languageName = language?.ToString();

        Strings.Culture = languageName switch {
            "ja" => CultureInfo.GetCultureInfo("ja-JP"),
            "zh" => CultureInfo.GetCultureInfo("zh-CN"),
            "de" => CultureInfo.GetCultureInfo("de-DE"),
            "fr" => CultureInfo.GetCultureInfo("fr-FR"),
            _ => CultureInfo.GetCultureInfo("en-US"),
        };
    }
}
