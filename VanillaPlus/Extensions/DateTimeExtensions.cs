using System;

namespace VanillaPlus.Extensions;

public static class DateTimeExtensions {
    extension(DateTime dateTime) {
        public bool IsSeasonalEvent => dateTime switch {
            { Month: 4, Day: 1 } => true, // April Fools
            _ => false,
        };
    }
}
