using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Classes.TimelineBuilding;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;
using VanillaPlus.Extensions;

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

    private AddonController<AtkUnitBase> screenTextController = null!;
    private ResNode? animationContainer;
    private IconImageNode? imageNode;
    
    private BetterCursorConfig config = null!;
    private BetterCursorConfigWindow configWindow = null!;

    public override string ImageName => "BetterCursor.png";

    public override void OnEnable() {
        config = BetterCursorConfig.Load();
        configWindow = new BetterCursorConfigWindow(config, OnColorChanged, OnAnimationToggled, OnSizeChanged);
        configWindow.AddToWindowSystem();
        OpenConfigAction = configWindow.Toggle;
        
        screenTextController = new AddonController<AtkUnitBase>(Services.PluginInterface, "_ScreenText");
        screenTextController.OnAttach += AttachNodes;
        screenTextController.OnDetach += DetachNodes;
        screenTextController.OnUpdate += Update;
        screenTextController.Enable();
    }

    public override void OnDisable() {
        screenTextController.Dispose();
        configWindow.RemoveFromWindowSystem();
    }
    
    private void OnSizeChanged() {
        if (animationContainer is not null) {
            animationContainer.Size = new Vector2(config.Size);
        }

        if (imageNode is not null) {
            imageNode.Size = new Vector2(config.Size);
            imageNode.Origin = new Vector2(config.Size / 2.0f);
        }
    }

    private void OnAnimationToggled()
        => animationContainer?.Timeline?.PlayAnimation(config.Animations ? 1 : 2);

    private void OnColorChanged() {
        if (imageNode is not null) {
            imageNode.Color = config.Color;
        }
    }

    private void Update(AtkUnitBase* addon) {
        if (animationContainer is not null && imageNode is not null) {
            ref var cursorData = ref UIInputData.Instance()->CursorInputs;
            animationContainer.Position = new Vector2(cursorData.PositionX, cursorData.PositionY) - imageNode.Size / 2.0f;

            var isLeftHeld = (cursorData.MouseButtonHeldFlags & MouseButtonFlags.LBUTTON) != 0;
            var isRightHeld = (cursorData.MouseButtonHeldFlags & MouseButtonFlags.RBUTTON) != 0;
            
            animationContainer.IsVisible = !isLeftHeld && !isRightHeld;
        }
    }

    private void AttachNodes(AtkUnitBase* addon) {
        animationContainer = new ResNode {
            Size = new Vector2(config.Size),
            IsVisible = true,
        };
        System.NativeController.AttachNode(animationContainer, addon->RootNode);
        
        imageNode = new IconImageNode {
            Size = new Vector2(config.Size),
            Origin = new Vector2(config.Size / 2.0f),
            Color = config.Color,
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
              .AddFrame(1, scale: new Vector2(1.0f, 1.0f))
              .EndFrameSet()
              .Build());
        
        animationContainer.Timeline?.PlayAnimation(config.Animations ? 1 : 2);
    }

    private void DetachNodes(AtkUnitBase* addon) {
        System.NativeController.DetachNode(animationContainer, () => {
            animationContainer?.Dispose();
            animationContainer = null;
        });
        
        System.NativeController.DetachNode(imageNode, () => {
            imageNode?.Dispose();
            imageNode = null;
        });
    }
}
