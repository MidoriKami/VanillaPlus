using Lumina.Text.ReadOnly;

namespace VanillaPlus.Features.CosmicExplorationProgressWindow.Classes;

public class Progress(byte jobId, byte researchType) {
    public readonly byte JobId = jobId;
    public readonly byte ResearchType = researchType;

    public ushort Current;
    public uint IconId;
    public ReadOnlySeString IconTooltip;
    public ushort Max;
    public ushort Needed;

    public float Percentage => float.Clamp(Current / (float)Needed, 0, 1);

    // Explicitly check Current >= Max since sometimes Current == Max == Needed and our calculation would return NaN
    public float MaxPercentage => Current >= Max ? 1f : float.Clamp(((float)Current - Needed) / (Max - Needed), 0f, 1f);
    
    public bool Complete => Current >= Needed;

    public bool Capped => Current >= Max;
}
