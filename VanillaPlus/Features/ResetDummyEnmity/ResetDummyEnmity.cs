using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Arrays;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;
using KamiToolKit.Controllers;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.ResetDummyEnmity;

public class ResetDummyEnmity : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_ResetDummyEnmity,
        Description = Strings.ModificationDescription_ResetDummyEnmity,
        Type = ModificationType.UserInterface,
        Authors = ["Zeffuro"],
    };

    public override string ImageName => "ResetDummyEnmity.png";

    private AddonController? enemyListController;

    private const int MaxEnemyCount = 8;
    private const uint StrikingDummyNameId = 541;
    private const float ButtonSize = 30.0f;

    private CircleButtonNode?[]? resetButtons;
    private IBattleChara?[]? buttonTargets;

    public override async Task OnEnableAsync() {
        unsafe {
            enemyListController = new AddonController {
                AddonName = "_EnemyList",
                OnSetup = SetupEnemyList,
                OnUpdate = UpdateEnemyList,
                OnFinalize = FinalizeEnemyList,
            };
        }

        resetButtons = new CircleButtonNode?[MaxEnemyCount];
        buttonTargets = new IBattleChara?[MaxEnemyCount];

        await Services.Framework.Run(enemyListController.Enable);
    }

    public override async Task OnDisableAsync() {
        await Services.Framework.Run(() => {
            enemyListController?.Dispose();
        });

        enemyListController = null;

        resetButtons = null;
        buttonTargets = null;
    }

    private unsafe void SetupEnemyList(AtkUnitBase* addon) {
        if (resetButtons is null) return;
        if (addon == null) return;

        for (int i = 0; i < MaxEnemyCount; i++) {
            if (resetButtons[i] is not null) continue;

            var duplicatedNode = (AtkComponentNode*)addon->UldManager.GetDuplicatedNode(2, (uint)i, 0);
            if (duplicatedNode == null) continue;

            var button = new CircleButtonNode {
                Icon = ButtonIcon.Refresh,
                Size = new Vector2(ButtonSize),
                Position = new Vector2(
                    duplicatedNode->X + duplicatedNode->Width - ButtonSize,
                    duplicatedNode->Y + ((duplicatedNode->Height - ButtonSize) / 2.0f)
                ),
                TextTooltip = Strings.ResetDummyEnmity_ResetEnmityToolTip,
                IsVisible = false,
            };

            var buttonIndex = i;
            button.OnClick = () => ResetDummy(buttonIndex);

            button.AttachNode(addon);

            resetButtons[i] = button;
        }
    }

    private unsafe void UpdateEnemyList(AtkUnitBase* addon) {
        if (resetButtons == null) return;
        if (buttonTargets == null) return;

        var enemyListData = EnemyListNumberArray.Instance();
        var enemyCount = enemyListData is not null ? enemyListData->EnemyCount : 0;

        for (var index = 0; index < resetButtons.Length; index++) {
            var resetButton = resetButtons[index];
            if (resetButton is null) continue;

            var wasVisible = resetButton.IsVisible;
            var shouldBeVisible = false;
            buttonTargets[index] = null;

            if (index < enemyCount && enemyListData->Enemies[index].ActiveInList) {
                var matchingDummy = GetDummyByEntityId((uint)enemyListData->Enemies[index].EntityId);
                if (matchingDummy is { NameId: StrikingDummyNameId }) {
                    shouldBeVisible = Services.Condition[ConditionFlag.InCombat];
                    buttonTargets[index] = matchingDummy;
                }
            }

            if (wasVisible && !shouldBeVisible) {
                resetButton.HideTooltip();
            }

            resetButton.IsVisible = shouldBeVisible;
        }
    }

    private unsafe void FinalizeEnemyList(AtkUnitBase* addon) {
        if (resetButtons == null) return;
        if (buttonTargets == null) return;

        for (var index = 0; index < resetButtons.Length; index++) {
            resetButtons[index]?.Dispose();
            resetButtons[index] = null;
            buttonTargets[index] = null;
        }
    }

    private void ResetDummy(int buttonIndex) {
        var dummy = buttonTargets?[buttonIndex];
        if (dummy is not { NameId: StrikingDummyNameId }) return;
        if (!dummy.IsValid()) return;
        if (!Services.Condition[ConditionFlag.InCombat]) return;

        GameMain.ExecuteCommand(319, (int)dummy.GameObjectId);
    }

    private static IBattleChara? GetDummyByEntityId(uint entityId)
        => Services.ObjectTable.CharacterManagerObjects.FirstOrDefault(obj => obj.EntityId == entityId) as IBattleChara;
}
