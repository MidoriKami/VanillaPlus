using System.Numerics;
using Dalamud.Utility.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;

namespace VanillaPlus.Features.CosmicExplorationProgressWindow.Nodes;

public sealed unsafe partial class WksWindowNode : WindowNodeBase {
    private const float HorizontalPadding = 12.0f;
    private const float VerticalPadding = 30.0f;

    private readonly NineGridNode backgroundGlowNode;
    private readonly NineGridNode backgroundNode;
    private readonly NineGridNode borderNode;
    private readonly ImageNode bottomTextureNode;

    private readonly TextureButtonNode closeButtonNode;

    private readonly NineGridNode scanlineNode;
    private readonly ImageNode starshipImageNode;
    private readonly ImageNode topTextureNode;

    private readonly TextNode vanillaPlusLabel;

    public WksWindowNode() {
        Component->ShowFlags = 18;

        var baseSize = new Vector2(320.0f, 290.0f);
        Size = baseSize;

        var contentSize = baseSize - 2 * BorderThickness;
        var windowCollisionNode = new CollisionNode {
            Uses = 2,
            Size = new Vector2(320.0f, 48.0f),
            Position = Vector2.Zero,
            NodeFlags = NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.HasCollision | NodeFlags.RespondToMouse | NodeFlags.EmitsEvents | NodeFlags.Focusable,
        };
        windowCollisionNode.AttachNode(this);

        backgroundNode = new SimpleNineGridNode {
            Position = BorderThickness,
            Size = contentSize,
            TextureCoordinates = Vector2.Zero,
            TextureSize = new Vector2(48.0f, 130.0f),
            Offsets = new Vector4(16.0f),
            PartsRenderType = 0x60,
            TexturePath = "ui/uld/WKSWindow.tex",
        };
        backgroundNode.AttachNode(this);

        vanillaPlusLabel = new TextNode {
            Position = new Vector2(30.0f, 40.0f),
            FontSize = 23,
            FontType = FontType.TrumpGothic,
            TextColor = ColorHelper.GetColor(1),
            TextOutlineColor = ColorHelper.GetColor(7),
            String = "VanillaPlus",
        };
        vanillaPlusLabel.AttachNode(this);

        backgroundGlowNode = new SimpleNineGridNode {
            Position = BorderThickness,
            Size = contentSize,
            TextureCoordinates = new Vector2(48.0f, 0.0f),
            TextureSize = new Vector2(48.0f, 130.0f),
            Offsets = new Vector4(16.0f),
            PartsRenderType = 0x6C,
            TexturePath = "ui/uld/WKSWindow.tex",
        };
        backgroundGlowNode.AttachNode(this);

        starshipImageNode = new SimpleImageNode {
            TexturePath = "ui/uld/WKSWindowEff2.tex",
            WrapMode = WrapMode.Stretch,
            Position = new Vector2(25.0f, 19.0f),
            Scale = new Vector2(0.6f, 0.6f),
            Origin = new Vector2(290.0f, 250.0f),
            Size = new Vector2(290.0f, 250.0f),
            TextureSize = new Vector2(290.0f, 250.0f),
            Alpha = 0.15f,
        };
        starshipImageNode.AttachNode(this);

        scanlineNode = new NineGridNode {
            Position = BorderThickness,
            Size = contentSize,
            PartsRenderType = 0x6F,
            Parts = [
                new Part { TextureCoordinates = new Vector2(0.0f, 0.0f), Size = new Vector2(32.0f, 32.0f), Id = 0, TexturePath = "ui/uld/WKSHudLine_Corner.tex" },
                new Part { TextureCoordinates = new Vector2(0.0f, 0.0f), Size = new Vector2(32.0f, 32.0f), Id = 1, TexturePath = "ui/uld/WKSHudLine_BgH.tex" },
                new Part { TextureCoordinates = new Vector2(32.0f, 0.0f), Size = new Vector2(32.0f, 32.0f), Id = 2, TexturePath = "ui/uld/WKSHudLine_Corner.tex" },
                new Part { TextureCoordinates = new Vector2(0.0f, 0.0f), Size = new Vector2(32.0f, 32.0f), Id = 3, TexturePath = "ui/uld/WKSHudLine_BgV.tex" },
                new Part { TextureCoordinates = new Vector2(0.0f, 0.0f), Size = new Vector2(32.0f, 32.0f), Id = 4, TexturePath = "ui/uld/WKSHudLine_BgHV.tex" },
                new Part { TextureCoordinates = new Vector2(32.0f, 0.0f), Size = new Vector2(32.0f, 32.0f), Id = 5, TexturePath = "ui/uld/WKSHudLine_BgV.tex" },
                new Part { TextureCoordinates = new Vector2(0.0f, 32.0f), Size = new Vector2(32.0f, 32.0f), Id = 6, TexturePath = "ui/uld/WKSHudLine_Corner.tex" },
                new Part { TextureCoordinates = new Vector2(0.0f, 32.0f), Size = new Vector2(32.0f, 32.0f), Id = 7, TexturePath = "ui/uld/WKSHudLine_BgH.tex" },
                new Part { TextureCoordinates = new Vector2(32.0f, 32.0f), Size = new Vector2(32.0f, 32.0f), Id = 8, TexturePath = "ui/uld/WKSHudLine_Corner.tex" },
            ],
        };
        scanlineNode.AttachNode(this);

        bottomTextureNode = new SimpleImageNode {
            TexturePath = "ui/uld/WKSWindow.tex",
            WrapMode = WrapMode.Stretch,
            Position = new Vector2(22.0f, 129.0f),
            Size = new Vector2(228.0f, 126.0f),
            TextureCoordinates = new Vector2(96.0f, 0.0f),
            TextureSize = new Vector2(228.0f, 126.0f),
            ImageNodeFlags = ImageNodeFlags.FlipH | ImageNodeFlags.FlipV,
            MultiplyColor = new Vector3(90.0f / 255.0f),
            Alpha = 127.0f / 255.0f,
        };
        bottomTextureNode.AttachNode(this);

        topTextureNode = new SimpleImageNode {
            TexturePath = "ui/uld/WKSWindow.tex",
            WrapMode = WrapMode.Stretch,
            Position = new Vector2(70.0f, 42.0f),
            Size = new Vector2(228.0f, 126.0f),
            TextureCoordinates = new Vector2(96.0f, 0.0f),
            TextureSize = new Vector2(228.0f, 126.0f),
            MultiplyColor = new Vector3(90.0f / 255.0f),
            Alpha = 127.0f / 255.0f,
        };
        topTextureNode.AttachNode(this);

        borderNode = new SimpleNineGridNode {
            Position = Vector2.Zero,
            TextureCoordinates = Vector2.Zero,
            TextureSize = new Vector2(178.0f, 192.0f),
            Size = baseSize,
            Offsets = new Vector4(120.0f, 78.0f, 80.0f, 80.0f),
            PartsRenderType = 0x60,
            TexturePath = "ui/uld/WKSWindowFrame.tex",
        };
        borderNode.AttachNode(this);

        var borderTopBarNode = new SimpleNineGridNode {
            Position = new Vector2(45.0f, 5.0f),
            TextureCoordinates = Vector2.Zero,
            TextureSize = new Vector2(16.0f, 14.0f),
            Size = new Vector2(230.0f, 14.0f),
            TexturePath = "ui/uld/WKSWindowFrame2.tex",
            PartsRenderType = 2,
        };
        borderTopBarNode.AttachNode(this);

        WindowHeaderFocusNode = new ResNode {
            NodeId = 113,
            Size = new Vector2(284.0f, 38.0f),
            Position = new Vector2(18.0f, 24.0f),
            AddColor = new Vector3(15.0f / 255.0f),
            NodeFlags = NodeFlags.Focusable | NodeFlags.Visible,
        };
        WindowHeaderFocusNode.AttachNode(this);

        closeButtonNode = new TextureButtonNode {
            NodeId = 114,
            Size = new Vector2(28.0f, 28.0f),
            Position = new Vector2(247.0f, 6.0f),
            TextureCoordinates = new Vector2(0.0f, 0.0f),
            TextureSize = new Vector2(28.0f, 28.0f),
        };
        closeButtonNode.ImageNode.LoadTexture("ui/uld/WindowA_Button.tex", false);
        closeButtonNode.AttachNode(WindowHeaderFocusNode);

        var grabHandleNode = new SimpleNineGridNode {
            Position = new Vector2(87.0f, 11.0f),
            Size = new Vector2(110.0f, 10.0f),
            TexturePath = "ui/uld/WKSHud.tex",
            TextureCoordinates = new Vector2(184.0f, 112.0f),
            TextureSize = new Vector2(6.0f, 10.0f),
            Offsets = new Vector4(0.0f, 0.0f, 2.0f, 2.0f),
            PartsRenderType = 0x74,
        };
        grabHandleNode.AttachNode(WindowHeaderFocusNode);

        Data->ShowCloseButton = 1;
        Data->ShowConfigButton = 0;
        Data->ShowHelpButton = 0;
        Data->ShowHeader = 1;

        Data->Nodes[0] = 0;
        Data->Nodes[1] = 0;
        Data->Nodes[2] = closeButtonNode.NodeId;
        Data->Nodes[3] = 0;
        Data->Nodes[4] = 0;
        Data->Nodes[5] = WindowHeaderFocusNode.NodeId;
        Data->Nodes[6] = 0;
        Data->Nodes[7] = 0;

        AddNodeFlags(NodeFlags.Visible, NodeFlags.Enabled, NodeFlags.EmitsEvents);

        LoadTimelines();

        InitializeComponentEvents();
    }

    private static Vector2 BorderThickness => new(17.0f, 22.0f);
    private static Vector2 ContentPadding => new(HorizontalPadding, VerticalPadding);

    public override Vector2 ContentSize => Size - (2 * (BorderThickness + ContentPadding)) - Vector2.Zero.WithY(20);
    public override Vector2 ContentStartPosition => BorderThickness + ContentPadding + Vector2.Zero.WithY(20);
    public override float HeaderHeight => WindowHeaderFocusNode.Height;
    public override ResNode WindowHeaderFocusNode { get; }

    public override void SetTitle(string title, string? subtitle = null) { }
}
