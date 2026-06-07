using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Dalamud.Plugin;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.InternalSystem;

public class ModificationManager : IAsyncDisposable {

    private readonly List<LoadedModification> loadedModifications = [];

    public IReadOnlyList<LoadedModification> LoadedModifications => loadedModifications;

    public async Task LoadModulesAsync() {
        var allGameModifications = GetGameModifications();

        List<Task> loadingTasks = [];

        foreach (var gameMod in allGameModifications) {
            Services.PluginInterface.Inject(gameMod);

            var newLoadedModification = new LoadedModification(gameMod, LoadedState.Disabled);

            loadedModifications.Add(newLoadedModification);

            if (System.SystemConfig.EnabledModifications.Contains(gameMod.Name)) {
                if (System.SystemConfig.SafeMode) {
                    TryEnableModification(newLoadedModification).GetAwaiter().GetResult();
                }
                else {
                    loadingTasks.Add(Task.Run(() => TryEnableModification(newLoadedModification)));
                }
            }
        }

        await Task.WhenAll(loadingTasks);

        Services.PluginInterface.ActivePluginsChanged += OnPluginsChanged;
    }

    public async ValueTask DisposeAsync() {
        Services.PluginInterface.ActivePluginsChanged -= OnPluginsChanged;

        Services.PluginLog.InternalDebug("Disposing Modification Manager, now disabling all GameModifications");

        if (System.SystemConfig.SafeMode) {
            Services.PluginLog.InternalDebug("Disposing in safemode, all modules will be unloaded sequentially.");

            foreach (var modification in loadedModifications.Where(mod => mod.State is LoadedState.Enabled)) {
                TryDisableModification(modification, false).GetAwaiter().GetResult();
            }
        }
        else {
            await Task.WhenAll(loadedModifications
                .Where(loadedMod => loadedMod.State is LoadedState.Enabled)
                .Select(async module => {
                    await Task.Run(() => TryDisableModification(module, false));
                })
            );
        }
    }

    // When loaded plugins change, re-evaluate any compat modules
    private void OnPluginsChanged(IActivePluginsChangedEventArgs args)
        => Task.Run(ReloadConflictedModules);

    public async Task ReloadConflictedModules() {
        List<Task> moduleTasks = [];

        foreach (var gameModification in loadedModifications) {

            // Only evaluate modules that have a compatability module
            if (gameModification.Modification.ModificationInfo.CompatibilityModule is not { } compatibilityModule) continue;

            switch (gameModification.State) {
                // If the module is currently enabled, check that it should stay enabled, if not disable it
                case LoadedState.Enabled:

                    // This module was enabled, but after a refresh it's not allowed, disable it
                    if (!compatibilityModule.ShouldLoadGameModification()) {
                        Services.PluginLog.InternalWarning($"Loaded plugins have changed, and {gameModification.Name} is now no longer allowed to be enabled");
                        moduleTasks.Add(Task.Run(() => TryDisableModification(gameModification, false)));
                        gameModification.State = LoadedState.CompatError;
                        gameModification.ErrorMessage = compatibilityModule.GetErrorMessage();
                    }
                    break;

                // If the module is disabled due to a compat error, re-evaluate if it can be enabled now
                case LoadedState.CompatError:

                    // This module was disabled due to compat, it is now allowed, load it
                    if (compatibilityModule.ShouldLoadGameModification()) {
                        Services.PluginLog.InternalInfo($"Loaded plugins have changed, and {gameModification.Name} is now allowed to be enabled");
                        moduleTasks.Add(Task.Run(() => TryEnableModification(gameModification)));
                    }
                    break;
            }
        }

        await Task.WhenAll(moduleTasks);

        System.ModificationBrowserAddon.UpdateDisabledState();
    }

    private static async Task TryEnableModification(LoadedModification modification) {
        if (modification.State is LoadedState.Errored) {
            Services.PluginLog.InternalError($"[{modification.Name}] Attempted to enable errored modification");
            return;
        }

        try {
            Services.PluginLog.InternalInfo($"Enabling {modification.Name}");

            if (modification.Modification.ModificationInfo is { DisabledReason: { } disabledReason }) {
                modification.State = LoadedState.ForceDisabled;
                modification.ErrorMessage = disabledReason;

                Services.PluginLog.InternalWarning($"[{modification.Name}] Force Disabled. {disabledReason}");
                Services.PluginLog.InternalWarning($"Aborted enabling {modification.Name}");
                return;
            }

            if (modification.Modification.ModificationInfo.CompatibilityModule is { } compatibilityModule) {
                if (!compatibilityModule.ShouldLoadGameModification()) {
                    modification.State = LoadedState.CompatError;
                    modification.ErrorMessage = compatibilityModule.GetErrorMessage();

                    Services.PluginLog.InternalWarning($"[{modification.Name}] {compatibilityModule.GetErrorMessage()}");
                    Services.PluginLog.InternalWarning($"Aborted enabling {modification.Name}");
                    return;
                }
            }

            await modification.Modification.OnEnableAsync();

            modification.State = LoadedState.Enabled;
            Services.PluginLog.InternalInfo($"Successfully Enabled {modification.Name}");

            if (System.SystemConfig.EnabledModifications.Add(modification.Name)) {
                await System.SystemConfig.Save();
            }
        }
        catch (Exception e) {
            modification.State = LoadedState.Errored;
            modification.ErrorMessage = "Failed to load, this module has been disabled.";
            Services.PluginLog.InternalError(e, $"Error while enabling {modification.Name}, attempting to disable");

            try {

                await modification.Modification.OnDisableAsync();

                Services.PluginLog.InternalInfo($"Successfully disabled erroring modification {modification.Name}");
            }
            catch (Exception fatal) {
                modification.ErrorMessage = "Critical Error: Module failed to load, and errored again while unloading.";
                Services.PluginLog.InternalError(fatal, $"Critical Error while trying to unload erroring modification: {modification.Name}");
            }
        }
    }

    private static async Task TryDisableModification(LoadedModification modification, bool removeFromList = true) {
        if (modification.State is LoadedState.Errored) {
            Services.PluginLog.InternalError($"[{modification.Name}] Attempted to disable errored modification");
            return;
        }

        try {
            Services.PluginLog.InternalInfo($"Disabling {modification.Name}");

            await modification.Modification.OnDisableAsync();

            modification.Modification.OpenConfigAction = null;
            modification.State = LoadedState.Disabled;
            Services.PluginLog.InternalDebug($"Successfully Disabled {modification.Name}");
        }
        catch (Exception e) {
            modification.State = LoadedState.Errored;
            Services.PluginLog.InternalError(e, $"Failed to Disable {modification.Name}");
        }

        if (removeFromList) {
            System.SystemConfig.EnabledModifications.Remove(modification.Name);
            await System.SystemConfig.Save();
        }
    }

    public static async Task TryToggleModification(LoadedModification modification) {
        switch (modification) {
            case { State: LoadedState.Enabled }:
                await TryDisableModification(modification);
                break;

            case { State: LoadedState.Disabled }:
                await TryEnableModification(modification);
                break;
        }
    }

    private static List<GameModification> GetGameModifications() => Assembly
        .GetCallingAssembly()
        .GetTypes()
        .Where(type => type.IsSubclassOf(typeof(GameModification)))
        .Where(type => !type.IsAbstract)
        .Select(type => (GameModification?)Activator.CreateInstance(type))
        .Where(modification => modification?.ModificationInfo.Type is not ModificationType.Hidden)
        .OfType<GameModification>()
        .ToList();
}
