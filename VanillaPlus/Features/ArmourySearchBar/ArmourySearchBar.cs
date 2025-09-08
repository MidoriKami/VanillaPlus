using System;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Config;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;
using KamiToolKit;
using Lumina.Extensions;
using VanillaPlus.Basic_Nodes;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.ArmourySearchBar;

public unsafe class ArmourySearchBar : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Armoury Search Bar",
        Description = "Adds a search bar to the armoury window.",
        Type = ModificationType.UserInterface,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "InitialChangelog"),
        ],
    };

    private AddonController<AddonArmouryBoard>? armouryInventoryController;

    private TextInputWithHintNode? inputText;
    private int armouryTab;

    private bool? configFadeUnusable;
    private bool searchStarted;
    
    public override void OnEnable() {
        armouryInventoryController = new AddonController<AddonArmouryBoard>("ArmouryBoard");
        armouryInventoryController.OnAttach += addon => {
            var headerSize = new Vector2(addon->WindowHeaderCollisionNode->Width, addon->WindowHeaderCollisionNode->Height);
            var searchBoxSize = new Vector2(200.0f, 28.0f);

            inputText = new TextInputWithHintNode {
                Position = headerSize / 2.0f - searchBoxSize / 2.0f + new Vector2(30.0f, 8.0f),
                Size = searchBoxSize,
                OnInputReceived = searchString => UpdateArmoury(addon, searchString),
                IsVisible = true,
            };
            System.NativeController.AttachNode(inputText, addon->WindowNode);
        };

        armouryInventoryController.OnUpdate += addon => {
            if (inputText is null) return;
            if (armouryTab != addon->TabIndex) {
                UpdateArmoury(addon, inputText.SearchString);
            }

            armouryTab = addon->TabIndex;
        };

        armouryInventoryController.OnDetach += addon => {
            UpdateArmoury(addon, string.Empty);
            System.NativeController.DisposeNode(ref inputText);
        };

        armouryInventoryController.Enable();
    }

    public override void OnDisable() {
        armouryInventoryController?.Dispose();
        armouryInventoryController = null;
    }
    
    private void UpdateArmoury(AddonArmouryBoard* addon, SeString searchString) {
        if (configFadeUnusable is null) {
            Services.GameConfig.TryGet(UiConfigOption.ItemNoArmoryMaskOff, out bool value);
            configFadeUnusable = value;
        }

        if (!searchString.ToString().IsNullOrEmpty() && !searchStarted) {
            Services.GameConfig.Set(UiConfigOption.ItemNoArmoryMaskOff, true);
            searchStarted = true;
        }

        if (searchStarted && searchString.ToString().IsNullOrEmpty()) {
            Services.GameConfig.Set(UiConfigOption.ItemNoArmoryMaskOff, configFadeUnusable.Value);
            searchStarted = false;
        }

        var sorter = ItemOrderModule.Instance()->ArmourySorter[addon->TabIndex].Value;

        // Temporary until ClientStructs Updates
        var slots = new Span<Pointer<AtkComponentDragDrop>>((void*)((nint)addon + 0x358), 50);

        foreach (var index in Enumerable.Range(0, slots.Length)) {
            var sorterItem = sorter->Items
                .FirstOrNull(item => item.Value->Slot == index);
            if (sorterItem is null) continue;
        
            var inventoryItem = sorter->GetInventoryItem(sorterItem);
            if (inventoryItem is null) continue;
        
            var inventorySlot = slots[index].Value;
            if (inventorySlot is null) continue;
        
            var slotNode = inventorySlot->OwnerNode;
            if (slotNode is null) continue;
                
            if (inventoryItem->IsRegexMatch(searchString.ToString())) {
                slotNode->FadeNode(0.0f);
            }
            else {
                slotNode->FadeNode(0.5f);
            }
        }
    }
}
