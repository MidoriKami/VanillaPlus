using System;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.Interop;

namespace VanillaPlus.Features.RetrieveAllMateriaFromGearPiece;

public enum RetrievalAttemptStatus {
    NoAttemptMade,
    RetrievedSome,
    RetrievedAll,
    AttemptRunning,
    RetryNeeded,
    TimedOut
}

public unsafe class QueuedGearPiece(Pointer<InventoryItem> inventoryItem) {
    public uint ItemId => inventoryItem.Value->ItemId;

    private readonly byte startingPointMateriaCount = inventoryItem.Value->GetMateriaCount();

    private byte CurrentMateriaCount => inventoryItem.Value->GetMateriaCount();

    private RetrievalAttemptStatus lastRetrievalAttemptStatus
        = RetrievalAttemptStatus.NoAttemptMade;

    private byte previousMateriaCount = inventoryItem.Value->GetMateriaCount();

    private DateTime? lastRetrievalAttemptAt;

    private byte attemptsCounter = 0;

    public RetrievalAttemptStatus GetRetrievalAttemptStatus() {
        lastRetrievalAttemptStatus = CalculateRetrievalAttemptStatus();

        return lastRetrievalAttemptStatus;
    }

    private RetrievalAttemptStatus CalculateRetrievalAttemptStatus() {
        var currentCount = CurrentMateriaCount;

        if (currentCount == 0) {
            return RetrievalAttemptStatus.RetrievedAll;
        }

        if (!lastRetrievalAttemptAt.HasValue) {
            return RetrievalAttemptStatus.NoAttemptMade;
        }

        if (attemptsCounter > 3) {
            return RetrievalAttemptStatus.TimedOut;
        }


        if (currentCount != previousMateriaCount) {
            return RetrievalAttemptStatus.RetrievedSome;
        }

        if (lastRetrievalAttemptAt.Value.AddSeconds(3) < DateTime.UtcNow) {
            return RetrievalAttemptStatus.RetryNeeded;
        }

        return RetrievalAttemptStatus.AttemptRunning;
    }

    public bool IsForInventoryItem(Pointer<InventoryItem> outsideItem) {
        return inventoryItem.Equals(outsideItem);
    }

    public void AttemptRetrieval() {
        Services.PluginLog.Debug($"Attempt materia retrieval of itemId: {ItemId}");

        previousMateriaCount = inventoryItem.Value->GetMateriaCount();
        lastRetrievalAttemptAt = DateTime.UtcNow;
        attemptsCounter++;

        // This runs asynchronously and gives no insights whether it started or failed.
        EventFramework.Instance()->MaterializeItem(inventoryItem, MaterializeEntryId.Retrieve);
    }

    public GearPieceNodeData ToGearListItemNodeData() {
        return new GearPieceNodeData {
            ItemId = ItemId,
            StartingMateriaCount = startingPointMateriaCount,
            CurrentMateriaCount = CurrentMateriaCount,
            Status = lastRetrievalAttemptStatus,
        };
    }
}
