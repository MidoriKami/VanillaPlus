using System;

namespace VanillaPlus.Extensions;

public static class ArrayExtensions {
    extension(Array) {
        public static T[] CreateInitialized<T>(int n) where T : new() {
            var ret = new T[n];

            for (var i = 0; i < n; ++i) {
                ret[i] = new T();
            }

            return ret;
        }
    }
}
