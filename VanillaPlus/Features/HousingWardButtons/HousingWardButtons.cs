using System.Numerics;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes.Controllers;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.HousingWardButtons;

public unsafe class HousingWardButtons : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Housing Ward Buttons",
        Description = "Adds previous and next ward buttons to the housing selection screen.",
        Type = ModificationType.UserInterface,
        Authors = [ "Zeffuro" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    // public override string ImageName => "SampleGameModification.png";

    // public override bool IsExperimental => true;

    private AddonController? housingAddonController;
    private CircleButtonNode? previousWardButtonNode;
    private CircleButtonNode? nextWardButtonNode;

    public override void OnEnable() {
        housingAddonController = new AddonController("HousingSelectBlock");
        housingAddonController.OnAttach += AttachNodes;
        housingAddonController.OnDetach += DetachNodes;
        housingAddonController.Enable();

        Services.AddonLifecycle.RegisterListener(AddonEvent.PostRefresh, "HousingSelectBlock", OnHousingRefresh);
    }

    private void AttachNodes(AtkUnitBase* addon) {
        if (addon is null) return;
        var buttonContainerNode = addon->GetNodeById(2);
        if (buttonContainerNode is null) return;

        buttonContainerNode->SetHeight(350);
        var buttonY = buttonContainerNode->Height - 30.0f;
        previousWardButtonNode = new CircleButtonNode {
            Position = new Vector2(0.0f, buttonY),
            Size = new Vector2(26.0f, 26.0f),
            Icon = ButtonIcon.LeftArrow,
            OnClick = NextWard,
        };
        previousWardButtonNode.AttachNode(buttonContainerNode);

        nextWardButtonNode = new CircleButtonNode {
            Position = new Vector2(32.0f, buttonY),
            Size = new Vector2(26.0f, 26.0f),
            Icon = ButtonIcon.RightArrow,
            OnClick = PreviousWard,
        };
        nextWardButtonNode.AttachNode(buttonContainerNode);
    }

    private void DetachNodes(AtkUnitBase* addon) {
        if (addon is null) return;
        var buttonContainerNode = addon->GetNodeById(2);
        if (buttonContainerNode is null) return;

        buttonContainerNode->SetHeight(316);

        if (previousWardButtonNode is null) return;
        if (nextWardButtonNode is null) return;

        previousWardButtonNode.Dispose();
        previousWardButtonNode = null;

        nextWardButtonNode.Dispose();
        nextWardButtonNode = null;
    }

    private void PreviousWard() {
        Services.PluginLog.Debug("Previous Ward");
    }

    private void NextWard() {
        Services.PluginLog.Debug("Next Ward");
    }

    private void OnHousingRefresh(AddonEvent type, AddonArgs args) {
        if (args is not AddonRefreshArgs refreshArgs) return;
    }

    public override void OnDisable() {
        Services.AddonLifecycle.UnregisterListener(OnHousingRefresh);
        housingAddonController?.Dispose();
        housingAddonController = null;
    }
}
