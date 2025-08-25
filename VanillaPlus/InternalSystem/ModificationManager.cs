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
        var gameModifications1 = GetGameModifications();

        foreach (var gameMod in gameModifications1) {
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
                    Services.PluginLog.Debug($"{loadedMod.Name} has been disabled");
                }
                catch (Exception e) {
                    Services.PluginLog.Error(e, $"Error while unloading modification {loadedMod.Name}");
                }
            }
        }
    }

    // When loaded plugins change, re-evaluate any compat modules
    private void OnPluginsChanged(IActivePluginsChangedEventArgs args) {
        
        // If a new plugin is loaded, check for modules that we now need to disable
        if (args is { Kind: PluginListInvalidationKind.Loaded }) {
            foreach (var modification in LoadedModifications) {
                
                // We only care about currently enabled modules
                if (modification.State is not LoadedState.Enabled) continue;
                if (modification.Modification.ModificationInfo.CompatibilityModule is { } compatibilityModule) {
                    
                    // It was loaded, but shouldn't be anymore, unload it.
                    if (!compatibilityModule.ShouldLoadGameModification()) {
                        TryDisableModification(modification, false);
                        modification.State = LoadedState.CompatError;
                        modification.ErrorMessage = compatibilityModule.GetErrorMessage();
                    }
                }
            }
        }
        
        // If a new plugin is unloaded, check for CompatError modules and try to enable them
        if (args is { Kind: PluginListInvalidationKind.Unloaded }) {
            foreach (var modification in LoadedModifications) {
                
                // We only care about compat error modules
                if (modification.State is not LoadedState.CompatError) continue;
                if (modification.Modification.ModificationInfo.CompatibilityModule is { } compatibilityModule) {
                    
                    // It tried to load earlier, but failed due to compat, if compat says we're good, load it
                    if (compatibilityModule.ShouldLoadGameModification()) {
                        TryEnableModification(modification);
                    }
                }
            }
        }
        
        System.AddonModificationBrowser.UpdateDisabledState();
    }

    public void TryEnableModification(LoadedModification modification) {
        if (modification.State is LoadedState.Errored) {
            Services.PluginLog.Error("Attempted to enable errored modification");
            return;
        }

        try {
            if (modification.Modification.ModificationInfo.CompatibilityModule is { } compatibilityModule) {
                if (!compatibilityModule.ShouldLoadGameModification()) {
                    modification.State = LoadedState.CompatError;
                    modification.ErrorMessage = compatibilityModule.GetErrorMessage();
                    
                    Services.PluginLog.Warning(compatibilityModule.GetErrorMessage());
                    return;
                }
            }
            
            Services.PluginLog.Info($"Enabling {modification.Name}");
            modification.Modification.OnEnable();
            modification.State = LoadedState.Enabled;
            Services.PluginLog.Info($"{modification.Name} has been enabled");
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

    public void TryDisableModification(LoadedModification modification, bool removeFromList = true) {
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
            Services.PluginLog.Info($"{modification.Name} has been disabled");

            if (removeFromList) {
                System.SystemConfig.EnabledModifications.Remove(modification.Name);
                System.SystemConfig.Save();
            }
        }
    }

    private List<GameModification> GetGameModifications() 
        => Assembly
           .GetCallingAssembly()
           .GetTypes()
           .Where(type => type.IsSubclassOf(typeof(GameModification)))
           .Where(type => !type.IsAbstract)
           .Select(type => (GameModification?) Activator.CreateInstance(type))
           .Where(modification => modification?.ModificationInfo.Type is not ModificationType.Hidden)
           .OfType<GameModification>()
           .ToList();
}
