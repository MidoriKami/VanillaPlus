using Lumina.Text.ReadOnly;

namespace VanillaPlus.Features.BetterEmoteWindow;

public readonly record struct EmoteLogMessagePreview(ReadOnlySeString Untargeted, ReadOnlySeString Targeted);
