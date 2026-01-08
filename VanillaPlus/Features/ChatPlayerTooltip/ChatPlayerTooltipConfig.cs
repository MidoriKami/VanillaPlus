using VanillaPlus.Classes;

namespace VanillaPlus.Features.ChatPlayerTooltip;

public class ChatPlayerTooltipConfig : GameModificationConfig<ChatPlayerTooltipConfig> {
    protected override string FileName => "ChatTooltip";

    public bool showWorldName;
}
