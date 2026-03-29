using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.UI;
using KamiToolKit.Controllers;

namespace VanillaPlus.Features.AprilFools;

/// <summary>
/// Swaps the position of the panels in the Character window to have the player model on the left and the stats on the right.
/// This one actually looks really nice so may become a permanent feature.
/// </summary>
public unsafe class BetterCharacterPanelFools : FoolsModule {
    private AddonController<AddonCharacter>? characterController;

    public override bool IsEnabledByConfig 
        => Config.BetterCharacterPanel;

    protected override void OnEnable() {
        characterController = new AddonController<AddonCharacter>("Character");
        characterController.OnAttach += OnCharacterAttach;
        characterController.OnPreUpdate += OnCharacterUpdate;
        characterController.OnDetach += OnCharacterDetach;
        characterController.Enable();
    }

    protected override void OnDisable() {
        characterController?.Dispose();
        characterController = null;
    }
    
    private static void OnCharacterAttach(AddonCharacter* addonCharacter) {
        var characterNode = addonCharacter->GetNodeById(10);
        if (characterNode is null) return;

        characterNode->Position -= new Vector2(375.0f, 0.0f);
    }

    private static void OnCharacterUpdate(AddonCharacter* addonCharacter) {
        foreach (var child in addonCharacter->AddonControl.ChildAddons) {
            if (child.Value is null) continue;

            child.Value->PositionX = (short) 356.0f;
        }
    }

    private static void OnCharacterDetach(AddonCharacter* addonCharacter) {
        var characterNode = addonCharacter->GetNodeById(10);
        if (characterNode is null) return;

        characterNode->Position += new Vector2(375.0f, 0.0f);
    }
}
