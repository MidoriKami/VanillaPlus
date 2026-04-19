using System.Collections.Generic;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Controllers;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Features.GearSetReorderButtons.Nodes;

namespace VanillaPlus.Features.GearSetReorderButtons;

public unsafe class GearSetReorderButtons : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_GearSetReorderButtons,
        Description = Strings.ModificationDescription_GearSetReorderButtons,
        Type = ModificationType.UserInterface,
        Authors = ["zajrik"],
    };

    public override string ImageName => "GearSetReorderButtons.png";

    private AddonController<AddonGearSetList>? gearSetsAddonController;
    private NativeListController<AddonGearSetList, GearSetListListItem>? gearSetsListController;

    private readonly Dictionary<uint, GearSetListReorderButtonNode> reorderButtonNodes = [];

    private const float ExtraAddonWidth = 56.0f;

    public override void OnEnable() {
        gearSetsAddonController = new AddonController<AddonGearSetList> {
            AddonName = "GearSetList",
            OnSetup = SetUpAddon,
            OnFinalize = FinalizeAddon,
        };
        gearSetsAddonController.Enable();

        gearSetsListController = new NativeListController<AddonGearSetList, GearSetListListItem> {
            AddonName = "GearSetList",
            GetPopulatorNode = GetPopulatorNode,
            UpdateElement = UpdateElement,
        };
        gearSetsListController.Enable();
    }

    public override void OnDisable() {
        gearSetsAddonController?.Dispose();
        gearSetsListController?.Dispose();

        gearSetsAddonController = null;
        gearSetsListController = null;
    }

    private static void SetUpAddon(AddonGearSetList* addon) {

        // Gearset Help Header Button
        var gearsetHelpButton = addon->GetNodeById(2);
        if (gearsetHelpButton is not null) {
            gearsetHelpButton->Position += new Vector2(ExtraAddonWidth, 0.0f);
        }
        
        // Gearset Count/Limit text node
        var gearsetCountTextNode = addon->GetNodeById(10);
        if (gearsetCountTextNode is not null) {
            gearsetCountTextNode->Position += new Vector2(ExtraAddonWidth, 0.0f);
        }

        // List Component Node
        var listComponentNode = addon->GetNodeById(7);
        if (listComponentNode is not null) {
            listComponentNode->Size += new Vector2(ExtraAddonWidth, 0.0f);
        }

        addon->AtkUnitBase.Size += new Vector2(ExtraAddonWidth, 0.0f);
    }

    private void FinalizeAddon(AddonGearSetList* addon) {

        // Gearset Help Header Button
        var gearsetHelpButton = addon->GetNodeById(2);
        if (gearsetHelpButton is not null) {
            gearsetHelpButton->Position -= new Vector2(ExtraAddonWidth, 0.0f);
        }

        // List Component Node
        var listComponentNode = addon->GetNodeById(7);
        if (listComponentNode is not null) {
            listComponentNode->Size -= new Vector2(ExtraAddonWidth, 0.0f);
        }

        // Gearset Count/Limit text node
        var gearsetCountTextNode = addon->GetNodeById(10);
        if (gearsetCountTextNode is not null) {
            gearsetCountTextNode->Position -= new Vector2(ExtraAddonWidth, 0.0f);
        }

        addon->AtkUnitBase.Size -= new Vector2(ExtraAddonWidth, 0.0f);

        foreach (var (_, node) in reorderButtonNodes) {
            node.Dispose();
        }
        reorderButtonNodes.Clear();
    }

    private static AtkComponentListItemRenderer* GetPopulatorNode(AddonGearSetList* addon)
        => addon->GetComponentListById(7)->FirstAtkComponentListItemRenderer;

    private void UpdateElement(AddonGearSetList* addon, GearSetListListItem listItemData) {

        // If the reorder button node does not exist, create and add it.
        if (!reorderButtonNodes.TryGetValue(listItemData.NodeId, out var reorderButton)) {
            var collisionNode = listItemData.CollisionNode;
            var ownerNode = listItemData.ItemRenderer->OwnerNode;
            
            reorderButton = new GearSetListReorderButtonNode {
                Size = new Vector2(ExtraAddonWidth - 4.0f, 28.0f),
                Position = new Vector2(collisionNode->AtkResNode.Width - 2.0f - ExtraAddonWidth - 4.0f, 0.0f),
            };
            
            // Resize the entire entry, so it's collision and events don't overlap with ours.
            ownerNode->AtkResNode.Size -= new Vector2(ExtraAddonWidth + 4.0f, 0.0f);

            reorderButtonNodes.Add(listItemData.NodeId, reorderButton);
            reorderButton.AttachNode(ownerNode, NodePosition.AsLastChild);
        }

        reorderButton.Update(listItemData);
    }
}
