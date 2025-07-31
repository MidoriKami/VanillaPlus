﻿using System.Numerics;
using VanillaPlus.Classes;

namespace VanillaPlus.BetterCursor;

public class BetterCursorConfig : GameModificationConfig<BetterCursorConfig> {
    protected override string FileName =>  "BetterCursor.config.json";

    public Vector4 Color = Vector4.One;
    public bool Animations = true;
    public bool HideOnCameraMove = true;
    public float Size = 96.0f;
}
