using System;
using System.Collections.Generic;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;
using Lumina.Text.ReadOnly;

namespace VanillaPlus.Features.DutyLootPreview;

public enum LootFilter {
    All,
    Favorites,
    Equipment,
    Misc
}

public class DutyLootFilterBarNode : HorizontalListNode {
    private readonly Dictionary<LootFilter, IconToggleNode> filterButtons = new();

    public Action<LootFilter>? OnFilterChanged { get; set; }

    public LootFilter CurrentFilter {
        get;
        private set {
            field = value;
            UpdateButtonStates();
        }
    } = LootFilter.All;

    public DutyLootFilterBarNode() {
        ItemSpacing = 1;

        AddButton(LootFilter.All, 61808, "All Items");
        AddButton(LootFilter.Favorites, 61830, "Favorites");
        AddButton(LootFilter.Equipment, 61828, "Equipment");
        AddButton(LootFilter.Misc, 61807, "Miscellaneous");

        UpdateButtonStates();
    }

    private void AddButton(LootFilter filter, uint iconId, ReadOnlySeString tooltip) {
        var button = new IconToggleNode {
            Size = new Vector2(36, 36),
            IconId = iconId,
            Tooltip = tooltip,
        };

        button.CollisionNode.AddEvent(AtkEventType.MouseClick, () => {
            CurrentFilter = filter;
            OnFilterChanged?.Invoke(filter);
        });

        AddNode(button);
        filterButtons[filter] = button;
    }

    private void UpdateButtonStates() {
        foreach (var (filter, button) in filterButtons) {
            button.IsToggled = filter == CurrentFilter;
        }
    }
}
