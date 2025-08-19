using System.Numerics;
using Dalamud.Game.ClientState.Keys;
using VanillaPlus.Classes;
using VanillaPlus.Extensions;
using VanillaPlus.Modals;

namespace VanillaPlus.Features.ListInventory;

public class ListInventory : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "List Inventory Window",
        Description = "Adds a window that displays your inventory as a list, with toggleable filters.",
        Type = ModificationType.NewWindow,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "InitialChangelog"),
        ],
    };
    
    private AddonListInventory? listInventory;
    private AddonConfig? config;
    private KeybindModal? keybindModal;
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
        
        if (config.WindowPosition is { } windowPosition) {
            listInventory.Position = windowPosition;
        }

        if (config.WindowSize is { } windowSize) {
            listInventory.Size = windowSize;
        }

        keybindListener = new KeybindListener {
            KeybindCallback = listInventory.Toggle,
            KeyCombo = config.OpenKeyCombo,
        };
        
        keybindModal = new KeybindModal {
            KeybindSetCallback = keyBind => {
                config.OpenKeyCombo = keyBind;
                config.Save();
                    
                keybindListener.KeyCombo = keyBind;
            },
        };

        OpenConfigAction = keybindModal.Open;
    }

    public override void OnDisable() {
        listInventory?.Dispose();
        listInventory = null;
        
        keybindModal?.Dispose();
        keybindModal = null;
        
        keybindListener?.Dispose();
        keybindListener = null;

        config = null;
    }
}
