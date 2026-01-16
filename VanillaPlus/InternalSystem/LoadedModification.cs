using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.InternalSystem;

public class LoadedModification(GameModification modification, LoadedState state = LoadedState.Unknown) {
    public GameModification Modification { get; set; } = modification;
    public LoadedState State { get; set; } = state;
    public string ErrorMessage { get; set; } = string.Empty;
    
    public string Name => Modification.Name;
}
