using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.UI;
using KamiToolKit.Controllers;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.ReverseCharacterPanel;

public unsafe class ReverseCharacterPanel : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_ReverseCharacterPanel,
        Description = Strings.ModificationDescription_ReverseCharacterPanel,
        Type = ModificationType.UserInterface,
        Authors = [ "MidoriKami" ],
    };

    public override string ImageName => "ReverseCharacterPanel.png";

    private AddonController<AddonCharacter>? characterController;

    public override void OnEnable() {
        characterController = new AddonController<AddonCharacter> {
            AddonName = "Character",
            OnSetup = SetupCharacter,
            OnPreUpdate = UpdateCharacter,
            OnFinalize = FinalizeCharacter,
        };
        characterController.Enable();
    }

    public override void OnDisable() {
        characterController?.Dispose();
        characterController = null;
    }

    private static void SetupCharacter(AddonCharacter* addonCharacter) {
        var characterNode = addonCharacter->GetNodeById(10);
        if (characterNode is null) return;

        characterNode->Position -= new Vector2(380.0f, 0.0f);
    }

    private static void UpdateCharacter(AddonCharacter* addonCharacter) {
        foreach (var child in addonCharacter->AddonControl.ChildAddons) {
            if (child.Value is null) continue;

            child.Value->PositionX = (short) (addonCharacter->AtkUnitBase.Size.X - 386.0f);
        }
    }

    private static void FinalizeCharacter(AddonCharacter* addonCharacter) {
        var characterNode = addonCharacter->GetNodeById(10);
        if (characterNode is null) return;

        characterNode->Position += new Vector2(380.0f, 0.0f);
    }
}
