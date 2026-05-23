// ReSharper disable RedundantUnsafeContext

using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.DevFeatures.DebugGameModification;

#if DEBUG
/// <summary>
/// Debug Game Modification for use with playing around with ideas, DO NOT commit changes to this file
/// </summary>
public unsafe class DebugGameModification : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Debug Game Modification",
        Description = "A module for playing around and testing VanillaPlus features",
        Type = ModificationType.Debug,
        Authors = ["YourNameHere"],
    };

    public override void OnEnableAsync() {

    }

    public override void OnDisableAsync() {

    }
}
#endif
