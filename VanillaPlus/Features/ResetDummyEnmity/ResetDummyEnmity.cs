using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Arrays;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.ResetDummyEnmity;

public class ResetDummyEnmity : GameModification {
    private const int MaxEnemyCount = 8;
    private const uint StrikingDummyNameId = 541;
    private const float ButtonSize = 30.0f;

    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_ResetDummyEnmity,
        Description = Strings.ModificationDescription_ResetDummyEnmity,
        Type = ModificationType.UserInterface,
        Authors = ["Zeffuro"],
    };

    public override string ImageName => "ResetDummyEnmity.png";

    private AddonController? enemyListController;
    private readonly CircleButtonNode?[] resetButtons = new CircleButtonNode?[MaxEnemyCount];
    private readonly IBattleChara?[] buttonTargets = new IBattleChara?[MaxEnemyCount];

    public override async Task OnEnableAsync() {
        unsafe {
            enemyListController = new AddonController {
                AddonName = "_EnemyList",
                OnSetup = SetupEnemyList,
                OnUpdate = UpdateEnemyList,
                OnFinalize = FinalizeEnemyList,
            };
        }

        await Services.Framework.Run(enemyListController.Enable);
    }

    public override async Task OnDisableAsync() {
        await Services.Framework.Run(() => {
            enemyListController?.Dispose();
            DisposeButtons();
        });

        enemyListController = null;
    }

    private unsafe void SetupEnemyList(AtkUnitBase* addon) {
        var enemyRowNodes = GetEnemyRowNodes(addon);

        for (var index = 0; index < resetButtons.Length; index++) {
            if (resetButtons[index] is not null) continue;

            var enemyRowNode = (AtkResNode*)enemyRowNodes[index];
            if (enemyRowNode is null) continue;

            var button = new CircleButtonNode {
                Icon = ButtonIcon.Refresh,
                Size = new Vector2(ButtonSize),
                Position = new Vector2(
                    enemyRowNode->X + enemyRowNode->Width - ButtonSize,
                    enemyRowNode->Y + ((enemyRowNode->Height - ButtonSize) / 2.0f)
                ),
                TextTooltip = Strings.ResetDummyEnmity_ResetEnmityToolTip,
                IsVisible = false,
            };

            var buttonIndex = index;
            button.OnClick = () => ResetDummy(buttonIndex);

            button.AttachNode(addon);

            resetButtons[index] = button;
        }
    }

    private unsafe void UpdateEnemyList(AtkUnitBase* addon) {
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

    private unsafe void FinalizeEnemyList(AtkUnitBase* addon) => DisposeButtons();

    private void DisposeButtons() {
        for (var index = 0; index < resetButtons.Length; index++) {
            resetButtons[index]?.Dispose();
            resetButtons[index] = null;
            buttonTargets[index] = null;
        }
    }

    private void ResetDummy(int buttonIndex) {
        var dummy = buttonTargets[buttonIndex];
        if (dummy is not { NameId: StrikingDummyNameId }) return;
        if (!dummy.IsValid()) return;
        if (!Services.Condition[ConditionFlag.InCombat]) return;

        GameMain.ExecuteCommand(319, (int)dummy.GameObjectId);
    }

    private static IBattleChara? GetDummyByEntityId(uint entityId)
        => Services.ObjectTable.CharacterManagerObjects.FirstOrDefault(obj => obj.EntityId == entityId) as IBattleChara;

    private static unsafe nint[] GetEnemyRowNodes(AtkUnitBase* addon) {
        var enemyRowNodes = new nint[MaxEnemyCount];

        for (var i = 0; i < MaxEnemyCount; i++) {
            uint nodeId = (uint)(i == 0 ? 2 : 20000 + i);
            var node = addon->GetNodeById(nodeId);

            if (node != null) {
                enemyRowNodes[i] = (nint)node;
            }
        }

        return enemyRowNodes;
    }
}
