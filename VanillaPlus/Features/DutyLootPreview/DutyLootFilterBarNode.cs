using System;
using System.Collections.Generic;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;
using Lumina.Text;

namespace VanillaPlus.Features.DutyLootPreview;

public enum LootFilter {
    All,
    Favorites,
    Equipment,
    Misc,
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

        AddButton(LootFilter.All, 61808, Strings("DutyLoot_Filter_All"));
        AddButton(LootFilter.Favorites, 61830, Strings("DutyLoot_Filter_Favorites"));
        AddButton(LootFilter.Equipment, 61828, Strings("DutyLoot_Filter_Equipment"));
        AddButton(LootFilter.Misc, 61807, Strings("DutyLoot_Filter_Misc"));

        UpdateButtonStates();
    }

    private void AddButton(LootFilter filter, uint iconId, string tooltipText) {
        var button = new IconToggleNode {
            Size = new Vector2(36, 36),
            IconId = iconId,
            Tooltip = new SeStringBuilder().Append(tooltipText).ToReadOnlySeString(),
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
