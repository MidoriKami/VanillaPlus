namespace VanillaPlus.Features.RetrieveAllMateriaFromGearPieceContextMenu;

public record struct QueuedItemNodeData {
    public uint ItemId;
    public byte StartingMateriaCount;
    public byte CurrentMateriaCount;
    public RetrievalAttemptStatus Status;
}
