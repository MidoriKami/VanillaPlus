using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using VanillaPlus.Features.GearSetReorderButtons.Nodes;

namespace VanillaPlus.Features.GearSetReorderButtons;

public unsafe class GearSetListUiController {
    private AddonController<AddonGearSetList>? gearSetsAddonController;
    private NativeListController<AddonGearSetList, GearSetListListItem>? gearSetsListController;

    private readonly Dictionary<uint, GearSetListReorderButtonNode> reorderButtonNodes = [];

    private const ushort ExtraAddonWidth = 56;

    public void OnEnable() {
        gearSetsAddonController = new() {
            AddonName = "GearSetList",
            OnSetup = SetUpAddon,
            OnFinalize = FinalizeAddon,
        };
        gearSetsAddonController.Enable();

        gearSetsListController = new() {
            AddonName = "GearSetList",
            GetPopulatorNode = GetPopulatorNode,
            ShouldModifyElement = ShouldModifyElement,
            UpdateElement = UpdateElement,
            ResetElement = ResetElement
        };
        gearSetsListController.Enable();
    }

    public void OnDisable() {
        gearSetsAddonController?.Dispose();
        gearSetsListController?.Dispose();

        gearSetsAddonController = null;
        gearSetsListController = null;
    }

    private void SetUpAddon(AddonGearSetList* addon) {
        // Tried to use AtkUnitBase.Resize() but it doesn't update the position/width
        // of all of the nodes needed to make room for the reorder buttons so I'm still
        // doing this manually. It's still gross and I still hate it.

        // Get all the nodes that need widened
        var nodesToWiden = new AtkResNode*[] {
            addon->GetNodeById(11),
            addon->GetNodeById(8),
            addon->GetNodeById(7),
            addon->GetNodeById(5),

            addon->GetComponentByNodeId(11)->GetNodeById(2),

            (AtkResNode*)addon->GetComponentByNodeId(11)->GetCollisionNodeById(12),
            (AtkResNode*)addon->GetComponentByNodeId(11)->GetCollisionNodeById(11),
            (AtkResNode*)addon->GetComponentByNodeId(11)->GetNineGridNodeById(10),
            (AtkResNode*)addon->GetComponentByNodeId(11)->GetNineGridNodeById(9),
            (AtkResNode*)addon->GetComponentByNodeId(11)->GetImageNodeById(8),

            (AtkResNode*)addon->GetComponentByNodeId(7)->GetCollisionNodeById(6),
        };

        foreach (var node in nodesToWiden) {
            if (node is null) continue;
            node->Width += ExtraAddonWidth;
        }

        // Get all the nodes that need repositioned
        var nodesToReposition = new AtkResNode*[] {
            addon->GetNodeById(10),
            addon->GetNodeById(2),
            addon->GetComponentByNodeId(11)->GetNodeById(7),
            addon->GetComponentByNodeId(7)->GetNodeById(4),
        };

        foreach (var node in nodesToReposition) {
            if (node is null) continue;
            node->X += ExtraAddonWidth;
        }
    }

    private void FinalizeAddon(AddonGearSetList* addon) {
        // Dispose of all reorder button nodes
        reorderButtonNodes.Values.ToList().ForEach(it => it.Dispose());
        reorderButtonNodes.Clear();
    }

    private AtkComponentListItemRenderer* GetPopulatorNode(AddonGearSetList* addon) {
        return addon->GetComponentListById(7)->FirstAtkComponentListItemRenderer;
    }

    private bool ShouldModifyElement(AddonGearSetList* addon, GearSetListListItem listItemData) {
        return true;
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

    private void ResetElement(AddonGearSetList* addon, GearSetListListItem listItemData) { }

}
