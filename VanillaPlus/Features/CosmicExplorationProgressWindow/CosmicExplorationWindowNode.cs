using System.Numerics;
using Dalamud.Utility.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using KamiToolKit.Timelines;

namespace VanillaPlus.Features.CosmicExplorationProgressWindow;

public sealed unsafe class CosmicExplorationWindowNode : WindowNodeBase {
    public const float HorizontalPadding = 12.0f;
    public const float VerticalPadding = 30.0f;

    private const string WindowPath = "ui/uld/WKSWindow.tex";
    private const string WindowEff2Path = "ui/uld/WKSWindowEff2.tex";
    private const string WindowScanlinePathPrefix = "ui/uld/WKSHudLine";
    private const string WindowFramePath = "ui/uld/WKSWindowFrame.tex";
    private const string WindowFrame2Path = "ui/uld/WKSWindowFrame2.tex";
    private const string HudPath = "ui/uld/WKSHud.tex";
    public readonly SimpleNineGridNode BackgroundGlowNode;
    public readonly SimpleNineGridNode BackgroundNode;
    public readonly SimpleNineGridNode BorderNode;
    public readonly SimpleNineGridNode BorderTopBarNode;
    public readonly SimpleImageNode BottomTextureNode;

    public readonly TextureButtonNode CloseButtonNode;

    public readonly SimpleNineGridNode GrabHandleNode;

    public readonly CollisionNode HeaderCollisionNode;
    public readonly ResNode HeaderContainerNode;
    public readonly NineGridNode ScanlineNode;
    public readonly ImageNode StarshipImageNode;
    public readonly SimpleImageNode TopTextureNode;

    public readonly TextNode VanillaPlusLabel;

    public CosmicExplorationWindowNode() {
        Component->ShowFlags = 18;
        // Explicitly set our base size here so that when we lay all of this out, everything scales correctly
        var baseSize = new Vector2(320.0f, 280.0f);
        Size = baseSize;

        var contentSize = baseSize - (2 * BorderThickness);


        HeaderCollisionNode = new CollisionNode {
            Uses = 2,
            Size = new Vector2(320.0f, 48.0f),
            Position = Vector2.Zero,
            NodeFlags = NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.HasCollision | NodeFlags.RespondToMouse |
                        NodeFlags.EmitsEvents | NodeFlags.Focusable,
        };
        HeaderCollisionNode.AttachNode(this);

        BackgroundNode = new SimpleNineGridNode {
            Position = BorderThickness,
            Size = contentSize,
            TextureCoordinates = Vector2.Zero,
            TextureSize = new Vector2(48.0f, 130.0f),
            Offsets = new Vector4(16.0f),
            NodeFlags = NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents,
            PartsRenderType = 0x60,
            TexturePath = WindowPath,
        };
        BackgroundNode.AttachNode(this);

        VanillaPlusLabel = new TextNode {
            Position = new Vector2(36f, 44f),
            FontSize = 20,
            FontType = FontType.TrumpGothic,
            TextColor = Vector4.One.WithW(0.651f),
            TextOutlineColor = new Vector4(0.169f, 0.388f, 0.765f, 0.557f),
            TextFlags = TextFlags.Edge,
            String = "VanillaPlus",
            Alpha = 0.5f,
        };
        VanillaPlusLabel.AttachNode(this);

        BackgroundGlowNode = new SimpleNineGridNode {
            Position = BorderThickness,
            Size = contentSize,
            TextureCoordinates = new Vector2(48.0f, 0.0f),
            TextureSize = new Vector2(48.0f, 130.0f),
            Offsets = new Vector4(16.0f),
            NodeFlags = NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents,
            PartsRenderType = 0x6C,
            TexturePath = WindowPath,
        };
        BackgroundGlowNode.AttachNode(this);

        StarshipImageNode = new SimpleImageNode {
            TexturePath = WindowEff2Path,
            NodeFlags = NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents,
            WrapMode = WrapMode.Stretch,
            Position = new Vector2(25.0f, 19.0f),
            Scale = new Vector2(0.6f, 0.6f),
            Origin = new Vector2(290.0f, 250.0f),
            Size = new Vector2(290.0f, 250.0f),
            TextureCoordinates = Vector2.Zero,
            TextureSize = new Vector2(290.0f, 250.0f),
        };
        StarshipImageNode.AttachNode(this);
        StarshipImageNode.Node->SetAlpha(25);


        ScanlineNode = new NineGridNode {
            Position = BorderThickness,
            Size = contentSize,
            Offsets = Vector4.Zero,
            NodeFlags = NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents,
            PartsRenderType = 0x6F,
        };
        ScanlineNode.PartsList.Add(
            new Part {
                TextureCoordinates = new Vector2(0.0f, 0.0f), Size = new Vector2(32.0f, 32.0f), Id = 0,
                TexturePath = $"{WindowScanlinePathPrefix}_Corner.tex",
            },
            new Part {
                TextureCoordinates = new Vector2(0.0f, 0.0f), Size = new Vector2(32.0f, 32.0f), Id = 1,
                TexturePath = $"{WindowScanlinePathPrefix}_BgH.tex",
            },
            new Part {
                TextureCoordinates = new Vector2(32.0f, 0.0f), Size = new Vector2(32.0f, 32.0f), Id = 2,
                TexturePath = $"{WindowScanlinePathPrefix}_Corner.tex",
            },
            new Part {
                TextureCoordinates = new Vector2(0.0f, 0.0f), Size = new Vector2(32.0f, 32.0f), Id = 3,
                TexturePath = $"{WindowScanlinePathPrefix}_BgV.tex",
            },
            new Part {
                TextureCoordinates = new Vector2(0.0f, 0.0f), Size = new Vector2(32.0f, 32.0f), Id = 4,
                TexturePath = $"{WindowScanlinePathPrefix}_BgHV.tex",
            },
            new Part {
                TextureCoordinates = new Vector2(32.0f, 0.0f), Size = new Vector2(32.0f, 32.0f), Id = 5,
                TexturePath = $"{WindowScanlinePathPrefix}_BgV.tex",
            },
            new Part {
                TextureCoordinates = new Vector2(0.0f, 32.0f), Size = new Vector2(32.0f, 32.0f), Id = 6,
                TexturePath = $"{WindowScanlinePathPrefix}_Corner.tex",
            },
            new Part {
                TextureCoordinates = new Vector2(0.0f, 32.0f), Size = new Vector2(32.0f, 32.0f), Id = 7,
                TexturePath = $"{WindowScanlinePathPrefix}_BgH.tex",
            },
            new Part {
                TextureCoordinates = new Vector2(32.0f, 32.0f), Size = new Vector2(32.0f, 32.0f), Id = 8,
                TexturePath = $"{WindowScanlinePathPrefix}_Corner.tex",
            });
        ScanlineNode.AttachNode(this);

        BottomTextureNode = new SimpleImageNode {
            TexturePath = WindowPath,
            NodeFlags = NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents,
            WrapMode = WrapMode.Stretch,
            Position = new Vector2(22.0f, 129.0f),
            Size = new Vector2(228.0f, 126.0f),
            TextureCoordinates = new Vector2(96.0f, 0.0f),
            TextureSize = new Vector2(228.0f, 126.0f),
            ImageNodeFlags = ImageNodeFlags.FlipH | ImageNodeFlags.FlipV,
            MultiplyColor = new Vector3(90.0f / 255.0f),
            Alpha = 127.0f / 255.0f,
        };
        BottomTextureNode.AttachNode(this);

        TopTextureNode = new SimpleImageNode {
            TexturePath = WindowPath,
            NodeFlags = NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents,
            WrapMode = WrapMode.Stretch,
            Position = new Vector2(70.0f, 42.0f),
            Size = new Vector2(228.0f, 126.0f),
            TextureCoordinates = new Vector2(96.0f, 0.0f),
            TextureSize = new Vector2(228.0f, 126.0f),
            MultiplyColor = new Vector3(90.0f / 255.0f),
            Alpha = 127.0f / 255.0f,
        };
        TopTextureNode.AttachNode(this);

        BorderNode = new SimpleNineGridNode {
            Position = Vector2.Zero,
            TextureCoordinates = Vector2.Zero,
            TextureSize = new Vector2(178.0f, 192.0f),
            Size = baseSize,
            Offsets = new Vector4(120.0f, 78.0f, 80.0f, 80.0f),
            NodeFlags = NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents,
            PartsRenderType = 0x60,
            TexturePath = WindowFramePath,
        };
        BorderNode.AttachNode(this);

        BorderTopBarNode = new SimpleNineGridNode {
            Position = new Vector2(45.0f, 5.0f),
            TextureCoordinates = Vector2.Zero,
            TextureSize = new Vector2(16.0f, 14.0f),
            Size = new Vector2(230.0f, 14.0f),
            TexturePath = WindowFrame2Path,
            PartsRenderType = 2,
        };
        BorderTopBarNode.AttachNode(this);

        HeaderContainerNode = new ResNode {
            NodeId = 113,
            Size = new Vector2(284.0f, 38.0f),
            Position = new Vector2(18.0f, 24.0f),
            AddColor = new Vector3(15.0f / 255.0f),
        };
        HeaderContainerNode.AttachNode(this);

        CloseButtonNode = new TextureButtonNode {
            NodeId = 114,
            Size = new Vector2(28.0f, 28.0f),
            Position = new Vector2(247.0f, 6.0f),
            NodeFlags = NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents,
            TextureCoordinates = new Vector2(0.0f, 0.0f),
            TextureSize = new Vector2(28.0f, 28.0f),
        };
        CloseButtonNode.ImageNode.LoadTexture("ui/uld/WindowA_Button.tex", false);
        CloseButtonNode.AttachNode(HeaderContainerNode);

        GrabHandleNode = new SimpleNineGridNode {
            Position = new Vector2(87.0f, 11.0f),
            Size = new Vector2(110.0f, 10.0f),
            TexturePath = HudPath,
            TextureCoordinates = new Vector2(184.0f, 112.0f),
            TextureSize = new Vector2(6.0f, 10.0f),
            Offsets = new Vector4(0.0f, 0.0f, 2.0f, 2.0f),
            PartsRenderType = 0x74,
        };
        GrabHandleNode.AttachNode(HeaderContainerNode);

        Data->ShowCloseButton = 1;
        Data->ShowConfigButton = 0;
        Data->ShowHelpButton = 0;
        Data->ShowHeader = 1;
        Data->Nodes[0] = 0;
        Data->Nodes[1] = 0;
        Data->Nodes[2] = CloseButtonNode.NodeId;
        Data->Nodes[3] = 0;
        Data->Nodes[4] = 0;
        Data->Nodes[5] = HeaderContainerNode.NodeId;
        Data->Nodes[6] = 0;
        Data->Nodes[7] = 0;

        AddNodeFlags(NodeFlags.Visible, NodeFlags.Enabled, NodeFlags.EmitsEvents);

        LoadTimelines();

        InitializeComponentEvents();
    }

    public static Vector2 BorderThickness => new(17.0f, 22.0f);
    public static Vector2 ContentPadding => new(HorizontalPadding, VerticalPadding);


    public override Vector2 ContentSize => Size - (2 * (BorderThickness + ContentPadding)) - Vector2.Zero.WithY(20);
    public override Vector2 ContentStartPosition => BorderThickness + ContentPadding + Vector2.Zero.WithY(20);
    public override float HeaderHeight => HeaderContainerNode.Height;
    public override ResNode WindowHeaderFocusNode => HeaderContainerNode;

    public static Vector2 WindowSizeForContentSize(Vector2 contentSize) {
        return contentSize + (2 * (BorderThickness + ContentPadding)) + Vector2.Zero.WithY(8);
    }

    public override void SetTitle(string title, string? subtitle = null) { }

    private void LoadTimelines() {
        AddTimeline(new TimelineBuilder()
            .BeginFrameSet(1, 29)
            .AddLabel(1, 17, AtkTimelineJumpBehavior.Start, 0)
            .AddLabel(9, 0, AtkTimelineJumpBehavior.PlayOnce, 0)
            .AddLabel(10, 18, AtkTimelineJumpBehavior.Start, 0)
            .AddLabel(19, 0, AtkTimelineJumpBehavior.PlayOnce, 0)
            .AddLabel(20, 7, AtkTimelineJumpBehavior.Start, 0)
            .AddLabel(29, 0, AtkTimelineJumpBehavior.PlayOnce, 0)
            .EndFrameSet()
            .Build()
        );

        BackgroundNode.AddTimeline(new TimelineBuilder()
            .BeginFrameSet(1, 9)
            .AddFrame(1, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(10, 19)
            .AddFrame(10, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .AddFrame(12, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(20, 29)
            .AddFrame(20, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(50, 50, 50))
            .EndFrameSet()
            .Build()
        );

        BackgroundGlowNode.AddTimeline(new TimelineBuilder()
            .BeginFrameSet(1, 9)
            .AddFrame(1, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(90, 90, 90))
            .EndFrameSet()
            .BeginFrameSet(10, 19)
            .AddFrame(10, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .AddFrame(12, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(20, 29)
            .AddFrame(20, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(50, 50, 50))
            .EndFrameSet()
            .Build()
        );
        StarshipImageNode.AddTimeline(new TimelineBuilder()
            .BeginFrameSet(1, 9)
            .AddFrame(1, addColor: new Vector3(100, 100, 100), multiplyColor: new Vector3(90, 90, 90))
            .EndFrameSet()
            .BeginFrameSet(10, 19)
            .AddFrame(10, addColor: new Vector3(100, 100, 100), multiplyColor: new Vector3(100, 100, 100))
            .AddFrame(12, addColor: new Vector3(100, 100, 100), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(20, 29)
            .AddFrame(20, addColor: new Vector3(100, 100, 100), multiplyColor: new Vector3(50, 50, 50))
            .EndFrameSet()
            .Build()
        );
        ScanlineNode.AddTimeline(new TimelineBuilder()
            .BeginFrameSet(1, 9)
            .AddFrame(1, alpha: 25)
            .EndFrameSet()
            .BeginFrameSet(10, 19)
            .AddFrame(10, alpha: 51)
            .AddFrame(12, alpha: 51)
            .EndFrameSet()
            .BeginFrameSet(20, 29)
            .AddFrame(20, alpha: 51)
            .EndFrameSet()
            .Build()
        );
        BottomTextureNode.AddTimeline(new TimelineBuilder()
            .BeginFrameSet(1, 9)
            .AddFrame(1, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(90, 90, 90))
            .EndFrameSet()
            .BeginFrameSet(10, 19)
            .AddFrame(10, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .AddFrame(12, addColor: new Vector3(15, 15, 15), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(20, 29)
            .AddFrame(20, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(50, 50, 50))
            .EndFrameSet()
            .Build()
        );
        TopTextureNode.AddTimeline(new TimelineBuilder()
            .BeginFrameSet(1, 9)
            .AddFrame(1, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(90, 90, 90))
            .EndFrameSet()
            .BeginFrameSet(10, 19)
            .AddFrame(10, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .AddFrame(12, addColor: new Vector3(15, 15, 15), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(20, 29)
            .AddFrame(20, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(50, 50, 50))
            .EndFrameSet()
            .Build()
        );
        BorderNode.AddTimeline(new TimelineBuilder()
            .BeginFrameSet(1, 9)
            .AddFrame(1, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(80, 80, 80))
            .EndFrameSet()
            .BeginFrameSet(10, 19)
            .AddFrame(10, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(80, 80, 80))
            .AddFrame(12, addColor: new Vector3(15, 15, 15), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(20, 29)
            .AddFrame(20, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(50, 50, 50))
            .EndFrameSet()
            .Build()
        );

        HeaderContainerNode.AddTimeline(new TimelineBuilder()
            .BeginFrameSet(1, 9)
            .AddFrame(1, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(90, 90, 90))
            .EndFrameSet()
            .BeginFrameSet(10, 19)
            .AddFrame(10, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .AddFrame(12, addColor: new Vector3(15, 15, 15), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(20, 29)
            .AddFrame(20, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(50, 50, 50))
            .EndFrameSet()
            .Build()
        );

        VanillaPlusLabel.AddTimeline(new TimelineBuilder()
            .BeginFrameSet(1, 9)
            .AddFrame(1, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(90, 90, 90))
            .EndFrameSet()
            .BeginFrameSet(10, 19)
            .AddFrame(10, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .AddFrame(12, addColor: new Vector3(15, 15, 15), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(20, 29)
            .AddFrame(20, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(50, 50, 50))
            .EndFrameSet()
            .Build()
        );

        CloseButtonNode.AddTimeline(new TimelineBuilder()
            .BeginFrameSet(1, 59)
            .AddLabel(1, 1, AtkTimelineJumpBehavior.Start, 0)
            .AddLabel(9, 0, AtkTimelineJumpBehavior.PlayOnce, 0)
            .AddLabel(10, 2, AtkTimelineJumpBehavior.Start, 0)
            .AddLabel(19, 0, AtkTimelineJumpBehavior.PlayOnce, 0)
            .AddLabel(20, 3, AtkTimelineJumpBehavior.Start, 0)
            .AddLabel(29, 0, AtkTimelineJumpBehavior.PlayOnce, 0)
            .AddLabel(30, 7, AtkTimelineJumpBehavior.Start, 0)
            .AddLabel(39, 0, AtkTimelineJumpBehavior.PlayOnce, 0)
            .AddLabel(40, 6, AtkTimelineJumpBehavior.Start, 0)
            .AddLabel(49, 0, AtkTimelineJumpBehavior.PlayOnce, 0)
            .AddLabel(50, 4, AtkTimelineJumpBehavior.Start, 0)
            .AddLabel(59, 0, AtkTimelineJumpBehavior.PlayOnce, 0)
            .EndFrameSet()
            .Build()
        );
    }
}
