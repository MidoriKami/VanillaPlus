using System;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;

namespace VanillaPlus.Extensions;

/// <summary>
/// Extensions for dalamud's IFramework service class.
/// </summary>
public static class FrameworkExtensions {
    extension(IFramework framework) {
        /// <summary>
        /// Helper method for calling Framework.Run in a safe way that does nothing in the case of a game shutdown.
        /// </summary>
        public Task RunSafely(Action runAction) {
            // If we are unloading the game, do nothing and return completed.
            if (framework.IsFrameworkUnloading) return Task.CompletedTask;

            // If we are already on the main thread, run it and then return completed.
            if (ThreadSafety.IsMainThread) {
                runAction();
                return Task.CompletedTask;
            }

            // Else, queue it for running.
            return framework.Run(runAction);
        }
    }
}
