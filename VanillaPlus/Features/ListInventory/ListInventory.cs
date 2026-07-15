using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Game.Command;
using Dalamud.Plugin.Services;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Features.ListInventory.Addons;
using VanillaPlus.Native.Addons;

namespace VanillaPlus.Features.ListInventory;

public class ListInventory : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_ListInventory,
        Description = Strings.ModificationDescription_ListInventory,
        Type = ModificationType.NewWindow,
        Authors = ["MidoriKami"],
    };

    public override string ImageName => "ListInventory.png";

    private InventoryListAddon? inventoryListAddon;
    private KeybindListener? keybindListener;
    private AddonConfig? inventoryListAddonSettings;
    private AddonConfigAddon? keybindConfigAddon;

    public override async Task OnEnableAsync() {
        inventoryListAddonSettings = await AddonConfig.Load("ListInventory.addon.json");

        inventoryListAddon = new InventoryListAddon {
            Size = inventoryListAddonSettings.GetWindowSizeWithDefault(new Vector2(500.0f, 510.0f)),
            InternalName = "ListInventory",
            Title = Strings.ListInventory_Title,
        };

        keybindConfigAddon = new AddonConfigAddon {
            InternalName = "KeybindConfig",
            Title = "List Inventory Window Keybind",
            AddonConfig = inventoryListAddonSettings,
            OnConfigChanged = OnAddonConfigChanged,
        };

        keybindListener = new KeybindListener {
            Callback = OnKeybindPressed,
            Keybind = inventoryListAddonSettings.Keybind,
            IsEnabled = inventoryListAddonSettings.KeybindEnabled,
        };

        OpenConfigAction = keybindConfigAddon.Toggle;

        Services.GetService<ICommandManager>().AddHandler("/listinventory", new CommandInfo(OnListInventoryCommand) {
            HelpMessage = "Opens Inventory List Window",
        });
        Services.GetService<IFramework>().Update += OnFrameworkUpdate;
    }

    public override async Task OnDisableAsync() {
        Services.GetService<IFramework>().Update -= OnFrameworkUpdate;
        Services.GetService<ICommandManager>().RemoveHandler("/listinventory");

        await Task.WhenAll(inventoryListAddon?.DisposeAsync().AsTask() ?? Task.CompletedTask,
            keybindConfigAddon?.DisposeAsync().AsTask() ?? Task.CompletedTask
        );
        inventoryListAddon = null;
        keybindConfigAddon = null;

        keybindListener = null;

        inventoryListAddonSettings  = null;
    }

    private void OnFrameworkUpdate(IFramework framework)
        => keybindListener?.Update();

    private void OnListInventoryCommand(string command, string arguments)
        => inventoryListAddon?.Toggle();

    private void OnKeybindPressed(ref bool isHandled) {
        Services.GetService<IFramework>().RunSafely(() => inventoryListAddon?.Toggle());

        isHandled = true;
    }

    private void OnAddonConfigChanged(AddonConfig addonConfig) {
        inventoryListAddon?.Size = addonConfig.GetWindowSizeWithDefault(new Vector2(500.0f, 510.0f));
        keybindListener?.IsEnabled = addonConfig.KeybindEnabled;
        keybindListener?.Keybind = addonConfig.Keybind;
    }
}
