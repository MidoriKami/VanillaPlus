﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Inventory.InventoryEventArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using VanillaPlus.Core;
using VanillaPlus.Extensions;

namespace VanillaPlus.RecentlyLootedWindow;

public class RecentlyLootedWindow : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Recently Looted Items Window",
        Description = "Adds a window that shows a scrollable list of all items that you have looted this session.\n\n" +
                      "Can only show items looted after this feature is enabled.",
        Type = ModificationType.NewWindow,
        Authors = ["MidoriKami"],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    private AddonRecentlyLooted recentlyLootedWindow = null!; 
    private RecentlyLootedWindowConfig config = null!;
    private RecentlyLootedWindowConfigWindow configWindow = null!;

    private readonly Stopwatch stopwatch = Stopwatch.StartNew();

    public override string ImageName => "RecentlyLootedWindow.png";

    public override void OnEnable() {
        config = RecentlyLootedWindowConfig.Load();
        configWindow = new RecentlyLootedWindowConfigWindow(config);
        configWindow.AddToWindowSystem();
        OpenConfigAction = configWindow.Toggle;

        recentlyLootedWindow = new AddonRecentlyLooted(config) {
            NativeController = System.NativeController,
            Size = new Vector2(300.0f, 400.0f),
            InternalName = "RecentlyLooted",
            Title = "Recently Looted Items",
        };
        
        if (config.WindowPosition is { } windowPosition) {
            recentlyLootedWindow.Position = windowPosition;
        }

        Services.Framework.Update += OnFrameworkUpdate;
        Services.GameInventory.InventoryChanged += OnRawItemAdded;
    }

    public override void OnDisable() {
        recentlyLootedWindow.Dispose();
        configWindow.RemoveFromWindowSystem();
        Services.Framework.Update -= OnFrameworkUpdate;
        Services.GameInventory.InventoryChanged -= OnRawItemAdded;
    }

    private void OnRawItemAdded(IReadOnlyCollection<InventoryEventArgs> events) {
        foreach (var eventData in events) {
            recentlyLootedWindow.AddInventoryItem(eventData);
        }
    }

    private unsafe void OnFrameworkUpdate(IFramework framework) {
        if (UIInputData.Instance()->IsComboPressed(config.OpenKeyCombo.ToArray()) && stopwatch.ElapsedMilliseconds >= 250) {
            if (recentlyLootedWindow.IsOpen) {
                recentlyLootedWindow.Close();
            }
            else {
                recentlyLootedWindow.Open();
            }
            
            stopwatch.Restart();
        }
    }
}
