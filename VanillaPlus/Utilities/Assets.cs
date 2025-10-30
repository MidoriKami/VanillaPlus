using System;
using System.IO;

namespace VanillaPlus.Utilities;

public static class Assets {
    public static string GetAssetDirectoryPath()
        => Path.Combine(Services.PluginInterface.AssemblyLocation.DirectoryName ?? throw new Exception("Directory from Dalamud is Invalid Somehow"), "Assets");

    public static string GetAssetPath(string assetName)
        => Path.Combine(GetAssetDirectoryPath(), assetName);
}
