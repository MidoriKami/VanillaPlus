using System;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;

namespace VanillaPlus.Extensions;

/// <summary>
/// Extension methods for IFramework.
/// </summary>
public static class FrameworkExtensions {
    extension(IFramework framework) {

        /// <summary>
        /// Disposes all disposables on the main game thread.
        /// </summary>
        public async Task DisposeMainThreaded(params IDisposable?[] disposables)
            => await framework.Run(() => {
                foreach (var disposable in disposables) {
                    disposable?.Dispose();
                }
            });
    }
}
