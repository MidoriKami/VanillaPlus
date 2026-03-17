using System.IO;
using Newtonsoft.Json.Linq;

namespace VanillaPlus.Classes;

public class QuestAwayCompatabilityModule : CompatibilityModule {

    public override bool ShouldLoadGameModification() {
        if (!IsQuestAwayLoaded()) return true;

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
