using System.Collections.Generic;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.Interop;
using Lumina.Excel.Sheets;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.RetrieveAllMateriaFromGearPieceContextMenu;

public unsafe class RetrieveAllMateriaFromGearPieceContextMenu : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_RetrieveAllMateriaFromGearPieceContextMenu,
        Description = Strings.ModificationDescription_RetrieveAllMateriaFromGearPieceContextMenu,
        Authors = ["Marci696"],
        Type = ModificationType.GameBehavior,
        ChangeLog = [new ChangeLogInfo(1, "Initial Implementation"),],
    };

    private readonly Queue<Pointer<InventoryItem>> queuedItemsForMateriaRetrieval = [];

    private CurrentlyQueuedItem? currentItemForRetrieval;

    public override void OnEnable() {
        ClearQueue();

        Services.ContextMenu.OnMenuOpened += OnMenuOpened;
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

        var inventoryItem = (InventoryItem*)targetItem.Address;

        if (!DoesInventoryItemSupportMateria(inventoryItem)) {
            return;
        }

        args.AddMenuItem(
            new MenuItem {
                IsSubmenu = false,
                IsEnabled = inventoryItem->GetMateriaCount() > 0,
                Name = Strings.RetrieveAllMateriaFromGearPieceContextMenu_MenuItemName,
           
                // Blue circle to imitate the look of melded materia.
                PrefixColor = 37,
                Prefix = SeIconChar.Circle,
                
                OnClicked = clickedArgs => {
                    clickedArgs.OpenSubmenu(
                        [
                            new MenuItem {
                                Name = Strings.RetrieveAllMateriaFromGearPieceContextMenu_MenuItemConfirm,
                                OnClicked = _ => AddItemToQueue(inventoryItem),
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

    private void OnFrameworkUpdate(Dalamud.Plugin.Services.IFramework framework) {
        if (queuedItemsForMateriaRetrieval.Count == 0 && currentItemForRetrieval is null) {
            Services.PluginLog.Debug("Queue is empty and framework update will be unsubscribed to.");
            Services.Framework.Update -= OnFrameworkUpdate;

            return;
        }

        if (IsCurrentlyRetrievingMateria()) {
            return;
        }

        if (currentItemForRetrieval is { } itemForRetrieval) {
            switch (itemForRetrieval.GetRetrievalAttemptStatus()) {
                case RetrievalAttemptStatus.NoAttemptMade:
                    itemForRetrieval.AttemptRetrieval();

                    return;
                case RetrievalAttemptStatus.RetrievedSome:
                    Services.PluginLog.Debug($"Retrieved some materia from itemId: {itemForRetrieval.GetItemId()}");
                    // There is more materia left to retrieve.
                    itemForRetrieval.AttemptRetrieval();

                    return;
                case RetrievalAttemptStatus.RetrievedAll:
                    Services.PluginLog.Debug($"Retrieved all materia from itemId: {itemForRetrieval.GetItemId()}");
                    currentItemForRetrieval = null;

                    // Continue with dequeuing.
                    break;
                case RetrievalAttemptStatus.AttemptRunning:
                    // Check again in the next update tick.
                    return;
                case RetrievalAttemptStatus.TimedOut:
                    // Character must have been busy and unable to retrieve materia in current state.
                    Services.PluginLog.Debug("Timed out while retrieving materia from one gear piece");
                    Services.ChatGui.PrintError(Strings.RetrieveAllMateriaFromGearPieceContextMenu_NotPossibleInState);
                    ClearQueue();

                    return;
            }
        }

        if (!queuedItemsForMateriaRetrieval.TryDequeue(out var queuedItemForMaterialRetrievalPointer)) {
            return;
        }

        currentItemForRetrieval = new CurrentlyQueuedItem(queuedItemForMaterialRetrievalPointer);
        currentItemForRetrieval.AttemptRetrieval();
    }

    private void AddItemToQueue(InventoryItem* item) {
        queuedItemsForMateriaRetrieval.Enqueue(item);
        Services.Framework.Update += OnFrameworkUpdate;

        Services.PluginLog.Debug($"Queued material retrieval for itemId: {item->ItemId}");
    }

    private static bool DoesInventoryItemSupportMateria(InventoryItem* item) {
        var itemSheet = Services.DataManager.Excel.GetSheet<Item>();
        
        return itemSheet.GetRowOrDefault(item->ItemId)?.MateriaSlotCount > 0;
    }

    private void ClearQueue() {
        currentItemForRetrieval = null;
        queuedItemsForMateriaRetrieval.Clear();
    }

    private static bool IsCurrentlyRetrievingMateria() {
        return Services.Condition[ConditionFlag.Occupied39];
    }
}
