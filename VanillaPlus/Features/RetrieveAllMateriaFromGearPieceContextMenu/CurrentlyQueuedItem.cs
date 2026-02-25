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
    TimedOut
}

internal unsafe class CurrentlyQueuedItem(Pointer<InventoryItem> inventoryItem) {
    private byte previousMateriaCount = inventoryItem.Value->GetMateriaCount();

    private DateTime lastRetrievalAttemptAt = DateTime.UtcNow;

    private byte retrievalAttemptNumber = 0;

    public RetrievalAttemptStatus GetRetrievalAttemptStatus() {
        if (retrievalAttemptNumber == 0) {
            return RetrievalAttemptStatus.NoAttemptMade;
        }

        var currentCount = inventoryItem.Value->GetMateriaCount();

        if (currentCount != previousMateriaCount) {
            return currentCount == 0 ? RetrievalAttemptStatus.RetrievedAll : RetrievalAttemptStatus.RetrievedSome;
        }

        if (lastRetrievalAttemptAt.AddSeconds(3) < DateTime.UtcNow) {
            return RetrievalAttemptStatus.TimedOut;
        }

        return RetrievalAttemptStatus.AttemptRunning;
    }

    public void AttemptRetrieval() {
        var eventFramework = EventFramework.Instance();
        if (eventFramework is null) {
            return;
        }

        previousMateriaCount = inventoryItem.Value->GetMateriaCount();
        lastRetrievalAttemptAt = DateTime.UtcNow;
        retrievalAttemptNumber++;

        Services.PluginLog.Debug("Attempt materia retrieval");

        eventFramework->MaterializeItem(inventoryItem, MaterializeEntryId.Retrieve);
    }
}
