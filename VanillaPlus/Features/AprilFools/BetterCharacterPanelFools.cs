using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.UI;
using KamiToolKit.Controllers;

namespace VanillaPlus.Features.AprilFools;

public unsafe class BetterCharacterPanelFools : IFoolsModule {
    public required AprilFoolsConfig Config { get; set; }

    private AddonController<AddonCharacter>? characterController;
    
    public void Enable() {
        characterController = new AddonController<AddonCharacter>("Character");
        characterController.OnAttach += OnCharacterAttach;
        characterController.OnPreUpdate += OnCharacterUpdate;
        characterController.OnDetach += OnCharacterDetach;
        characterController.Enable();
    }

    public void Disable() {
        characterController?.Dispose();
        characterController = null;
    }
    
    private void OnCharacterAttach(AddonCharacter* addonCharacter) {
        if (!Config.BetterCharacterPanel) return;
        
        var characterNode = addonCharacter->GetNodeById(10);
        if (characterNode is null) return;

        characterNode->Position -= new Vector2(375.0f, 0.0f);
    }

    private void OnCharacterUpdate(AddonCharacter* addonCharacter) {
        if (!Config.BetterCharacterPanel) return;
        
        var childAddons = addonCharacter->AddonControl.ChildAddons;

        foreach (var child in childAddons) {
            if (child.Value is null) continue;

            child.Value->PositionX = (short) 356.0f;
        }
    }

    private void OnCharacterDetach(AddonCharacter* addonCharacter) {
        if (!Config.BetterCharacterPanel) return;

        var characterNode = addonCharacter->GetNodeById(10);
        if (characterNode is null) return;

        characterNode->Position += new Vector2(375.0f, 0.0f);
    }
}
