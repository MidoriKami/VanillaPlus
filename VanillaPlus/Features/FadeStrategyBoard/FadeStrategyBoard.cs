using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using VanillaPlus.Classes;
using VanillaPlus.NativeElements.Config;

namespace VanillaPlus.Features.FadeStrategyBoard;

public unsafe class FadeStrategyBoard : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_FadeStrategyBoard,
        Description = Strings.ModificationDescription_FadeStrategyBoard,
        Type = ModificationType.GameBehavior,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    public override string ImageName => "FadeStrategyBoard.png";
    
    private AddonController? notificationStrategyBoardController;
    private FadeStrategyBoardConfig? config;
    private ConfigAddon? configWindow;

    public override void OnEnable() {
        config = FadeStrategyBoardConfig.Load();
        
        configWindow = new ConfigAddon {
            Size = new Vector2(400.0f, 125.0f),
            InternalName = "FadeStrategyBoardConfig",
            Title = Strings.FadeStrategyBoard_ConfigTitle,
            Config = config,
        };

        configWindow.AddCategory(Strings.FadeStrategyBoard_CategoryStyleSettings)
            .AddFloatSlider(Strings.FadeStrategyBoard_LabelFadePercentage, 0.0f, 1.0f, 2, 0.05f, nameof(config.FadePercentage));

        OpenConfigAction = configWindow.Toggle;
        
        notificationStrategyBoardController = new AddonController("_NotificationTestOver32");
        notificationStrategyBoardController.OnUpdate += OnStrategyBoardRefresh;
        notificationStrategyBoardController.OnDetach += OnStrategyBoardDisable;
        notificationStrategyBoardController.Enable();
    }

    public override void OnDisable() {
        notificationStrategyBoardController?.Dispose();
        notificationStrategyBoardController = null;
        
        config = null;
        
        configWindow?.Dispose();
        configWindow = null;
    }
    
    private static void OnStrategyBoardDisable(AtkUnitBase* addon) {
        if (addon->RootNode is null) return;

        addon->RootNode->Color.A = 255;
    }
    
    private void OnStrategyBoardRefresh(AtkUnitBase* addon) {
        if (config is null) return;
        if (addon->RootNode is null) return;

        addon->RootNode->Color.A = (byte)(255 * (1.0f - config.FadePercentage));
    }
}
