using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using KamiToolKit.Controllers;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.ReverseCharacterPanel;

public class ReverseCharacterPanel : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_ReverseCharacterPanel,
        Description = Strings.ModificationDescription_ReverseCharacterPanel,
        Type = ModificationType.UserInterface,
        Authors = ["MidoriKami"],
    };

    public override string ImageName => "ReverseCharacterPanel.png";

    private AddonController<AddonCharacter>? characterController;

    public override async Task OnEnableAsync() {
        unsafe {
            characterController = new AddonController<AddonCharacter> {
                AddonName = "Character",
                OnSetup = SetupCharacter,
                OnPreUpdate = UpdateCharacter,
                OnFinalize = FinalizeCharacter,
            };
        }

        await IFramework.Get().RunSafely(characterController.Enable);
    }

    public override async Task OnDisableAsync() {
        await IFramework.Get().RunSafely(() => characterController?.Dispose());
        characterController = null;
    }

    private static unsafe void SetupCharacter(AddonCharacter* addonCharacter) {
        var characterNode = addonCharacter->GetNodeById(10);
        if (characterNode is null) return;

        characterNode->Position -= new Vector2(380.0f, 0.0f);
    }

    private static unsafe void UpdateCharacter(AddonCharacter* addonCharacter) {
        foreach (var child in addonCharacter->AddonControl.ChildAddons) {
            if (child.Value is null) continue;

            child.Value->PositionX = (short)(addonCharacter->AtkUnitBase.Size.X - 386.0f);
        }
    }

    private static unsafe void FinalizeCharacter(AddonCharacter* addonCharacter) {
        var characterNode = addonCharacter->GetNodeById(10);
        if (characterNode is null) return;

        characterNode->Position += new Vector2(380.0f, 0.0f);
    }
}
