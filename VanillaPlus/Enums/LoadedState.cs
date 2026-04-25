namespace VanillaPlus.Enums;

public enum LoadedState {
    Unknown,
    Enabled,
    Disabled,
    Errored,
    CompatError,
    ForceDisabled,
}

public static class LoadedStateExtensions {
    extension(LoadedState state) {
        public bool IsTrouble => state is LoadedState.Errored or LoadedState.CompatError or LoadedState.ForceDisabled;
    }
}
