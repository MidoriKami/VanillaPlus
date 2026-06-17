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
            Title = "Fate List Window Keybind",
            AddonConfig = recentlyLootedAddonSettings,
            OnConfigChanged = OnAddonConfigChanged,
        };

        keybindListener = new KeybindListener {
            Callback = OnKeybindPressed,
            Keybind = recentlyLootedAddonSettings.Keybind,
            IsEnabled = recentlyLootedAddonSettings.KeybindEnabled,
        };

        OpenConfigAction = keybindConfigAddon.Open;

        Services.CommandManager.AddHandler("/recentloot", new CommandInfo(OnFateListCommand) {
            HelpMessage = "Opens Recently Looted Items window",
        });
        Services.Framework.Update += OnFrameworkUpdate;
    }

    public override async Task OnDisableAsync() {
        Services.Framework.Update -= OnFrameworkUpdate;
        Services.CommandManager.RemoveHandler("/recentloot");

        await Task.WhenAll(
            addonRecentlyLooted?.DisposeAsync().AsTask() ?? Task.CompletedTask,
            keybindConfigAddon?.DisposeAsync().AsTask() ?? Task.CompletedTask
        );
        addonRecentlyLooted = null;
        keybindConfigAddon = null;

        keybindListener = null;

        recentlyLootedAddonSettings = null;
    }

    private void OnFrameworkUpdate(IFramework framework)
        => keybindListener?.Update();

    private void OnFateListCommand(string command, string arguments)
        => addonRecentlyLooted?.Toggle();

    private void OnKeybindPressed(ref bool isHandled) {
        Services.Framework.Run(() => addonRecentlyLooted?.Toggle());

        isHandled = true;
    }

    private void OnAddonConfigChanged(AddonConfig addonConfig) {
        keybindListener?.IsEnabled = addonConfig.KeybindEnabled;
        keybindListener?.Keybind = addonConfig.Keybind;
    }
}
