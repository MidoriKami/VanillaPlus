using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Game.Command;
using Dalamud.Plugin.Services;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Native.Addons;

namespace VanillaPlus.Features.FateListWindow;

public class FateListWindow : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_FateListWindow,
        Description = Strings.ModificationDescription_FateListWindow,
        Type = ModificationType.NewWindow,
        Authors = ["MidoriKami"],
    };

    public override string ImageName => "FateListWindow.png";

    private FateListAddon? addonFateList;
    private KeybindListener? keybindListener;
    private AddonConfig? fateListAddonSettings;
    private AddonConfigAddon? keybindConfigAddon;

    public override async Task OnEnableAsync() {
        fateListAddonSettings = await AddonConfig.Load("FateList.addon.json");

        addonFateList = new FateListAddon {
            Size = fateListAddonSettings.GetWindowSizeWithDefault(new Vector2(300.0f, 400.0f)),
            InternalName = "FateList",
            Title = Strings.FateListWindow_Title,
        };

        keybindConfigAddon = new AddonConfigAddon {
            InternalName = "KeybindConfig",
            Title = "Fate List Window Keybind",
            AddonConfig = fateListAddonSettings,
            OnConfigChanged = OnAddonConfigChanged,
        };

        keybindListener = new KeybindListener {
            Callback = OnKeybindPressed,
            Keybind = fateListAddonSettings.Keybind,
            IsEnabled = fateListAddonSettings.KeybindEnabled,
        };

        OpenConfigAction = keybindConfigAddon.Open;

        Services.CommandManager.AddHandler("/fatelist", new CommandInfo(OnFateListCommand) {
            HelpMessage = "Opens Fate List Window",
        });
        Services.Framework.Update += OnFrameworkUpdate;
    }

    public override async Task OnDisableAsync() {
        Services.Framework.Update -= OnFrameworkUpdate;
        Services.CommandManager.RemoveHandler("/fatelist");

        await Task.WhenAll(
            addonFateList?.DisposeAsync().AsTask() ?? Task.CompletedTask,
            keybindConfigAddon?.DisposeAsync().AsTask() ?? Task.CompletedTask
        );
        addonFateList = null;
        keybindConfigAddon = null;

        keybindListener = null;

        fateListAddonSettings = null;
    }

    private void OnFrameworkUpdate(IFramework framework)
        => keybindListener?.Update();

    private void OnFateListCommand(string command, string arguments)
        => addonFateList?.Toggle();

    private void OnKeybindPressed(ref bool isHandled) {
        Services.Framework.Run(() => addonFateList?.Toggle());

        isHandled = true;
    }

    private void OnAddonConfigChanged(AddonConfig addonConfig) {
        keybindListener?.IsEnabled = addonConfig.KeybindEnabled;
        keybindListener?.Keybind = addonConfig.Keybind;
    }
}
