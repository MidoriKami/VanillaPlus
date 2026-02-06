using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Text.ReadOnly;
using VanillaPlus.Utilities;

namespace VanillaPlus.Features.PartyFinderPresets;

public static unsafe class PresetManager {
    public static string DefaultString => Strings.Preset_DefaultOption;
    public static string DontUseString => Strings.Preset_DontUseOption;

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
        var agent = AgentLookingForGroup.Instance();

        Data.LoadBinaryData(&agent->StoredRecruitmentInfo, sizeof(AgentLookingForGroup.RecruitmentSub), "PartyFinderPresets", $"{fileName}.preset.data");
        var extrasFile = Data.LoadData<PresetExtras>("PartyFinderPresets", $"{fileName}.extras.data");

        agent->AvgItemLv = extrasFile.ItemLevel;
        agent->AvgItemLvEnabled = extrasFile.ItemLevelEnabled;
    }

    public static void SavePreset(string fileName) {
        var agent = AgentLookingForGroup.Instance();

        Data.SaveBinaryData(&agent->StoredRecruitmentInfo, sizeof(AgentLookingForGroup.RecruitmentSub), "PartyFinderPresets", $"{fileName}.preset.data");
        Data.SaveData(new PresetExtras {
            ItemLevel = agent->AvgItemLv,
            ItemLevelEnabled = agent->AvgItemLvEnabled,
        }, "PartyFinderPresets", $"{fileName}.extras.data");
    }

    public static void RenamePreset(string oldName, string newName) {
        var presetFile = FileHelpers.GetFileInfo("Data", "PartyFinderPresets", $"{oldName}.preset.data");
        var extrasFile = FileHelpers.GetFileInfo("Data", "PartyFinderPresets", $"{oldName}.extras.data");

        if (presetFile is { Exists: true }) {
            presetFile.MoveTo(FileHelpers.GetFileInfo("Data", "PartyFinderPresets", $"{newName}.preset.data").FullName);
        }

        if (extrasFile is { Exists: true }) {
            extrasFile.MoveTo(FileHelpers.GetFileInfo("Data", "PartyFinderPresets", $"{newName}.extras.data").FullName);
        }
    }

    private static DirectoryInfo GetPresetDirectory() {
        var directoryInfo = new DirectoryInfo(Path.Combine(Data.DataPath, "PartyFinderPresets"));
        if (!directoryInfo.Exists) {
            directoryInfo.Create();
        }

        return directoryInfo;
    }

    public static bool IsValidFileName(ReadOnlySeString fileName)
        => !fileName.ToString().Any(character => Enumerable.Contains(Path.GetInvalidFileNameChars(), character));

    public static void DeletePreset(string fileName) {
        var presetFile = FileHelpers.GetFileInfo("Data", "PartyFinderPresets", $"{fileName}.preset.data");
        var extrasFile = FileHelpers.GetFileInfo("Data", "PartyFinderPresets", $"{fileName}.extras.data");

        if (presetFile.Exists) {
            presetFile.Delete();
        }

        if (extrasFile.Exists) {
            extrasFile.Delete();
        }
    }
}
