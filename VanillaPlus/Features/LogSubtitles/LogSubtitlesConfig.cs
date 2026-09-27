using Dalamud.Game.Text;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.LogSubtitles;

public class LogSubtitlesConfig : GameModificationConfig<LogSubtitlesConfig> {

    protected override string FileName => "LogSubtitles";

    public XivChatType Channel = XivChatType.NPCDialogueAnnouncements;
}
