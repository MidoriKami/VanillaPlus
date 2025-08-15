using System.Numerics;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.DebugGameModification;

#if DEBUG
/// <summary>
/// Debug Game Modification for use with playing around with ideas, DO NOT commit changes to this file
/// </summary>
public class DebugGameModification : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Debug GameModification",
        Description = "A module for playing around and testing VanillaPlus features",
        Type = ModificationType.Debug,
        Authors = [ "YourNameHere" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    // This is probably a horrible idea...
    
    private AddonWhmGauge? whmGauge;
    
    public override void OnEnable() {
        whmGauge = new AddonWhmGauge {
            NativeController = System.NativeController,
            InternalName = "WhiteMageGauge",
            Title = "White Mage Gauge",
            Size = new Vector2(200.0f, 100.0f),
        };
        
        whmGauge.Open();
    }

    public override void OnDisable() {
        whmGauge?.Dispose();
        whmGauge = null;
    }
}
#endif
