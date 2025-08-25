using System.Collections.Generic;
using System.IO;
using Dalamud.Utility;
using Newtonsoft.Json.Linq;

namespace VanillaPlus.Classes;

public class HaselTweaksCompatibilityModule(string moduleName) : CompatibilityModule {
    public override bool ShouldLoadGameModification() {
        // If HaselTweaks is not loaded, we can load our module
        if (!IsHaselTweaksLoaded()) return true;
        
        // If HaselTweaks is loaded, but doesn't contain our module, then we can load our module
        return !GetTargetPluginLoadedModules().Contains(moduleName);
    }

    public override string GetErrorMessage()
        => $"The original version of this feature is already active in HaselTweaks Plugin.\n\nID: {moduleName}";

    private static List<string> GetTargetPluginLoadedModules() {
        var configFileInfo = GetConfigFileInfo();
        if (configFileInfo.Exists) {
            var fileText = File.ReadAllText(configFileInfo.FullName);

            if (fileText.IsNullOrEmpty()) return [];
            
            var jObject = JObject.Parse(fileText);
            if (!jObject.HasValues) return [];

            var enabledTweaksToken = jObject.GetValue("EnabledTweaks");
            if (enabledTweaksToken is null) return [];

            if (enabledTweaksToken.Type is JTokenType.Array) {
                var enabledTweaksList = enabledTweaksToken.ToObject<List<string>>();
                
                return enabledTweaksList ?? [];
            }
        }

        return [];
    }
    
    private static bool IsHaselTweaksLoaded()
        => IsPluginLoaded("HaselTweaks");
    
    private static string GetConfigFilePath()
        => Path.Combine(Services.PluginInterface.GetPluginConfigDirectory().Replace("VanillaPlus", "HaselTweaks.json"));
    
    private static FileInfo GetConfigFileInfo()
        => new(GetConfigFilePath());
}
