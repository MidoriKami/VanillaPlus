using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using VanillaPlus.Utilities;

namespace VanillaPlus.Features.PartyFinderPresets;

public static unsafe class PresetManager {
    public const string DefaultString = "No Presets Saved";
    public const string DontUseString = "Don't Use Preset";

    public static List<string> GetPresetNames() {
        var directory = GetPresetDirectory();

        var fileList = new List<string>();
        foreach (var file in directory.EnumerateFiles()) {
            var fileName = file.Name;
            if (!fileName.EndsWith(".preset.data")) continue;
            
            var rawName = fileName[..fileName.IndexOf(".preset.data", StringComparison.OrdinalIgnoreCase)];
            fileList.Add(rawName);
        }
        
        return fileList.Count is 0 ? [ DefaultString ] : fileList.Prepend(DontUseString).ToList();
    }

    public static void LoadPreset(string fileName) {
        var address = Unsafe.AsPointer(ref AgentLookingForGroup.Instance()->StoredRecruitmentInfo);
        var size = sizeof(AgentLookingForGroup.RecruitmentSub);

        var dataFilePath = GetDataFileInfo(fileName).FullName;
        var result = File.ReadAllBytes(dataFilePath);

        if (result.Length < size) {
            Services.PluginLog.Debug("Not data to load, creating new file.");
            result = new byte[size];
            FilesystemUtil.WriteAllBytesSafe(dataFilePath, result);
        }

        Marshal.Copy(result, 0, (nint)address, size);
    }

    public static void SavePreset(string fileName) {
        var address = Unsafe.AsPointer(ref AgentLookingForGroup.Instance()->StoredRecruitmentInfo);
        var size = sizeof(AgentLookingForGroup.RecruitmentSub);

        var dataFilePath = GetDataFileInfo(fileName).FullName;
        var dataSpan = new Span<byte>(address, size);

        FilesystemUtil.WriteAllBytesSafe(dataFilePath, dataSpan.ToArray());
    }

    private static DirectoryInfo GetPresetDirectory() {
        var directoryInfo = new DirectoryInfo(Path.Combine(Config.DataPath, "PartyFinderPresets"));
        if (!directoryInfo.Exists) {
            directoryInfo.Create();
        }

        return directoryInfo;
    }

    private static FileInfo GetDataFileInfo(string presetName) {
        var directoryInfo = GetPresetDirectory();

        var fileInfo = new FileInfo(Path.Combine(directoryInfo.FullName, $"{presetName}.preset.data"));
        if (!fileInfo.Exists) {
            fileInfo.Create().Close();
        }

        return fileInfo;
    }

    public static bool IsValidFileName(string fileName)
        => !fileName.Any(character => Enumerable.Contains(Path.GetInvalidFileNameChars(), character));
}
