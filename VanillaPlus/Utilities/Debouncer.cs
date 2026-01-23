using System;
using System.Threading;
using System.Threading.Tasks;

namespace VanillaPlus.Utilities;

public class Debouncer : IDisposable {
    private CancellationTokenSource? _cts;

    public void Run(Action<CancellationToken> action) {
        var newCts = new CancellationTokenSource();
        var oldCts = Interlocked.Exchange(ref _cts, newCts);

        oldCts?.Cancel();
        oldCts?.Dispose();

        Task.Run(() => action(newCts.Token), newCts.Token);
    }

    public void Cancel() {
        var oldCts = Interlocked.Exchange(ref _cts, null);
        oldCts?.Cancel();
        oldCts?.Dispose();
    }

    public void Dispose() => Cancel();
}
