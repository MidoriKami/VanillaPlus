using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using VanillaPlus.Utilities;

namespace VanillaPlus.Features.HUDPresets;

public static unsafe class HUDPresetManager {
    public const string DefaultOption = "No Option Selected";

    public static List<string> GetPresetNames() {
        var directory = GetPresetDirectory();

        var fileList = new List<string>();
        foreach (var file in directory.EnumerateFiles()) {
            var fileName = file.Name;
            if (!fileName.EndsWith(".layout.data")) continue;
            
            var rawName = fileName[..fileName.IndexOf(".layout.data", StringComparison.OrdinalIgnoreCase)];
            fileList.Add(rawName);
        }

        return fileList.Prepend(DefaultOption).ToList();
    }
    
    private static DirectoryInfo GetPresetDirectory() {
        var directoryInfo = new DirectoryInfo(Path.Combine(Data.DataPath, "HUDPresets"));
        if (!directoryInfo.Exists) {
            directoryInfo.Create();
        }

        return directoryInfo;
    }

    public static void SavePreset(string fileName) {
        ref var dataSet = ref AddonConfig.Instance()->ActiveDataSet;

        var layoutEntryCount = dataSet->HudLayoutConfigEntries.Length / 4;
        var layoutOffset = layoutEntryCount * dataSet->CurrentHudLayout;
        var layoutAddress = Unsafe.AsPointer(ref dataSet->HudLayoutConfigEntries[layoutOffset]);
        var layoutSize = sizeof(AddonConfigEntry) * layoutEntryCount;
        var layoutDataSpan = new Span<byte>(layoutAddress, layoutSize);

        Data.SaveBinaryData(layoutDataSpan.ToArray(), "HUDPresets", $"{fileName}.layout.data");
    }

    public static void LoadPreset(string fileName) {
        var addonConfig = AddonConfig.Instance();
        ref var dataSet = ref addonConfig->ActiveDataSet;
        var currentHudLayout = dataSet->CurrentHudLayout;

        var layoutEntryCount = dataSet->HudLayoutConfigEntries.Length / 4;
        var layoutOffset = layoutEntryCount * currentHudLayout;
        var layoutAddress = Unsafe.AsPointer(ref dataSet->HudLayoutConfigEntries[layoutOffset]);
        var layoutSize =  sizeof(AddonConfigEntry) * layoutEntryCount;
        
        var layoutData = Data.LoadBinaryData(layoutSize, "HUDPresets", $"{fileName}.layout.data");
        Marshal.Copy(layoutData, 0, (nint)layoutAddress, layoutSize);

        addonConfig->ApplyHudLayout();
        addonConfig->SaveFile(true);
    }

    public static void DeletePreset(string fileName) {
        var extrasFile = FileHelpers.GetFileInfo("Data", "HUDPresets", $"{fileName}.layout.data");

        if (extrasFile.Exists) {
            extrasFile.Delete();
        }
    }
}
