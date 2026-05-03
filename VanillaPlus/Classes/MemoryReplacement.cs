using System;
using Dalamud.Memory;

namespace VanillaPlus.Classes;

public class MemoryReplacement(nint address, byte[] replacementBytes) : IDisposable {

    private byte[]? originalBytes;

    public void Enable() => Services.Framework.RunOnFrameworkThread(() => {
        if (originalBytes != null)
            return;

        originalBytes = ReplaceRaw(address, replacementBytes);
    });

    public void Disable() => Services.Framework.RunOnFrameworkThread(() => {
        if (originalBytes == null)
            return;

        ReplaceRaw(address, originalBytes);
        originalBytes = null;
    });

    public void Dispose()
        => Disable();

    public static byte[] ReplaceRaw(nint address, byte[] data) {
        var originalBytes = MemoryHelper.ReadRaw(address, data.Length);

        MemoryHelper.ChangePermission(address, data.Length, MemoryProtection.ExecuteReadWrite, out var oldPermissions);
        MemoryHelper.WriteRaw(address, data);
        MemoryHelper.ChangePermission(address, data.Length, oldPermissions);

        return originalBytes;
    }
}
