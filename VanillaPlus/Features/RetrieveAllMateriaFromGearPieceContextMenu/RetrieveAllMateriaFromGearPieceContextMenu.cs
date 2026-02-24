using System.Linq;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Config;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Inventory;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.RetrieveAllMateriaFromGearPieceContextMenu;

public class RetrieveAllMateriaFromGearPieceContextMenu : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        // todo replace with Strings. class
        DisplayName = "Retrieve All Materia From Gear Piece Context Menu",
        Description = "Makes it possible to retrieve all materia at once from a gear piece, instead of having to do it one by one.",
        Authors = ["Marci696"],
        Type = ModificationType.GameBehavior,
        ChangeLog = [new ChangeLogInfo(1, "Initial Implementation"),],
    };
    
    private GameInventoryItem? _pendingItemToRetrieveMateriaFrom;

    // todo replace image
    public override string ImageName => null;

    public override void OnEnable() {
        Services.ContextMenu.OnMenuOpened += OnMenuOpened;
        Services.Framework.Update += OnFrameworkUpdate;
    }

    public override void OnDisable() {
        Services.ContextMenu.OnMenuOpened -= OnMenuOpened;
        Services.Framework.Update -= OnFrameworkUpdate;
    }

    private void OnMenuOpened(IMenuOpenedArgs args) {
        var test = args.AddonName;


        if (args.MenuType != ContextMenuType.Inventory) {
            return;
        }


        if (args.Target is not MenuTargetInventory invTarget) {
            return;
        }

        if (invTarget.TargetItem is not { } targetItem) {
            return;
        }

        if (targetItem.MateriaEntries.Count == 0) {
            return;
        }

        args.AddMenuItem(
            new MenuItem {
                IsSubmenu = false,
                // todo replace with access to Strings class
                Name = "Extract All Materia",
                OnClicked = _ => { this._pendingItemToRetrieveMateriaFrom = invTarget.TargetItem; },
            }
        );
    }

    private unsafe void OnFrameworkUpdate(Dalamud.Plugin.Services.IFramework framework) {
        if (_pendingItemToRetrieveMateriaFrom is not { } itemToRetrieveMateriaFrom) {
            return;
        }

        if (IsCharacterBusy()) {
            return;
        }

        var inventorySlot = (InventoryItem*)itemToRetrieveMateriaFrom.Address;
        inventorySlot->GetMateriaCount();

        if (inventorySlot->GetMateriaCount() == 0) {
            _pendingItemToRetrieveMateriaFrom = null;
            return;
        }

        RetrieveMateria(itemToRetrieveMateriaFrom);
    }

    private static bool IsCharacterBusy() {
        // Keep this conservative: if the game says we're occupied by UI/event/etc, don't spam actions.
        var c = Services.Condition;

        return
            c[ConditionFlag.Occupied]
            || c[ConditionFlag.OccupiedInEvent]
            || c[ConditionFlag.OccupiedInQuestEvent]
            || c[ConditionFlag.OccupiedInCutSceneEvent]
            || c[ConditionFlag.BetweenAreas]
            || c[ConditionFlag.BetweenAreas51]
            || c[ConditionFlag.Jumping]
            || c[ConditionFlag.Mounted]
            || c[ConditionFlag.InCombat]
            || c[ConditionFlag.Casting]
            || c[ConditionFlag.MeldingMateria]
            || c[ConditionFlag.Crafting]
            || c[ConditionFlag.Gathering]
            || c[ConditionFlag.Occupied39];
    }

    private unsafe void RetrieveMateria(GameInventoryItem inventoryItem) {
        var eventFramework = EventFramework.Instance();
        if (eventFramework is null) {
            return;
        }

        var inventorySlot = (InventoryItem*)inventoryItem.Address;
        inventorySlot->GetMateriaCount();

        eventFramework->MaterializeItem(inventorySlot, MaterializeEntryId.Retrieve);
    }
}
