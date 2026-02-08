using System;

namespace VanillaPlus.Extensions;

public static class RangeExtensions {
    extension(Range range) {
        public bool Contains(int value) => value >= range.Start.Value && value <= range.End.Value;
        
        public int Length => range.End.Value - range.Start.Value;
    }
}
