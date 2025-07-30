﻿using System.Collections.Generic;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.UI;
using VanillaPlus.Utilities;

namespace VanillaPlus.Classes;

public class AddonConfig {
    private string fileName = null!;
   
    public static AddonConfig Load(string fileName, HashSet<SeVirtualKey> defaultKeyCombo) {
        var loadedConfig = Config.LoadConfig<AddonConfig>(fileName);
        loadedConfig.fileName = fileName;

        if (loadedConfig.OpenKeyCombo.Count is 0) {
            loadedConfig.OpenKeyCombo = defaultKeyCombo;
            loadedConfig.Save();
        }
        return loadedConfig;
    }

    public void Save()
        => Config.SaveConfig(this, fileName);

    public HashSet<SeVirtualKey> OpenKeyCombo = [];
    public Vector2? WindowPosition = null;
}
