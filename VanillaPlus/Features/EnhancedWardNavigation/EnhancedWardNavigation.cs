using System;
using System.Numerics;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.EnhancedWardNavigation;

public unsafe class EnhancedWardNavigation : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Enhanced Ward Navigation",
        Description = "Adds previous and next buttons to the housing ward selection list.",
        Type = ModificationType.UserInterface,
        Authors = [ "Zeffuro" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    public override string ImageName => "EnhancedWardNavigation.png";

    private AddonController? housingAddonController;
    private TextButtonNode? previousWardButtonNode;
    private TextButtonNode? nextWardButtonNode;
    private int currentWard;

    public override void OnEnable() {
        housingAddonController = new AddonController("HousingSelectBlock");
        housingAddonController.OnAttach += AttachNodes;
        housingAddonController.OnDetach += DetachNodes;
        housingAddonController.Enable();

        Services.AddonLifecycle.RegisterListener(AddonEvent.PreRefresh, "HousingSelectBlock", OnHousingRefresh);
    }

    private void AttachNodes(AtkUnitBase* addon) {
        if (addon->RootNode is null) return;

        var selectButton = addon->GetNodeById(34);
        if (selectButton is null) return;

        var buttonY = selectButton->Y - 30.0f;
        var buttonX = selectButton->X;

        currentWard = addon->AtkValuesSpan[1].Int;

        previousWardButtonNode = new TextButtonNode {
            Position = new Vector2(buttonX, buttonY),
            Size = new Vector2(56.0f, 28.0f),
            String = "Prev",
            OnClick = () => SetCurrentWard(),
            IsEnabled = currentWard > 0,
        };
        previousWardButtonNode.AttachNode(addon->RootNode);

        nextWardButtonNode = new TextButtonNode {
            Position = new Vector2(buttonX + 60.0f, buttonY),
            Size = new Vector2(56.0f, 28.0f),
            String = "Next",
            OnClick = () => SetCurrentWard(true),
            IsEnabled = currentWard < 29,
        };
        nextWardButtonNode.AttachNode(addon->RootNode);
    }

    private void DetachNodes(AtkUnitBase* addon) {
        if (addon is null) return;
        if (addon->RootNode is null) return;
        if (previousWardButtonNode is null) return;
        if (nextWardButtonNode is null) return;

        previousWardButtonNode.Dispose();
        previousWardButtonNode = null;

        nextWardButtonNode.Dispose();
        nextWardButtonNode = null;
    }

    private void OnHousingRefresh(AddonEvent type, AddonArgs args) {
        if (args is not AddonRefreshArgs refreshArgs) return;
        if (refreshArgs.ValueSpan.Length < 2) return;

        var eventKind = args.ValueSpan[0].UInt;
        var newWard = args.ValueSpan[1].Int;

        currentWard = newWard;
        ToggleButtons(eventKind is 4);
    }

    private void SetCurrentWard(bool isNext = false) {
        var destinationWard = Math.Clamp(currentWard + (isNext ? 1 : -1), 0, 29);
        ToggleButtons(false);
        AgentHousingPortal.Instance()->AgentInterface.SendCommand(1, [1, destinationWard]);
    }

    private void ToggleButtons(bool enabled) {
        if (previousWardButtonNode is null) return;
        if (nextWardButtonNode is null) return;

        var previousEnabled = enabled && currentWard > 0;
        var nextEnabled = enabled && currentWard < 29;

        previousWardButtonNode?.IsEnabled = previousEnabled;
        nextWardButtonNode?.IsEnabled = nextEnabled;
    }

    public override void OnDisable() {
        Services.AddonLifecycle.UnregisterListener(OnHousingRefresh);
        housingAddonController?.Dispose();
        housingAddonController = null;
    }
}
