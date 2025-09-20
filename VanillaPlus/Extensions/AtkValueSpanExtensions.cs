﻿using System;
using System.Linq;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace VanillaPlus.Extensions;

public static class AtkValueSpanExtensions {
    public static void PrintValues(this Span<AtkValue> values, int indentSpaces = 0) {
        foreach (var index in Enumerable.Range(0, values.Length)) {
            ref var value = ref values[index];

            Services.PluginLog.Debug($"{new string(' ', indentSpaces)}[{index}] [{value.Type}] {value.GetValueAsString()}");
        }
    }
}
