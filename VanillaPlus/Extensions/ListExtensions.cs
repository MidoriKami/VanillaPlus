using System.Collections.Generic;

namespace VanillaPlus.Extensions;

public static class ListExtensions {
    extension<T>(List<T>) where T : new() {
        public static List<T> CreateInitialized(int n) {
            var ret = new List<T>();

            for (var i = 0; i < n; ++i) {
                ret.Add( new T() );
            }

            return ret;
        }
    }
}
