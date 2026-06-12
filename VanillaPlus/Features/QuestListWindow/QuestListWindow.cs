using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Game.Command;
using Dalamud.Plugin.Services;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Features.QuestListWindow.Addons;
using VanillaPlus.Native.Addons;

namespace VanillaPlus.Features.QuestListWindow;

public class QuestListWindow : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_QuestListWindow,
        Description = Strings.ModificationDescription_QuestListWindow,
        Type = ModificationType.NewWindow,
        Authors = ["MidoriKami"],
    };


    public override string ImageName => "QuestList.png";

    private QuestListAddon? questListAddon;
    private KeybindListener? keybindListener;
    private AddonConfig? questListAddonSettings;
    private AddonConfigAddon? keybindConfigAddon;

    public override async Task OnEnableAsync() {
        questListAddonSettings = await AddonConfig.Load("QuestList.addon.json");

        questListAddon = new QuestListAddon {
            Size = new Vector2(300.0f, 400.0f),
            InternalName = "QuestList",
            Title = Strings.QuestListWindow_Title,
        };

        keybindConfigAddon = new AddonConfigAddon {
            InternalName = "KeybindConfig",
            Title = "Fate List Window Keybind",
            AddonConfig = questListAddonSettings,
        };

        keybindListener = new KeybindListener {
            Callback = OnKeybindPressed,
            Keybind = questListAddonSettings.Keybind,
            IsEnabled = true,
        };

        OpenConfigAction = keybindConfigAddon.Open;

        Services.CommandManager.AddHandler("/questlist", new CommandInfo(OnQuestListCommand));
        Services.Framework.Update += OnFrameworkUpdate;
    }

    public override async Task OnDisableAsync() {
        Services.Framework.Update -= OnFrameworkUpdate;
        Services.CommandManager.RemoveHandler("/questlist");

        await Task.WhenAll(
            questListAddon?.DisposeAsync().AsTask() ?? Task.CompletedTask,
            keybindConfigAddon?.DisposeAsync().AsTask() ?? Task.CompletedTask
        );
        questListAddon = null;
        keybindConfigAddon = null;

        keybindListener = null;

        questListAddonSettings = null;
    }

    private void OnFrameworkUpdate(IFramework framework)
        => keybindListener?.Update();

    private void OnQuestListCommand(string command, string arguments)
        => questListAddon?.Toggle();

    private void OnKeybindPressed(ref bool isHandled) {
        Services.Framework.Run(() => questListAddon?.Toggle());

        isHandled = true;
    }
}
