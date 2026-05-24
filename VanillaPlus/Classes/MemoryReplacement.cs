using System;
using System.Threading.Tasks;
using Dalamud.Memory;

namespace VanillaPlus.Classes;

public class MemoryReplacement(nint address, byte[] replacementBytes) : IDisposable, IAsyncDisposable {

    private byte[]? originalBytes;

    /// <summary>
    /// Applies memory patch.
    /// </summary>
    /// <exception cref="InvalidOperationException">When executed while not on the main thread. Await <see cref="EnableAsync"/> while not on main thread.</exception>
    public void Enable() {
        ThreadSafety.AssertMainThread();

        if (originalBytes != null)
            return;

        originalBytes = ReplaceRaw(address, replacementBytes);
    }

    public async Task EnableAsync()
        => await Services.Framework.Run(Enable);

    public void Disable() {
        ThreadSafety.AssertMainThread();

        if (originalBytes == null)
            return;

        ReplaceRaw(address, originalBytes);
        originalBytes = null;
    }

    public async Task DisableAsync()
        => await Services.Framework.Run(Disable);

    public void Dispose()
        => Disable();

    public async ValueTask DisposeAsync()
        => await DisableAsync();

    private static byte[] ReplaceRaw(nint address, byte[] data) {
        var originalBytes = MemoryHelper.ReadRaw(address, data.Length);

        MemoryHelper.ChangePermission(address, data.Length, MemoryProtection.ExecuteReadWrite, out var oldPermissions);
        MemoryHelper.WriteRaw(address, data);
        MemoryHelper.ChangePermission(address, data.Length, oldPermissions);

        return originalBytes;
    }
}
