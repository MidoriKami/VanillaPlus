using System.Numerics;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.NativeElements.Config;

namespace VanillaPlus.Features.FadeLootButton;

public class FadeLootButton : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_FadeLootButton,
        Description = Strings.ModificationDescription_FadeLootButton,
        Type = ModificationType.UserInterface,
        Authors = ["MidoriKami"],
    };

    public override string ImageName => "FadeLootButton.png";

    private AddonController? notificationLootController;
    private FadeLootButtonConfig? config;
    private ConfigAddon? configWindow;

    public override async Task OnEnableAsync() {
        config = await FadeLootButtonConfig.Load();

        configWindow = new ConfigAddon {
            Size = new Vector2(400.0f, 125.0f),
            InternalName = "FadeLootConfig",
            Title = Strings.FadeLootButton_ConfigTitle,
            Config = config,
        };

        configWindow.AddCategory(Strings.FadeLootButton_CategoryStyleSettings)
            .AddFloatSlider(Strings.FadeLootButton_LabelFadePercentage, 0.0f, 1.0f, 2, 0.05f, nameof(config.FadePercent));

        OpenConfigAsync = configWindow.ToggleAsync;

        unsafe {
            notificationLootController = new AddonController {
                AddonName = "_NotificationLoot",
                OnUpdate = UpdateNotificationLoot,
                OnFinalize = FinalizeNotificationLoot,
            };
        }

        await Services.Framework.Run(notificationLootController.Enable);
    }

    public override async Task OnDisableAsync() {
        await Services.Framework.Run(() => notificationLootController?.Dispose());
        notificationLootController = null;

        await Task.WhenAll(configWindow?.DisposeAsync().AsTask() ?? Task.CompletedTask);
        configWindow = null;

        config = null;
    }

    private unsafe void UpdateNotificationLoot(AtkUnitBase* addon) {
        if (config is null) return;
        if (addon->RootNode is null) return;

        if (AllLootRolled()) {
            addon->RootNode->Color.A = (byte)(255 * (1.0f - config.FadePercent));
        }
        else {
            addon->RootNode->Color.A = 255;
        }
    }

    private static unsafe void FinalizeNotificationLoot(AtkUnitBase* addon) {
        if (addon->RootNode is null) return;

        addon->RootNode->Color.A = 255;
    }

    private static unsafe bool AllLootRolled() {
        foreach (ref var lootItem in Loot.Instance()->Items) {
            if (lootItem is { ItemId: not 0, RollState: not RollState.Rolled }) {
                return false;
            }
        }

        return true;
    }
}
