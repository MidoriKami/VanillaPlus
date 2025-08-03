using System.Numerics;
using VanillaPlus.Classes;

namespace VanillaPlus.MiniCactpotHelper;

public class MiniCactpotHelperConfig : GameModificationConfig<MiniCactpotHelperConfig> {
    protected override string FileName => "MiniCactpotHelper.config.json";

    public bool EnableAnimations = true;

    public Vector4 ButtonColor = new(1.0f, 1.0f, 1.0f, 0.80f);
    public Vector4 LaneColor = new(1.0f, 1.0f, 1.0f, 1.0f);

    public uint IconId = 61332;
}
