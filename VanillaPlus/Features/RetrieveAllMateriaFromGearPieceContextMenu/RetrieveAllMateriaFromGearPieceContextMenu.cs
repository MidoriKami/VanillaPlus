using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Gui.ContextMenu;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.RetrieveAllMateriaFromGearPieceContextMenu;

public class RetrieveAllMateriaFromGearPieceContextMenu : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_RetrieveAllMateriaFromGearPieceContextMenu,
        Description = Strings.ModificationDescription_RetrieveAllMateriaFromGearPieceContextMenu,
        Authors = ["Marci696"],
        Type = ModificationType.GameBehavior,
        ChangeLog = [new ChangeLogInfo(1, "Initial Implementation"),],
    };
    
    private List<IntPtr> queuedItemsForMateriaRetrieval = [];
    
    public override void OnEnable() {
        queuedItemsForMateriaRetrieval = [];

        Services.ContextMenu.OnMenuOpened += OnMenuOpened;
        Services.Framework.Update += OnFrameworkUpdate;
    }

    public override void OnDisable() {
        Services.ContextMenu.OnMenuOpened -= OnMenuOpened;
        Services.Framework.Update -= OnFrameworkUpdate;
    }

    private void OnMenuOpened(IMenuOpenedArgs args) {
        if (args.MenuType != ContextMenuType.Inventory) {
            return;
        }

        if (args.Target is not MenuTargetInventory invTarget) {
            return;
        }

        if (invTarget.TargetItem is not { } targetItem) {
            return;
        }

        args.AddMenuItem(
            new MenuItem {
                IsSubmenu = false,
                Name = Strings.RetrieveAllMateriaFromGearPieceContextMenu_MenuItemName,
                OnClicked = clickedArgs => {
                    clickedArgs.OpenSubmenu(
                        [
                            new MenuItem {
                                Name = Strings.RetrieveAllMateriaFromGearPieceContextMenu_MenuItemConfirm,
                                OnClicked = _ => queuedItemsForMateriaRetrieval.Add(targetItem.Address),
                            },
                            new MenuItem {
                                Name = Strings.RetrieveAllMateriaFromGearPieceContextMenu_MenuItemCancel,
                            },
                        ]
                    );
                },
            }
        );
    }

    private unsafe void OnFrameworkUpdate(Dalamud.Plugin.Services.IFramework framework) {
        if (queuedItemsForMateriaRetrieval.Count == 0) {
            return;
        }

        if (IsCharacterBusy()) {
            return;
        }

        var inventorySlot = (InventoryItem*)queuedItemsForMateriaRetrieval.First();

        RetrieveMateria(inventorySlot);

        if (inventorySlot->GetMateriaCount() == 0) {
            queuedItemsForMateriaRetrieval.RemoveAt(0);
        }
    }

    private static bool IsCharacterBusy() {
        var condition = Services.Condition;
        
        // todo make any condition cancel the queue, except retrieving material which should be occupied39
        
        return
            condition[ConditionFlag.Occupied]
            || condition[ConditionFlag.OccupiedInEvent]
            || condition[ConditionFlag.OccupiedInQuestEvent]
            || condition[ConditionFlag.OccupiedInCutSceneEvent]
            || condition[ConditionFlag.BetweenAreas]
            || condition[ConditionFlag.BetweenAreas51]
            || condition[ConditionFlag.Jumping]
            || condition[ConditionFlag.Mounted]
            || condition[ConditionFlag.InCombat]
            || condition[ConditionFlag.Casting]
            || condition[ConditionFlag.MeldingMateria]
            || condition[ConditionFlag.Crafting]
            || condition[ConditionFlag.Gathering]
            || condition[ConditionFlag.Occupied39];
    }

    private unsafe void RetrieveMateria(InventoryItem* inventoryItem) {
        var eventFramework = EventFramework.Instance();
        if (eventFramework is null) {
            return;
        }

        eventFramework->MaterializeItem(inventoryItem, MaterializeEntryId.Retrieve);
    }
}
