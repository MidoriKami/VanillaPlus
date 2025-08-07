using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Classes.TimelineBuilding;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;
using VanillaPlus.Extensions;
using VanillaPlus.Features.BetterCursor;

namespace VanillaPlus.BetterCursor;

public unsafe class BetterCursor : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Better Cursor",
        Description = "Draws a ring around the cursor to make it easier to see",
        Authors = ["MidoriKami"],
        Type = ModificationType.UserInterface,
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    private ResNode? animationContainer;
    private IconImageNode? imageNode;

    private AddonController<AtkUnitBase> screenTextController = null!;

    private BetterCursorConfig config = null!;
    private BetterCursorConfigWindow configWindow = null!;

    public override string ImageName => "BetterCursor.png";

    public override void OnEnable() {
        config = BetterCursorConfig.Load();
        configWindow = new BetterCursorConfigWindow(config, UpdateNodeConfig);
        configWindow.AddToWindowSystem();
        OpenConfigAction = configWindow.Toggle;

        screenTextController = new AddonController<AtkUnitBase>("_ScreenText");
        screenTextController.OnAttach += AttachNodes;
        screenTextController.OnDetach += DetachNodes;
        screenTextController.OnUpdate += Update;
        screenTextController.Enable();
    }

    public override void OnDisable() {
        screenTextController.Dispose();
        configWindow.RemoveFromWindowSystem();
    }

    private void UpdateNodeConfig() {
        if (animationContainer is not null) {
            animationContainer.Size = new Vector2(config.Size);
        }

        if (imageNode is not null) {
            imageNode.Size = new Vector2(config.Size);
            imageNode.Origin = new Vector2(config.Size / 2.0f);
            imageNode.Color = config.Color;
        }

        animationContainer?.Timeline?.PlayAnimation(config.Animations ? 1 : 2);
    }

    private void Update(AtkUnitBase* addon) {
        if (animationContainer is not null && imageNode is not null) {
            ref var cursorData = ref UIInputData.Instance()->CursorInputs;
            animationContainer.Position = new Vector2(cursorData.PositionX, cursorData.PositionY) - imageNode.Size / 2.0f;

            var isLeftHeld = (cursorData.MouseButtonHeldFlags & MouseButtonFlags.LBUTTON) != 0;
            var isRightHeld = (cursorData.MouseButtonHeldFlags & MouseButtonFlags.RBUTTON) != 0;

            animationContainer.IsVisible = !isLeftHeld && !isRightHeld || !config.HideOnCameraMove;
        }
    }

    private void AttachNodes(AtkUnitBase* addon) {
        animationContainer = new ResNode {
            IsVisible = true,
        };
        System.NativeController.AttachNode(animationContainer, addon->RootNode);

        imageNode = new IconImageNode {
            IconId = 60498,
            IsVisible = true,
        };
        System.NativeController.AttachNode(imageNode, animationContainer);

        animationContainer.AddTimeline(new TimelineBuilder()
            .BeginFrameSet(1, 60)
            .AddLabel(1, 1, AtkTimelineJumpBehavior.Start, 0)
            .AddLabel(30, 0, AtkTimelineJumpBehavior.LoopForever, 1)
            .AddLabel(31, 2, AtkTimelineJumpBehavior.Start, 0)
            .AddLabel(60, 0, AtkTimelineJumpBehavior.LoopForever, 2)
            .EndFrameSet()
            .Build());

        imageNode.AddTimeline(new TimelineBuilder()
            .BeginFrameSet(1, 30)
            .AddFrame(1, scale: new Vector2(1.0f, 1.0f))
            .AddFrame(15, scale: new Vector2(0.75f, 0.75f))
            .AddFrame(30, scale: new Vector2(1.0f, 1.0f))
            .EndFrameSet()
            .BeginFrameSet(31, 60)
            .AddFrame(31, scale: new Vector2(1.0f, 1.0f))
            .EndFrameSet()
            .Build());

        UpdateNodeConfig();
    }

    private void DetachNodes(AtkUnitBase* addon) {
        System.NativeController.DisposeNode(ref animationContainer);
        System.NativeController.DisposeNode(ref imageNode);
    }
}
