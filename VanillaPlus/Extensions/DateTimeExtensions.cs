using System;
using System.Collections.Generic;
using System.Linq;

namespace VanillaPlus.Extensions;

public static class DateTimeExtensions {
    private static readonly List<DateTime> SeasonalDates = [
        new(2000, 4, 1), // April Fools
    ];

    extension(DateTime dateTime) {
        public bool IsSeasonalEvent 
            => SeasonalDates.Any(date => date.DayOfYear == dateTime.DayOfYear);
    }
}
