using System.IO;
using Newtonsoft.Json.Linq;

namespace VanillaPlus.Classes;

public class QuestAwayCompatabilityModule : CompatibilityModule {

    public override bool ShouldLoadGameModification() {
        // If SimpleTweaks is not loaded, we can load our module
        if (!IsQuestAwayLoaded()) return true;
        
        // If SimpleTweaks is loaded, but doesn't contain our module, then we can load our module
        return !IsAetheryteFeatureEnabled();
    }

    public override string GetErrorMessage()
        => "Not compatible with QuestAWAYS's 'Aetherytes always in front' feature";
    
    private static bool IsAetheryteFeatureEnabled() {
        var configFileInfo = GetConfigFileInfo();
        if (configFileInfo.Exists) {
            var fileText = File.ReadAllText(configFileInfo.FullName);

            if (fileText.IsNullOrEmpty()) return false;
            
            var jObject = JObject.Parse(fileText);
            if (!jObject.HasValues) return false;

            var aetheryteInFront = jObject.GetValue("AetheryteInFront");
            if (aetheryteInFront is null) return false;

            if (aetheryteInFront.Type is JTokenType.Boolean) {
                return aetheryteInFront.ToObject<bool>();
            }
        }

        return false;
    }
    
    private static string GetConfigFilePath()
        => Path.Combine(Services.PluginInterface.GetPluginConfigDirectory().Replace("VanillaPlus", "QuestAWAY.json"));
    
    private static FileInfo GetConfigFileInfo()
        => new(GetConfigFilePath());
    
    private bool IsQuestAwayLoaded()
        => IsPluginLoaded("QuestAWAY");
}
