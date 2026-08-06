using System;
using System.Linq;
using System.Threading.Tasks;

namespace VanillaPlus.Extensions;

/// <summary>
/// Extensions methods for Tasks.
/// </summary>
public static class TaskExtensions {
    extension(Task) {

        /// <summary>
        /// Awaits the completion of all IAsyncDisposable tasks.
        /// </summary>
        public static async Task WhenAllDisposed(params IAsyncDisposable?[] tasks)
            => await Task.WhenAll(tasks.Select(task => task?.DisposeAsync().AsTask() ?? Task.CompletedTask));
    }
}
