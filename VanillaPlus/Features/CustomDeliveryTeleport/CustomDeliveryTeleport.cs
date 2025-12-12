using Dalamud.Game.Gui.ContextMenu;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.CustomDeliveryTeleport;

public unsafe class CustomDeliveryTeleport : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Custom Delivery Teleport",
        Description = "Adds an option to the custom delivery list to teleport to the nearest aetheryte.",
        Type = ModificationType.UserInterface,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    // public override string ImageName => "SampleGameModification.png";

    // public override bool IsExperimental => true;

    public override void OnEnable() {
        // SatisfactionList
        Services.ContextMenu.OnMenuOpened += OnContextMenuOpened;
        Services.AddonLifecycle.LogAddon("SatisfactionList");

        // step 1: on context menu appear, determine which "SatisfactionNpc" was selected

        // step 2: get the Level data for that npc

        // step 3: search aetherytes for closest, probably using Dalamud's Aetheryte List to only include unlocked aetherytes
        // note: be sure to re-init the list before checking

        // step 4: add context menu entry

        // step 5: profit?
    }
    
    public override void OnDisable() {
        Services.ContextMenu.OnMenuOpened -= OnContextMenuOpened;
        Services.AddonLifecycle.UnLogAddon("SatisfactionList");
    }

    private void OnContextMenuOpened(IMenuOpenedArgs args) {
        if (args.AddonName is not "SatisfactionList") return;

        
        
        var data = true; // Breakpoint Here
        data = false;
        
        args.AddMenuItem(new MenuItem {
            Name = "Teleport",
            OnClicked = OnTeleportClicked,
        });
    }

    private void OnTeleportClicked(IMenuItemClickedArgs args) {
        
    }
}
