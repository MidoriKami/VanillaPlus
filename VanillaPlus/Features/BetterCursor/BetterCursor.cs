using System.Drawing;
using System.Numerics;
using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using KamiToolKit.Nodes;
using KamiToolKit.Timelines;
using VanillaPlus.Classes;
using VanillaPlus.NativeElements.Config;

namespace VanillaPlus.Features.BetterCursor;

public unsafe class BetterCursor : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Better Cursor",
        Description = "Draws a ring around the cursor to make it easier to see",
        Authors = ["MidoriKami"],
        Type = ModificationType.UserInterface,
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
            new ChangeLogInfo(2, "Reduced Animation Speed to 1Hz"),
            new ChangeLogInfo(3, "Added options to only show in duties and/or combat"),
        ],
    };

    private ResNode? animationContainer;
    private IconImageNode? imageNode;

    private AddonController<AtkUnitBase>? screenTextController;

    private BetterCursorConfig? config;
    private ConfigAddon? configWindow;

    public override string ImageName => "BetterCursor.png";

    public override void OnEnable() {
        config = BetterCursorConfig.Load();

        configWindow = new ConfigAddon {
            InternalName = "BetterCursorConfig",
            Title = "Better Cursor Config",
            Config = config,
        };

        configWindow.AddCategory("Style")
            .AddColorEdit("Color", nameof(config.Color), KnownColor.White.Vector())
            .AddInputFloat("Size", 16, 16..512, nameof(config.Size));

        configWindow.AddCategory("Functions")
            .AddCheckbox("Enable Animation", nameof(config.Animations))
            .AddCheckbox("Hide on Left-Hold or Right-Hold", nameof(config.HideOnCameraMove));
        
        configWindow.AddCategory("Visibility")
            .AddCheckbox("Only show in Combat", nameof(config.OnlyShowInCombat))
            .AddCheckbox("Only Show in Duties", nameof(config.OnlyShowInDuties));

        configWindow.AddCategory("Icon Selection")
            .AddSelectIcon("Icon", nameof(config.IconId));

        config.OnSave += UpdateNodeConfig;

        OpenConfigAction = configWindow.Toggle;

        screenTextController = new AddonController<AtkUnitBase>("_ScreenText");
        screenTextController.OnAttach += AttachNodes;
        screenTextController.OnDetach += DetachNodes;
        screenTextController.OnUpdate += Update;
        screenTextController.Enable();
    }

    public override void OnDisable() {
        screenTextController?.Dispose();
        screenTextController = null;
        
        configWindow?.Dispose();
        configWindow = null;
        
        config = null;
    }

    private void UpdateNodeConfig() {
        if (config is null) return;
        
        if (animationContainer is not null) {
            animationContainer.Size = new Vector2(config.Size);
        }

        if (imageNode is not null) {
            imageNode.Size = new Vector2(config.Size);
            imageNode.Origin = new Vector2(config.Size / 2.0f);
            imageNode.Color = config.Color;
            imageNode.IconId = config.IconId;
        }

        animationContainer?.Timeline?.PlayAnimation(config.Animations ? 1 : 2);
    }

    private void Update(AtkUnitBase* addon) {
        if (config is null) return;

        if (animationContainer is not null && imageNode is not null) {
            ref var cursorData = ref UIInputData.Instance()->CursorInputs;
            animationContainer.Position = new Vector2(cursorData.PositionX, cursorData.PositionY) - imageNode.Size / 2.0f;

            var isLeftHeld = (cursorData.MouseButtonHeldFlags & MouseButtonFlags.LBUTTON) != 0;
            var isRightHeld = (cursorData.MouseButtonHeldFlags & MouseButtonFlags.RBUTTON) != 0;

            if (config is { OnlyShowInCombat: true } or { OnlyShowInDuties: true }) {
                var shouldShow = true;
                shouldShow &= !config.OnlyShowInCombat || Services.Condition.IsInCombat();
                shouldShow &= !config.OnlyShowInDuties || Services.Condition.IsBoundByDuty();
                shouldShow &= !config.HideOnCameraMove || (!isLeftHeld && !isRightHeld);

                animationContainer.IsVisible = shouldShow;
            }
            else {
                animationContainer.IsVisible = !isLeftHeld && !isRightHeld || !config.HideOnCameraMove;
            }
        }
    }

    private void AttachNodes(AtkUnitBase* addon) {
        animationContainer = new ResNode();
        animationContainer.AttachNode(addon);

        imageNode = new IconImageNode {
            IconId = 60498,
            FitTexture = true,
        };
        imageNode.AttachNode(animationContainer);

        animationContainer.AddTimeline(new TimelineBuilder()
            .BeginFrameSet(1, 120)
            .AddLabel(1, 1, AtkTimelineJumpBehavior.Start, 0)
            .AddLabel(60, 0, AtkTimelineJumpBehavior.LoopForever, 1)
            .AddLabel(61, 2, AtkTimelineJumpBehavior.Start, 0)
            .AddLabel(120, 0, AtkTimelineJumpBehavior.LoopForever, 2)
            .EndFrameSet()
            .Build());

        imageNode.AddTimeline(new TimelineBuilder()
            .BeginFrameSet(1, 60)
            .AddFrame(1, scale: new Vector2(1.0f, 1.0f))
            .AddFrame(30, scale: new Vector2(0.75f, 0.75f))
            .AddFrame(60, scale: new Vector2(1.0f, 1.0f))
            .EndFrameSet()
            .BeginFrameSet(61, 120)
            .AddFrame(61, scale: new Vector2(1.0f, 1.0f))
            .EndFrameSet()
            .Build());

        UpdateNodeConfig();
    }

    private void DetachNodes(AtkUnitBase* addon) {
        animationContainer?.Dispose();
        animationContainer = null;

        imageNode?.Dispose();
        imageNode = null;
    }
}
