using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using VanillaPlus.Enums;

namespace VanillaPlus.Classes;

public class ModificationManager : IAsyncDisposable {

    private readonly List<LoadedModification> loadedModifications = [];

    public IReadOnlyList<LoadedModification> LoadedModifications => loadedModifications;

    public async Task LoadModulesAsync() {
        var allGameModifications = GetGameModifications();

        List<Task> loadingTasks = [];

        foreach (var gameMod in allGameModifications) {
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

        VanillaPlus.PluginInterface.ActivePluginsChanged += OnPluginsChanged;
    }

    public async ValueTask DisposeAsync() {
        VanillaPlus.PluginInterface.ActivePluginsChanged -= OnPluginsChanged;

        Service<IPluginLog>.Get().Debug("Disposing Modification Manager, now disabling all GameModifications");

        if (System.SystemConfig.SafeMode) {
            Service<IPluginLog>.Get().Debug("Disposing in safemode, all modules will be unloaded sequentially.");

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
                        Service<IPluginLog>.Get().Warning($"Loaded plugins have changed, and {gameModification.Name} is now no longer allowed to be enabled");
                        moduleTasks.Add(Task.Run(() => TryDisableModification(gameModification, false)));
                        gameModification.State = LoadedState.CompatError;
                        gameModification.ErrorMessage = compatibilityModule.GetErrorMessage();
                    }
                    break;

                // If the module is disabled due to a compat error, re-evaluate if it can be enabled now
                case LoadedState.CompatError:

                    // This module was disabled due to compat, it is now allowed, load it
                    if (compatibilityModule.ShouldLoadGameModification()) {
                        Service<IPluginLog>.Get().Info($"Loaded plugins have changed, and {gameModification.Name} is now allowed to be enabled");
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
            Service<IPluginLog>.Get().Error($"[{modification.Name}] Attempted to enable errored modification");
            return;
        }

        try {
            Service<IPluginLog>.Get().Info($"Enabling {modification.Name}");

            if (modification.Modification.ModificationInfo is { DisabledReason: { } disabledReason }) {
                modification.State = LoadedState.ForceDisabled;
                modification.ErrorMessage = disabledReason;

                Service<IPluginLog>.Get().Warning($"[{modification.Name}] Force Disabled. {disabledReason}");
                Service<IPluginLog>.Get().Warning($"Aborted enabling {modification.Name}");
                return;
            }

            if (modification.Modification.ModificationInfo.CompatibilityModule is { } compatibilityModule) {
                if (!compatibilityModule.ShouldLoadGameModification()) {
                    modification.State = LoadedState.CompatError;
                    modification.ErrorMessage = compatibilityModule.GetErrorMessage();

                    Service<IPluginLog>.Get().Warning($"[{modification.Name}] {compatibilityModule.GetErrorMessage()}");
                    Service<IPluginLog>.Get().Warning($"Aborted enabling {modification.Name}");
                    return;
                }
            }

            await modification.Modification.OnEnableAsync();

            modification.State = LoadedState.Enabled;
            Service<IPluginLog>.Get().Info($"Successfully Enabled {modification.Name}");

            if (System.SystemConfig.EnabledModifications.Add(modification.Name)) {
                await System.SystemConfig.Save();
            }
        }
        catch (Exception e) {
            modification.State = LoadedState.Errored;
            modification.ErrorMessage = "Failed to load, this module has been disabled.";
            Service<IPluginLog>.Get().Error(e, $"Error while enabling {modification.Name}, attempting to disable");

            try {

                await modification.Modification.OnDisableAsync();

                Service<IPluginLog>.Get().Info($"Successfully disabled erroring modification {modification.Name}");
            }
            catch (Exception fatal) {
                modification.ErrorMessage = "Critical Error: Module failed to load, and errored again while unloading.";
                Service<IPluginLog>.Get().Error(fatal, $"Critical Error while trying to unload erroring modification: {modification.Name}");
            }
        }
    }

    private static async Task TryDisableModification(LoadedModification modification, bool removeFromList = true) {
        if (modification.State is LoadedState.Errored) {
            Service<IPluginLog>.Get().Error($"[{modification.Name}] Attempted to disable errored modification");
            return;
        }

        try {
            Service<IPluginLog>.Get().Info($"Disabling {modification.Name}");

            await modification.Modification.OnDisableAsync();

            modification.Modification.OpenConfigAction = null;
            modification.State = LoadedState.Disabled;
            Service<IPluginLog>.Get().Debug($"Successfully Disabled {modification.Name}");
        }
        catch (Exception e) {
            modification.State = LoadedState.Errored;
            Service<IPluginLog>.Get().Error(e, $"Failed to Disable {modification.Name}");
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
