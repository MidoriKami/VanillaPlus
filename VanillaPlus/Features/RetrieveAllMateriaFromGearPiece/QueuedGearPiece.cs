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
    public readonly byte StartingPointMateriaCount = inventoryItem.Value->GetMateriaCount();

    public byte CurrentMateriaCount => inventoryItem.Value->GetMateriaCount();

    public RetrievalAttemptStatus LastRetrievalAttemptStatus { get; private set; }
        = RetrievalAttemptStatus.NoAttemptMade;

    private byte previousMateriaCount = inventoryItem.Value->GetMateriaCount();

    private DateTime? lastRetrievalAttemptAt;

    private byte attemptsCounter = 0;

    public RetrievalAttemptStatus GetRetrievalAttemptStatus() {
        LastRetrievalAttemptStatus = CalculateRetrievalAttemptStatus();

        return LastRetrievalAttemptStatus;
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

    public uint GetItemId() {
        return inventoryItem.Value->ItemId;
    }

    public bool EqualsInventoryItem(Pointer<InventoryItem> outsideItem) {
        return inventoryItem.Equals(outsideItem);
    }

    public void AttemptRetrieval() {
        var eventFramework = EventFramework.Instance();
        if (eventFramework is null) {
            return;
        }

        previousMateriaCount = inventoryItem.Value->GetMateriaCount();
        lastRetrievalAttemptAt = DateTime.UtcNow;

        Services.PluginLog.Debug($"Attempt materia retrieval of itemId: {GetItemId()}");

        attemptsCounter++;

        // This runs asynchronously and gives no insights whether it started or failed.
        eventFramework->MaterializeItem(inventoryItem, MaterializeEntryId.Retrieve);
    }

    public GearPieceNodeData ToGearListItemNodeData() {
        return new GearPieceNodeData {
            ItemId = GetItemId(),
            StartingMateriaCount = StartingPointMateriaCount,
            CurrentMateriaCount = CurrentMateriaCount,
            Status = GetRetrievalAttemptStatus()
        };
    }
}
