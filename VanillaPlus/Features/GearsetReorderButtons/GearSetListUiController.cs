using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;

namespace VanillaPlus.Features.GearsetReorderButtons;

public unsafe class GearSetListUiController {
    private AgentGearSet* agentGearSet;

    private NativeListController? gearSetListController;
    private AddonController<AddonGearSetList>? gearSetList;

    public void OnEnable() {
        agentGearSet = AgentGearSet.Instance();

        gearSetListController = new("GearSetList") {
            ShouldModifyElement = ShouldModifyElement,
            GetPopulatorNode = GetPopulatorNode,
            UpdateElement = UpdateElement,
            ResetElement = ResetElement
        };
        Services.PluginLog.Debug($"ShouldModifyElement");

        gearSetList = new AddonController<AddonGearSetList>("GearSetList");
        gearSetList.OnAttach += AttachNodes;
        gearSetList.OnDetach += DetachNodes;
        gearSetList.OnRefresh += RefreshNodes;

        gearSetList.Enable();
        gearSetListController.Enable();
    }

    private AtkComponentListItemRenderer* GetPopulatorNode(AtkUnitBase* addon) {
        var gearSetListAddon = (AddonGearSetList*)addon;
        return gearSetListAddon->GetComponentListById(7)->FirstAtkComponentListItemRenderer;
    }

    private bool ShouldModifyElement(AtkUnitBase* addon, ListItemData listItemData, AtkResNode** nodeList) {
        return listItemData.ItemRenderer->ComponentNode->GetAsAtkComponentListItemRenderer()->IsChecked;
    }

    private void UpdateElement(AtkUnitBase* addon, ListItemData listItemData, AtkResNode** nodeList) {
        Services.PluginLog.Debug($"element updated {listItemData.ItemRenderer->ComponentNode->NodeId}");
    }

    private void ResetElement(AtkUnitBase* addon, ListItemData listItemData, AtkResNode** nodeList) {
        Services.PluginLog.Debug($"element reset {listItemData.ItemRenderer->ComponentNode->NodeId}");
    }

    public void OnDisable() {
        gearSetList?.Dispose();
        gearSetListController?.Dispose();
        gearSetList = null;
        agentGearSet = null;
    }

    private void AttachNodes(AddonGearSetList* addon) {
        Services.PluginLog.Debug("AttachNodes called");
        var gearSetIds = agentGearSet->GearSetIds;

        //agentGearSet->OpenGearsetPreview(gearSetIds[0]);

        var gearSetListNode = addon->GetComponentListById(7);
        if (gearSetListNode is null) return;

        var len = gearSetListNode->ListLength;
        var foo = 1;
    }

    private void DetachNodes(AddonGearSetList* addon) { }

    private void RefreshNodes(AddonGearSetList* addon) {
        //throw new NotImplementedException();
    }
}
