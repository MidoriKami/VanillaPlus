using Lumina.Text.ReadOnly;

namespace VanillaPlus.Features.BetterEmoteWindow.Classes;

public readonly record struct EmoteLogMessagePreview(ReadOnlySeString Untargeted, ReadOnlySeString Targeted);
