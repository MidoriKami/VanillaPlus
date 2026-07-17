using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using KamiToolKit.Enums;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Features.GearSetReorderButtons.Nodes;

namespace VanillaPlus.Features.GearSetReorderButtons;

public class GearSetReorderButtons : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_GearSetReorderButtons,
        Description = Strings.ModificationDescription_GearSetReorderButtons,
        Type = ModificationType.UserInterface,
        Authors = ["zajrik"],
    };

    public override string ImageName => "GearSetReorderButtons.png";

    private AddonController<AddonGearSetList>? gearSetsAddonController;
    private NativeListController<AddonGearSetList, GearSetListListItem>? gearSetsListController;

    private Dictionary<uint, GearSetListReorderButtonNode>? reorderButtonNodes;

    private const float ExtraAddonWidth = 56.0f;

    public override async Task OnEnableAsync() {
        unsafe {
            gearSetsAddonController = new AddonController<AddonGearSetList> {
                AddonName = "GearSetList",
                OnSetup = SetUpAddon,
                OnFinalize = FinalizeAddon,
            };

            gearSetsListController = new NativeListController<AddonGearSetList, GearSetListListItem> {
                AddonName = "GearSetList",
                GetPopulatorNode = GetPopulatorNode,
                UpdateElement = UpdateElement,
            };
        }

        await IFramework.Get().RunSafely(() => {
            gearSetsAddonController.Enable();
            gearSetsListController.Enable();
        });
    }

    public override async Task OnDisableAsync() {
        await IFramework.Get().RunSafely(() => {
            foreach (var (_, node) in reorderButtonNodes ?? []) {
                node.Dispose();
            }
            reorderButtonNodes?.Clear();

            gearSetsAddonController?.Dispose();
            gearSetsListController?.Dispose();
        });

        gearSetsAddonController = null;
        gearSetsListController = null;
        reorderButtonNodes = null;
    }

    private unsafe void SetUpAddon(AddonGearSetList* addon) {

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

        reorderButtonNodes = [];
    }

    private unsafe void FinalizeAddon(AddonGearSetList* addon) {

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

        // Intentionally leak the nodes here, the native list doesn't like these being disposed in finalize.
        // Instead, we will manually dispose in DisposeAsync if this feature is being disabled.
        reorderButtonNodes?.Clear();
        reorderButtonNodes = null;
    }

    private static unsafe AtkComponentListItemRenderer* GetPopulatorNode(AddonGearSetList* addon)
        => addon->GetComponentListById(7)->FirstAtkComponentListItemRenderer;

    private unsafe void UpdateElement(AddonGearSetList* addon, GearSetListListItem listItemData) {
        if (reorderButtonNodes is null) return;

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

            if (reorderButtonNodes.TryAdd(listItemData.NodeId, reorderButton)) {
                reorderButton.AttachNode(ownerNode, NodePosition.AsLastChild);
            }
            else {
                return;
            }
        }

        reorderButton.Update(listItemData);
    }
}
