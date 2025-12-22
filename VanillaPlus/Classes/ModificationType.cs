using System.ComponentModel;

namespace VanillaPlus.Classes;

public enum ModificationType {
    
    /// <summary>
    /// Not intended for actual use, only used to prevent loading SampleGameModification
    /// </summary>
    Hidden,

    /// <summary>
    /// Not intended for actual use
    /// </summary>
    [Description("ModificationType_Debug")]
    Debug,
    
    /// <summary>
    /// Adds a new native window to the game
    /// </summary>
    [Description("ModificationType_NewWindow")]
    NewWindow,
    
    /// <summary>
    /// Modifies some aspect of the base games user interface
    /// </summary>
    [Description("ModificationType_UserInterface")]
    UserInterface,
    
    /// <summary>
    /// Modifies some type of base game functionality to make it behave differently
    /// </summary>
    [Description("ModificationType_GameBehavior")]
    GameBehavior,

    /// <summary>
    /// Adds a new native overlay to the game
    /// </summary>
    [Description("ModificationType_NewOverlay")]
    NewOverlay,
}
