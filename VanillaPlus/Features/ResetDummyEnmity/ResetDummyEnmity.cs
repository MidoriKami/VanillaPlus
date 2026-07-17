using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Arrays;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using KamiToolKit.Enums;
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

    private const int MaxEnemyCount = 8;
    private const uint StrikingDummyNameId = 541;
    private const float ButtonSize = 30.0f;

    private AddonController<AddonEnemyList>? enemyListController;

    private CircleButtonNode?[]? resetButtons;
    private IBattleChara?[]? buttonTargets;

    public override async Task OnEnableAsync() {
        unsafe {
            enemyListController = new AddonController<AddonEnemyList> {
                AddonName = "_EnemyList",
                OnSetup = SetupEnemyList,
                OnUpdate = UpdateEnemyList,
                OnFinalize = FinalizeEnemyList,
            };
        }

        resetButtons = new CircleButtonNode?[MaxEnemyCount];
        buttonTargets = new IBattleChara?[MaxEnemyCount];

        await IFramework.Get().RunSafely(enemyListController.Enable);
    }

    public override async Task OnDisableAsync() {
        await IFramework.Get().RunSafely(() => {
            enemyListController?.Dispose();
        });

        enemyListController = null;

        resetButtons = null;
        buttonTargets = null;
    }

    private unsafe void SetupEnemyList(AddonEnemyList* addon) {
        if (resetButtons is null) return;

        foreach (uint i in Enumerable.Range(0, MaxEnemyCount)) {
            if (resetButtons[i] is not null) continue;

            var duplicatedNode = (AtkComponentNode*)addon->UldManager.GetDuplicatedNode(2, i, 0);
            if (duplicatedNode is null) continue;

            var button = new CircleButtonNode {
                Icon = CircleButtonIcon.Refresh,
                Size = new Vector2(ButtonSize),
                Position = new Vector2(
                    duplicatedNode->X + duplicatedNode->Width - ButtonSize,
                    duplicatedNode->Y + (duplicatedNode->Height - ButtonSize) / 2.0f
                ),
                TextTooltip = Strings.ResetDummyEnmity_ResetEnmityToolTip,
                IsVisible = false,
                IsEnabled = false,
                OnClick = () => ResetDummy(i),
            };
            button.AttachNode(&addon->AtkUnitBase);

            resetButtons[i] = button;
        }
    }

    private unsafe void UpdateEnemyList(AddonEnemyList* addon) {
        if (resetButtons is null) return;
        if (buttonTargets is null) return;

        var enemyListData = EnemyListNumberArray.Instance();
        var enemyCount = enemyListData is not null ? enemyListData->EnemyCount : 0;

        foreach (var index in Enumerable.Range(0, resetButtons.Length)) {
            var resetButton = resetButtons[index];
            if (resetButton is null) continue;

            ref var entry = ref enemyListData->Enemies[index];

            var wasVisible = resetButton.IsVisible;
            var shouldBeVisible = false;

            buttonTargets[index] = null;

            if (index < enemyCount && entry.ActiveInList) {
                var matchingDummy = GetDummyByEntityId((uint)entry.EntityId);
                if (matchingDummy is { NameId: StrikingDummyNameId }) {
                    shouldBeVisible = ICondition.Get()[ConditionFlag.InCombat];
                    buttonTargets[index] = matchingDummy;
                }
            }

            if (wasVisible && !shouldBeVisible) {
                resetButton.HideTooltip();
            }

            resetButton.IsEnabled = TargetMatchesEntityId((uint)entry.EntityId);
            resetButton.IsVisible = shouldBeVisible;
        }
    }

    private unsafe void FinalizeEnemyList(AddonEnemyList* _) {
        if (resetButtons == null) return;
        if (buttonTargets == null) return;

        for (var index = 0; index < resetButtons.Length; index++) {
            resetButtons[index]?.Dispose();
            resetButtons[index] = null;
            buttonTargets[index] = null;
        }
    }

    private void ResetDummy(uint buttonIndex) {
        var dummy = buttonTargets?[buttonIndex];
        if (dummy is not { NameId: StrikingDummyNameId }) return;
        if (!dummy.IsValid()) return;
        if (!ICondition.Get()[ConditionFlag.InCombat]) return;
        if (!TargetMatchesEntityId(dummy.EntityId)) return;

        GameMain.ExecuteCommand(319, (int)dummy.GameObjectId);
    }

    private static IBattleChara? GetDummyByEntityId(uint entityId)
        => IObjectTable.Get().CharacterManagerObjects.FirstOrDefault(obj => obj.EntityId == entityId) as IBattleChara;

    private static bool TargetMatchesEntityId(uint entityId) =>
        ITargetManager.Get().GetTarget() is { } target && target.EntityId == entityId;
}
