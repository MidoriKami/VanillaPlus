using System.Collections.Generic;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Features.GearSetReorderButtons.Nodes;

namespace VanillaPlus.Features.GearSetReorderButtons;

public unsafe class GearSetReorderButtons : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.GearSetReorderButtons_DisplayName,
        Description = Strings.GearSetReorderButtons_Description,
        Type = ModificationType.UserInterface,
        Authors = ["zajrik"],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    public override string ImageName => "GearSetReorderButtons.png";

    private AddonController<AddonGearSetList>? gearSetsAddonController;
    private NativeListController<AddonGearSetList, GearSetListListItem>? gearSetsListController;

    private readonly Dictionary<uint, GearSetListReorderButtonNode> reorderButtonNodes = [];

    private const ushort ExtraAddonWidth = 56;

    public override void OnEnable() {
        gearSetsAddonController = new() {
            AddonName = "GearSetList",
            OnSetup = SetUpAddon,
            OnFinalize = FinalizeAddon,
        };
        gearSetsAddonController.Enable();

        gearSetsListController = new() {
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

    private void SetUpAddon(AddonGearSetList* addon) {
        // Tried to use AtkUnitBase.Resize() but it doesn't update the position/width
        // of all of the nodes needed to make room for the reorder buttons so I'm still
        // doing this manually. It's still gross and I still hate it.

        var windowNode = addon->GetComponentByNodeId(11);
        var listNode = addon->GetComponentByNodeId(7);

        if (windowNode is null || listNode is null) return;

        // Get all the nodes that need widened
        var nodesToWiden = new AtkResNode*[] {
            addon->RootNode,

            addon->GetNodeById(11),
            addon->GetNodeById(8),
            addon->GetNodeById(7),
            addon->GetNodeById(5),

            windowNode->GetNodeById(12),
            windowNode->GetNodeById(11),
            windowNode->GetNodeById(10),
            windowNode->GetNodeById(9),
            windowNode->GetNodeById(8),
            windowNode->GetNodeById(2),

            listNode->GetNodeById(6),
        };

        foreach (var node in nodesToWiden) {
            if (node is null) continue;
            node->Width += ExtraAddonWidth;
        }

        // Get all the nodes that need repositioned
        var nodesToReposition = new AtkResNode*[] {
            addon->GetNodeById(10),
            addon->GetNodeById(2),

            windowNode->GetNodeById(7),

            listNode->GetNodeById(4),
        };

        foreach (var node in nodesToReposition) {
            if (node is null) continue;
            node->X += ExtraAddonWidth;
        }
    }

    private void FinalizeAddon(AddonGearSetList* addon) {
        // Dispose of all reorder button nodes
        foreach (var (_, node) in reorderButtonNodes) {
            node.Dispose();
        }
        reorderButtonNodes.Clear();
    }

    private AtkComponentListItemRenderer* GetPopulatorNode(AddonGearSetList* addon) {
        return addon->GetComponentListById(7)->FirstAtkComponentListItemRenderer;
    }

    private void UpdateElement(AddonGearSetList* addon, GearSetListListItem listItemData) {
        // Update reorder button node if it already exists
        if (reorderButtonNodes.TryGetValue(listItemData.NodeId, out var reorderButton)) {
            reorderButton.Update(listItemData);
            return;
        }

        // Otherwise create and cache new reorder button node
        var anchorNode = listItemData.ButtonAnchorNode;

        reorderButton = new GearSetListReorderButtonNode() {
            Position = new Vector2(anchorNode->X + anchorNode->Width - 2, 0)
        };

        reorderButtonNodes.Add(listItemData.NodeId, reorderButton);
        reorderButton.Update(listItemData);
        reorderButton.AttachNode(anchorNode);
    }
}
