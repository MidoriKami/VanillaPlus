using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Game.Command;
using Dalamud.Plugin.Services;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Features.RecentlyLootedWindow.Addons;
using VanillaPlus.Native.Addons;

namespace VanillaPlus.Features.RecentlyLootedWindow;

public class RecentlyLootedWindow : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_RecentlyLootedWindow,
        Description = Strings.ModificationDescription_RecentlyLootedWindow,
        Type = ModificationType.NewWindow,
        Authors = ["MidoriKami"],
    };

    public override string ImageName => "RecentlyLootedWindow.png";

    private RecentlyLootedListAddon? addonRecentlyLooted;
    private KeybindListener? keybindListener;
    private AddonConfig? recentlyLootedAddonSettings;
    private AddonConfigAddon? keybindConfigAddon;

    public override async Task OnEnableAsync() {
        recentlyLootedAddonSettings = await AddonConfig.Load("RecentlyLooted.addon.json");

        addonRecentlyLooted = new RecentlyLootedListAddon {
            Size = recentlyLootedAddonSettings.GetWindowSizeWithDefault(new Vector2(250.0f, 350.0f)),
            InternalName = "RecentlyLooted",
            Title = Strings.RecentlyLootedWindow_Title,
        };

        keybindConfigAddon = new AddonConfigAddon {
            InternalName = "KeybindConfig",
            Title = Strings.AddonConfig_KeybindWindowTitle,
            AddonConfig = recentlyLootedAddonSettings,
            OnConfigChanged = OnAddonConfigChanged,
        };

        keybindListener = new KeybindListener {
            Callback = OnKeybindPressed,
            Keybind = recentlyLootedAddonSettings.Keybind,
            IsEnabled = recentlyLootedAddonSettings.KeybindEnabled,
        };

        OpenConfigAction = keybindConfigAddon.Toggle;

        ICommandManager.Get().AddHandler("/recentloot", new CommandInfo(OnFateListCommand) {
            HelpMessage = Strings.NodeList_OpenCommandHelp.Format(Strings.RecentlyLootedWindow_Title),
        });
        IFramework.Get().Update += OnFrameworkUpdate;
    }

    public override async Task OnDisableAsync() {
        IFramework.Get().Update -= OnFrameworkUpdate;
        ICommandManager.Get().RemoveHandler("/recentloot");

        await keybindConfigAddon.DisposeAsyncSafe();
        keybindConfigAddon = null;

        await addonRecentlyLooted.DisposeAsyncSafe();
        addonRecentlyLooted = null;

        keybindListener = null;
        recentlyLootedAddonSettings = null;
    }

    private void OnFrameworkUpdate(IFramework framework)
        => keybindListener?.Update();

    private void OnFateListCommand(string command, string arguments)
        => addonRecentlyLooted?.Toggle();

    private void OnKeybindPressed(ref bool isHandled) {
        IFramework.Get().Run(() => addonRecentlyLooted?.Toggle());

        isHandled = true;
    }

    private void OnAddonConfigChanged(AddonConfig addonConfig) {
        addonRecentlyLooted?.Size = addonConfig.GetWindowSizeWithDefault(new Vector2(250.0f, 350.0f));
        keybindListener?.IsEnabled = addonConfig.KeybindEnabled;
        keybindListener?.Keybind = addonConfig.Keybind;
    }
}
