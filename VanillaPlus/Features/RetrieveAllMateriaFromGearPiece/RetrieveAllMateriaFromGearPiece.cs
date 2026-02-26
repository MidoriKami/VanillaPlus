using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.Interop;
using Lumina.Excel.Sheets;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.RetrieveAllMateriaFromGearPiece;

public unsafe class RetrieveAllMateriaFromGearPiece : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_RetrieveAllMateriaFromGearPiece,
        Description = Strings.ModificationDescription_RetrieveAllMateriaFromGearPiece,
        Authors = ["Marci696"],
        Type = ModificationType.GameBehavior,
        ChangeLog = [new ChangeLogInfo(1, "Initial Implementation"),],
    };

    private MateriaRetrievalProgressAddon? materiaRetrievalProgressAddon;

    private readonly List<QueuedItemNodeData> finishedItemsForMateriaRetrieval = [];
    private readonly Queue<QueuedItem> queuedItemsForMateriaRetrieval = [];

    public override void OnEnable() {
        ClearLists();

        materiaRetrievalProgressAddon = new MateriaRetrievalProgressAddon(
            queuedItemsForMateriaRetrieval,
            finishedItemsForMateriaRetrieval
        ) {
            Size = new Vector2(300.0f, 400.0f),
            InternalName = "MateriaRetrievalProgress",
            Title = Strings.RetrieveAllMateriaFromGearPiece_ProgressWindowTitle,
            ItemSpacing = 3.0f,
            OnClose = ClearLists,
        };
        materiaRetrievalProgressAddon.Initialize();

        Services.ContextMenu.OnMenuOpened += OnMenuOpened;
    }

    public override void OnDisable() {
        ClearLists();

        Services.ContextMenu.OnMenuOpened -= OnMenuOpened;
        Services.Framework.Update -= OnFrameworkUpdate;

        materiaRetrievalProgressAddon?.Dispose();
        materiaRetrievalProgressAddon = null;
    }

    private void OnMenuOpened(IMenuOpenedArgs args) {
        if (GetInventoryItem(args) is not { } inventoryItem) {
            return;
        }

        if (!DoesInventoryItemSupportMateria(inventoryItem)) {
            return;
        }

        args.AddMenuItem(
            new MenuItem {
                IsSubmenu = false,
                IsEnabled = inventoryItem.Value->GetMateriaCount() > 0,
                Name = Strings.RetrieveAllMateriaFromGearPiece_MenuItemName,

                // Blue circle to imitate the look of melded materia.
                PrefixColor = 37,
                Prefix = SeIconChar.Circle,

                OnClicked = clickedArgs => {
                    clickedArgs.OpenSubmenu(
                        [
                            new MenuItem {
                                Name = Strings.RetrieveAllMateriaFromGearPiece_MenuItemConfirm,
                                OnClicked = _ => AddItemToQueue(inventoryItem),
                            },
                            new MenuItem {
                                Name = Strings.RetrieveAllMateriaFromGearPiece_MenuItemCancel,
                            },
                        ]
                    );
                },
            }
        );
    }

    private static Pointer<InventoryItem>? GetInventoryItem(IMenuOpenedArgs args) {
        if (args.AddonName == "MateriaAttach") {
            var agent = AgentMateriaAttach.Instance();

            if (agent->SelectedItemIndex < 0) {
                return null;
            }

            if (agent->Data->ItemsSorted.Length <= agent->SelectedItemIndex) {
                return null;
            }

            var itemByIndex = agent->Data->ItemsSorted[agent->SelectedItemIndex];

            return itemByIndex.Value->Item;
        }

        if (args.MenuType != ContextMenuType.Inventory) {
            return null;
        }

        if (args.Target is not MenuTargetInventory invTarget) {
            return null;
        }

        if (invTarget.TargetItem is not { } targetItem) {
            return null;
        }

        return (InventoryItem*)targetItem.Address;
    }

    private void OnFrameworkUpdate(Dalamud.Plugin.Services.IFramework framework) {
        var currentItemForRetrieval = queuedItemsForMateriaRetrieval.FirstOrDefault();

        if (currentItemForRetrieval is null) {
            Services.PluginLog.Debug("Queue is empty and framework update will be unsubscribed to.");
            Services.Framework.Update -= OnFrameworkUpdate;

            return;
        }

        if (IsCurrentlyRetrievingMateria()) {
            return;
        }

        switch (currentItemForRetrieval.GetRetrievalAttemptStatus()) {
            case RetrievalAttemptStatus.NoAttemptMade:
                currentItemForRetrieval.AttemptRetrieval();

                return;
            case RetrievalAttemptStatus.RetrievedSome:
                Services.PluginLog.Debug($"Retrieved some materia from itemId: {currentItemForRetrieval.GetItemId()}");
                // There is more materia left to retrieve.
                currentItemForRetrieval.AttemptRetrieval();

                return;
            case RetrievalAttemptStatus.RetrievedAll:
                Services.PluginLog.Debug($"Retrieved all materia from itemId: {currentItemForRetrieval.GetItemId()}");

                DequeueItem();

                return;
            case RetrievalAttemptStatus.AttemptRunning:
                // Check again in the next update tick.
                return;
            case RetrievalAttemptStatus.RetryNeeded:
                Services.PluginLog.Debug(
                    $"Retrying retrieval of materia from itemId: {currentItemForRetrieval.GetItemId()}"
                );
                currentItemForRetrieval.AttemptRetrieval();

                return;
            case RetrievalAttemptStatus.TimedOut:
                // Character must have been busy and unable to retrieve materia in the current state.
                Services.PluginLog.Debug("Timed out while retrieving materia from one gear piece");
                Services.ChatGui.PrintError(Strings.RetrieveAllMateriaFromGearPiece_NotPossibleInState);

                DequeueItem();

                return;
        }
    }

    private void AddItemToQueue(InventoryItem* item) {
        // No need to queue duplicates.
        if (queuedItemsForMateriaRetrieval.Any((alreadyQueuedItem) => alreadyQueuedItem.EqualsInventoryItem(item))) {
            return;
        }

        var queuedItem = new QueuedItem(item);

        queuedItemsForMateriaRetrieval.Enqueue(queuedItem);

        materiaRetrievalProgressAddon?.Open();
        Services.Framework.Update += OnFrameworkUpdate;

        Services.PluginLog.Debug($"Queued material retrieval for itemId: {item->ItemId}");
    }

    private void DequeueItem() {
        if (!queuedItemsForMateriaRetrieval.TryDequeue(out var dequeuedItem)) {
            return;
        }

        finishedItemsForMateriaRetrieval.Add(dequeuedItem.ToQueuedItemNodeData());
    }

    private static bool DoesInventoryItemSupportMateria(InventoryItem* item) {
        var itemSheet = Services.DataManager.Excel.GetSheet<Item>();

        return itemSheet.GetRowOrDefault(item->ItemId)?.MateriaSlotCount > 0;
    }

    private void ClearLists() {
        queuedItemsForMateriaRetrieval.Clear();
        finishedItemsForMateriaRetrieval.Clear();
    }

    private static bool IsCurrentlyRetrievingMateria() {
        return Services.Condition[ConditionFlag.Occupied39];
    }
}
