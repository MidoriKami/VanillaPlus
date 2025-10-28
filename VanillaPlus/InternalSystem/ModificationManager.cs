using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dalamud.Plugin;
using VanillaPlus.Classes;

namespace VanillaPlus.InternalSystem;

public class ModificationManager : IDisposable {

    public readonly List<LoadedModification> LoadedModifications = [];

    public ModificationManager() {
        var allGameModifications = GetGameModifications();

        foreach (var gameMod in allGameModifications) {
            Services.PluginInterface.Inject(gameMod);

            var newLoadedModification = new LoadedModification(gameMod, LoadedState.Disabled);

            LoadedModifications.Add(newLoadedModification);

            if (System.SystemConfig.EnabledModifications.Contains(gameMod.Name)) {
                TryEnableModification(newLoadedModification);
            }
        }

        Services.PluginInterface.ActivePluginsChanged += OnPluginsChanged;
    }

    public void Dispose() {
        Services.PluginInterface.ActivePluginsChanged -= OnPluginsChanged;

        Services.PluginLog.Debug("Disposing Modification Manager, now disabling all GameModifications");
        
        foreach (var loadedMod in LoadedModifications) {
            if (loadedMod.State is LoadedState.Enabled) {
                try {
                    Services.PluginLog.Debug($"Disabling {loadedMod.Name}");
                    loadedMod.Modification.OnDisable();
                    Services.PluginLog.Debug($"Successfully Disabled {loadedMod.Name}");
                }
                catch (Exception e) {
                    Services.PluginLog.Error(e, $"Error while unloading modification {loadedMod.Name}");
                }
            }
        }
    }

    // When loaded plugins change, re-evaluate any compat modules
    private void OnPluginsChanged(IActivePluginsChangedEventArgs args)
        => ReloadConflictedModules();

    public void ReloadConflictedModules() {
        foreach (var gameModification in LoadedModifications) {

            // Only evaluate modules that have a compatability module
            if (gameModification.Modification.ModificationInfo.CompatibilityModule is not { } compatibilityModule) continue;

            switch (gameModification.State) {
                // If the module is currently enabled, check that it should stay enabled, if not disable it
                case LoadedState.Enabled:

                    // This module was enabled, but after a refresh it's not allowed, disable it
                    if (!compatibilityModule.ShouldLoadGameModification()) {
                        Services.PluginLog.Warning($"Loaded plugins have changed, and {gameModification.Name} is now no longer allowed to be enabled");
                        TryDisableModification(gameModification, false);
                        gameModification.State = LoadedState.CompatError;
                        gameModification.ErrorMessage = compatibilityModule.GetErrorMessage();
                    }
                    break;

                // If the module is disabled due to a compat error, re-evaluate if it can be enabled now
                case LoadedState.CompatError:

                    // This module was disabled due to compat, it is now allowed, load it
                    if (compatibilityModule.ShouldLoadGameModification()) {
                        Services.PluginLog.Info($"Loaded plugins have changed, and {gameModification.Name} is now allowed to be enabled");
                        TryEnableModification(gameModification);
                    }
                    break;
            }
        }

        System.AddonModificationBrowser.UpdateDisabledState();
    }

    public static void TryEnableModification(LoadedModification modification) {
        if (modification.State is LoadedState.Errored) {
            Services.PluginLog.Error($"[{modification.Name}] Attempted to enable errored modification");
            return;
        }

        try {
            Services.PluginLog.Info($"Enabling {modification.Name}");

            if (modification.Modification.ModificationInfo.CompatibilityModule is { } compatibilityModule) {
                if (!compatibilityModule.ShouldLoadGameModification()) {
                    modification.State = LoadedState.CompatError;
                    modification.ErrorMessage = compatibilityModule.GetErrorMessage();

                    Services.PluginLog.Warning($"[{modification.Name}] {compatibilityModule.GetErrorMessage()}");
                    Services.PluginLog.Warning($"Aborted enabling {modification.Name}");
                    return;
                }
            }

            modification.Modification.OnEnable();
            modification.State = LoadedState.Enabled;
            Services.PluginLog.Info($"Successfully Enabled {modification.Name}");
            System.SystemConfig.EnabledModifications.Add(modification.Name);
            System.SystemConfig.Save();
        }
        catch (Exception e) {
            modification.State = LoadedState.Errored;
            modification.ErrorMessage = "Failed to load, this module has been disabled.";
            Services.PluginLog.Error(e, $"Error while enabling {modification.Name}, attempting to disable");
            
            try {
                modification.Modification.OnDisable();
                Services.PluginLog.Information($"Successfully disabled erroring modification {modification.Name}");
            }
            catch (Exception fatal) {
                modification.ErrorMessage = "Critical Error: Module failed to load, and errored again while unloading.";
                Services.PluginLog.Error(fatal, $"Critical Error while trying to unload erroring modification: {modification.Name}");
            }
        }
    }

    public static void TryDisableModification(LoadedModification modification, bool removeFromList = true) {
        if (modification.State is LoadedState.Errored) {
            Services.PluginLog.Error("Attempted to disable errored modification");
            return;
        }

        try {
            Services.PluginLog.Info($"Disabling {modification.Name}");
            modification.Modification.OnDisable();
            modification.Modification.OpenConfigAction = null;
        }
        catch (Exception e) {
            modification.State = LoadedState.Errored;
            Services.PluginLog.Error(e, $"Failed to disable modification: {modification.Name}");
        } finally {
            modification.State = LoadedState.Disabled;
            Services.PluginLog.Debug($"Successfully Disabled {modification.Name}");

            if (removeFromList) {
                System.SystemConfig.EnabledModifications.Remove(modification.Name);
                System.SystemConfig.Save();
            }
        }
    }

    private static List<GameModification> GetGameModifications() => Assembly
       .GetCallingAssembly()
       .GetTypes()
       .Where(type => type.IsSubclassOf(typeof(GameModification)))
       .Where(type => !type.IsAbstract)
       .Select(type => (GameModification?) Activator.CreateInstance(type))
       .Where(modification => modification?.ModificationInfo.Type is not ModificationType.Hidden)
       .OfType<GameModification>()
       .ToList();
}
