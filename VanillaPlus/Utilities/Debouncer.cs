using System;
using System.Threading;
using System.Threading.Tasks;

namespace VanillaPlus.Utilities;

public class Debouncer : IDisposable {
    private CancellationTokenSource? cts;

    public void Run(Action<CancellationToken> action) {
        var newCts = new CancellationTokenSource();
        var oldCts = Interlocked.Exchange(ref cts, newCts);

        oldCts?.Cancel();
        oldCts?.Dispose();

        Task.Run(() => action(newCts.Token), newCts.Token);
    }

    public void Cancel() {
        var oldCts = Interlocked.Exchange(ref cts, null);
        oldCts?.Cancel();
        oldCts?.Dispose();
    }

    public void Dispose() => Cancel();
}
