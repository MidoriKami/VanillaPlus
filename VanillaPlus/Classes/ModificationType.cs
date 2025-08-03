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
    [Description("Debug")]
    Debug,
    
    /// <summary>
    /// Modifies some aspect of the base games user interface
    /// </summary>
    [Description("UI Modification")]
    UserInterface,
    
    /// <summary>
    /// Modifies some type of base game functionality to make it behave differently
    /// </summary>
    [Description("Game Behavior Modification")]
    GameBehavior,
    
    /// <summary>
    /// Adds a new native window to the game
    /// </summary>
    [Description("Custom Native Windows")]
    NewWindow,
    
    /// <summary>
    /// Adds a new native overlay to the game
    /// </summary>
    [Description("Custom Native Overlay")]
    NewOverlay,
}
