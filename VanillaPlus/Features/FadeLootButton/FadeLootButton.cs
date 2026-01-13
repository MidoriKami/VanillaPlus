using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using VanillaPlus.Classes;
using VanillaPlus.NativeElements.Config;

namespace VanillaPlus.Features.FadeLootButton;

public unsafe class FadeLootButton : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_FadeLootButton,
        Description = Strings.ModificationDescription_FadeLootButton,
        Type = ModificationType.UserInterface,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    public override string ImageName => "FadeLootButton.png";

    private AddonController? notificationLootController;
    private FadeLootButtonConfig? config;
    private ConfigAddon? configWindow;
    
    public override void OnEnable() {
        config = FadeLootButtonConfig.Load();

        configWindow = new ConfigAddon {
            Size = new Vector2(400.0f, 125.0f),
            InternalName = "FadeLootConfig",
            Title = Strings.FadeLootButton_ConfigTitle,
            Config = config,
        };

        configWindow.AddCategory(Strings.FadeLootButton_CategoryStyleSettings)
            .AddFloatSlider(Strings.FadeLootButton_LabelFadePercentage, 0.0f, 1.0f, 2, 0.05f, nameof(config.FadePercent));

        OpenConfigAction = configWindow.Toggle;
        
        notificationLootController = new AddonController("_NotificationLoot");
        notificationLootController.OnUpdate += OnLootRefresh;
        notificationLootController.OnDetach += OnLootDisable;
        notificationLootController.Enable();
    }

    private static void OnLootDisable(AtkUnitBase* addon) {
        if (addon->RootNode is null) return;

        addon->RootNode->Color.A = 255;
    }

    public override void OnDisable() {
        notificationLootController?.Dispose();
        notificationLootController = null;

        config = null;
        
        configWindow?.Dispose();
        configWindow = null;
    }
    
    private void OnLootRefresh(AtkUnitBase* addon) {
        if (config is null) return;
        if (addon->RootNode is null) return;

        if (AllLootRolled()) {
            addon->RootNode->Color.A = (byte)(255 * (1.0f - config.FadePercent));
        }
        else {
            addon->RootNode->Color.A = 255;
        }
    }

    private static bool AllLootRolled() {
        foreach (ref var lootItem in Loot.Instance()->Items) {
            if (lootItem is { ItemId: not 0, RollState: not RollState.Rolled }) {
                return false;
            }
        }

        return true;
    }
}
