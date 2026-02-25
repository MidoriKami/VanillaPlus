using System;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.Interop;

namespace VanillaPlus.Features.RetrieveAllMateriaFromGearPieceContextMenu;

internal enum RetrievalAttemptStatus {
    NoAttemptMade,
    RetrievedSome,
    RetrievedAll,
    AttemptRunning,
    RetryNeeded,
    TimedOut
}

internal unsafe class CurrentlyQueuedItem(Pointer<InventoryItem> inventoryItem) {
    private byte previousMateriaCount = inventoryItem.Value->GetMateriaCount();

    private DateTime lastRetrievalAttemptAt = DateTime.UtcNow;

    private bool firstAttemptWasMade = false;

    private byte failedAttemptsCounter = 0;

    public RetrievalAttemptStatus GetRetrievalAttemptStatus() {
        if (!firstAttemptWasMade) {
            return RetrievalAttemptStatus.NoAttemptMade;
        }

        var currentCount = inventoryItem.Value->GetMateriaCount();

        if (currentCount != previousMateriaCount) {
            failedAttemptsCounter = 0;

            return currentCount == 0 ? RetrievalAttemptStatus.RetrievedAll : RetrievalAttemptStatus.RetrievedSome;
        }

        if (lastRetrievalAttemptAt.AddSeconds(3) < DateTime.UtcNow) {
            return ++failedAttemptsCounter < 3
                       ? RetrievalAttemptStatus.RetryNeeded
                       : RetrievalAttemptStatus.TimedOut;
        }

        return RetrievalAttemptStatus.AttemptRunning;
    }

    public uint GetItemId() {
        return inventoryItem.Value->ItemId;
    }

    public void AttemptRetrieval() {
        var eventFramework = EventFramework.Instance();
        if (eventFramework is null) {
            return;
        }

        previousMateriaCount = inventoryItem.Value->GetMateriaCount();
        lastRetrievalAttemptAt = DateTime.UtcNow;
        firstAttemptWasMade = true;

        Services.PluginLog.Debug($"Attempt materia retrieval of itemId: {GetItemId()}");

        // This runs asynchronously and gives no insights whether it started or failed.
        eventFramework->MaterializeItem(inventoryItem, MaterializeEntryId.Retrieve);
    }
}
