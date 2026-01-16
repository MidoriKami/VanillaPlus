using System;
using System.ComponentModel;

namespace VanillaPlus.Extensions;

public static class EnumExtensions {
    extension(Enum enumValue) {
        public string Description => enumValue.GetDescription();

        private string GetDescription() {
            var attribute = enumValue.GetAttribute<DescriptionAttribute>();
            return Strings.ResourceManager.GetString(attribute?.Description ?? string.Empty, Strings.Culture) ?? enumValue.ToString();
        }
    }
    
    public static T ParseAsEnum<T>(this string stringValue, T defaultValue) where T : Enum {
        foreach (Enum enumValue in Enum.GetValues(typeof(T))) {
            if (enumValue.Description == stringValue) {
                return (T)enumValue;
            }
        }

        return defaultValue;
    }
}
