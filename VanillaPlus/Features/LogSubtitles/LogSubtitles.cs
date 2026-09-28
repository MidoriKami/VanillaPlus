using System.Threading.Tasks;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Text;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Native.Addons;

namespace VanillaPlus.Features.LogSubtitles;

public class LogSubtitles : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_LogSubtitles,
        Description = Strings.ModificationDescription_LogSubtitles,
        Type = ModificationType.GameBehavior,
        Authors = ["beerpsi"],
    };

    public override string ImageName => "LogSubtitles.png";

    private LogSubtitlesConfig? config;
    private ConfigAddon? configAddon;

    public override async Task OnEnableAsync() {
        config = await LogSubtitlesConfig.Load();

        configAddon = new ConfigAddon {
            InternalName = "LogSubtitlesConfig",
            Title = Strings.LogSubtitles_ConfigTitle,
            Config = config,
        };

        configAddon.AddCategory(Strings.LogSubtitles_CategoryGeneral)
            .AddDropdown<XivChatType>(Strings.LogSubtitles_OutputChannel, nameof(LogSubtitlesConfig.Channel));

        OpenConfigAction = configAddon.Toggle;

        // A TalkSubtitle addon is configured when entering a cutscene, and then refreshed when the first text line
        // is displayed. This addon is then teared down. Additional subtitle lines get a new TalkSubtitle addon.
        IAddonLifecycle.Get().RegisterListener(AddonEvent.PostSetup, "TalkSubtitle", OnAddonTalkSubtitleEvent);
        IAddonLifecycle.Get().RegisterListener(AddonEvent.PostRefresh, "TalkSubtitle", OnAddonTalkSubtitleEvent);
    }

    public override async Task OnDisableAsync() {
        IAddonLifecycle.Get().UnregisterListener(OnAddonTalkSubtitleEvent);

        await configAddon.DisposeAsyncSafe();
        configAddon = null;

        config = null;
    }

    private unsafe void OnAddonTalkSubtitleEvent(AddonEvent evt, AddonArgs args) {
        var addon = args.GetAddon<AddonTalkSubtitle>();

        if (addon->SubtitleText.IsEmpty) return;
        if (addon->IsShowSuppressed) return;

        IChatGui.Get().Print(new XivChatEntry {
            Message = addon->SubtitleText.AsReadOnlySeString().ToDalamudString(),
            Name = Strings.LogSubtitles_Narrator,
            Type = config?.Channel ?? XivChatType.NPCDialogueAnnouncements,
        });
    }
}
