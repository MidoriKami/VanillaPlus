using System.Numerics;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Command;
using VanillaPlus.Classes;
using VanillaPlus.Extensions;

namespace VanillaPlus.Features.ListInventory;

public class ListInventory : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "List Inventory Window",
        Description = "Adds a window that displays your inventory as a list, with toggleable filters.",
        Type = ModificationType.NewWindow,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "InitialChangelog"),
            new ChangeLogInfo(2, "Added Sort by Quantity"),
            new ChangeLogInfo(3, "Added `/listinventory` command to open window"),
            new ChangeLogInfo(4, "Sort Dropdown is now on another line, added reverse sort direction button"),
        ],
    };
    
    private AddonListInventory? listInventory;
    private AddonConfig? config;
    private AddonConfigWindow? configWindow;
    private KeybindListener? keybindListener;
    
    public override void OnEnable() {
        config = AddonConfig.Load("ListInventory.addon.config", [VirtualKey.SHIFT, VirtualKey.CONTROL, VirtualKey.I]);
        
        listInventory = new AddonListInventory {
            NativeController = System.NativeController,
            InternalName = "ListInventory",
            Title = "Inventory List",
            Size = new Vector2(450.0f, 700.0f),
            Config = config,
        };
        
        listInventory.InitializeConfig(config);

        keybindListener = new KeybindListener {
            KeybindCallback = () => {
                listInventory.Position = config.WindowPosition;
                listInventory.Size = config.WindowSize;
                listInventory.Toggle();
            },
            KeyCombo = config.OpenKeyCombo,
        };

        configWindow = new AddonConfigWindow("Inventory List", config, keybind => {
            keybindListener.KeyCombo = keybind;
        });

        OpenConfigAction = configWindow.Toggle;

        Services.CommandManager.AddHandler("/listinventory", new CommandInfo(OnListInventoryCommand) {
            DisplayOrder = 3,
            HelpMessage = "Open List Inventory Window",
        });
    }

    private void OnListInventoryCommand(string command, string arguments)
        => listInventory?.Toggle();

    public override void OnDisable() {
        listInventory?.Dispose();
        listInventory = null;
        
        configWindow?.Dispose();
        configWindow = null;
        
        keybindListener?.Dispose();
        keybindListener = null;

        config = null;
    }
}
