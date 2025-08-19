using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Inventory.InventoryEventArgTypes;
using VanillaPlus.Classes;
using VanillaPlus.Extensions;
using VanillaPlus.Modals;
using VanillaPlus.Utilities;

namespace VanillaPlus.Features.RecentlyLootedWindow;

public class RecentlyLootedWindow : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Recently Looted Items Window",
        Description = "Adds a window that shows a scrollable list of all items that you have looted this session.\n\n" +
                      "Can only show items looted after this feature is enabled.",
        Type = ModificationType.NewWindow,
        Authors = ["MidoriKami"],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
            new ChangeLogInfo(2, "Limit tracking to standard inventories, and armory"),
            new ChangeLogInfo(3, "Displays item quantity as text over icon instead of appended to the end of name"),
        ],
    };

    private AddonRecentlyLooted? recentlyLootedWindow;
    private AddonConfig? config;
    private KeybindModal? keybindModal;
    private KeybindListener? keybindListener;

    public override string ImageName => "RecentlyLootedWindow.png";

    public override void OnEnable() {
        config = AddonConfig.Load("RecentlyLooted.addon.json", [VirtualKey.CONTROL, VirtualKey.L]);

        recentlyLootedWindow = new AddonRecentlyLooted(config) {
            NativeController = System.NativeController,
            Size = new Vector2(250.0f, 350.0f),
            InternalName = "RecentlyLooted",
            Title = "Recently Looted Items",
        };
        
        if (config.WindowPosition is { } windowPosition) {
            recentlyLootedWindow.Position = windowPosition;
        }

        keybindListener = new KeybindListener {
            KeybindCallback = recentlyLootedWindow.Toggle,
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
        
        Services.GameInventory.InventoryChanged += OnRawItemAdded;
    }

    public override void OnDisable() {
        recentlyLootedWindow?.Dispose();
        recentlyLootedWindow = null;
        
        keybindModal?.Dispose();
        keybindModal = null;
        
        keybindListener?.Dispose();
        keybindListener = null;

        Services.GameInventory.InventoryChanged -= OnRawItemAdded;
    }

    private void OnRawItemAdded(IReadOnlyCollection<InventoryEventArgs> events) {
        foreach (var eventData in events) {
            if (!Inventory.StandardInventories.Contains(eventData.Item.ContainerType)) continue;

            recentlyLootedWindow?.AddInventoryItem(eventData);
        }
    }
}
