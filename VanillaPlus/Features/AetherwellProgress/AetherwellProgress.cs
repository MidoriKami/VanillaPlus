using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Controllers;
using KamiToolKit.Premade.Node.Simple;
using KamiToolKit.Timelines;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.AetherwellProgress;

public unsafe class AetherwellProgress : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_AetherwellProgress,
        Description = Strings.ModificationDescription_AetherwellProgress,
        Type = ModificationType.UserInterface,
        Authors = ["salanth357"],
    };

    public override string ImageName => "AetherwellProgress.png";

    private readonly Vector2 baseUV = new(0, 276);
    private readonly Vector2 baseSize = new(92, 92);
    private readonly Vector2 basePosition = new(26, 26);
    private readonly List<uint> buttonIDs = [2, 3, 4, 5];

    private AddonController? addonController;
    private readonly List<SimpleImageNode> nodes = [];

    public override void OnEnable() {
        addonController = new AddonController {
            AddonName = "MKDRelicGrowth",
            OnSetup = OnAddonSetup,
            OnRefresh = OnAddonRefresh,
            OnFinalize = OnAddonFinalize,
        };
        addonController.Enable();
    }

    public override void OnDisable() {
        addonController?.Dispose();
        addonController = null;
        // the nodes list is cleared by addonController.OnFinalize, so we can ignore it here
    }

    private void OnAddonSetup(AtkUnitBase* addon) {
        foreach (var buttonId in buttonIDs) {
            var btn = addon->GetComponentButtonById(buttonId);
            if (btn == null) return;
            SetupNode(btn);
        }
    }

    private void SetupNode(AtkComponentButton* btn) {
        var existingImageNode = btn->GetImageNodeById(17);
        if (existingImageNode == null) return;
        existingImageNode->SetWidth(0);
        existingImageNode->SetHeight(0);

        var imageNode = new SimpleImageNode {
            Position = basePosition,
            Size = baseSize,
            Origin = baseSize/2,
            TextureCoordinates = baseUV,
            TexturePath = "ui/uld/MKDRelicGrowth2.tex",
            TextureSize = baseSize,
        };
        imageNode.AddTimeline(AnimationTimeline());

        imageNode.AttachNode(existingImageNode, NodePosition.AfterTarget);
        nodes.Add(imageNode);
    }

    private void OnAddonRefresh(AtkUnitBase* addon) {
        foreach (var (i, node) in nodes.Index()) {
            if (!long.TryParse(addon->AtkValuesSpan[i * 4].String.ToString(), out var progress))
                continue;
            if (!long.TryParse(addon->AtkValuesSpan[(i * 4) + 1].String.ToString(), out var max))
                continue;
            var pct = float.Clamp((float)progress / max, 0, 1);
            var position = basePosition;
            var size = baseSize with { Y = float.Floor(baseSize.Y * pct) };
            var shift = baseSize.Y - size.Y;
            var uv = baseUV with { Y = baseUV.Y + shift };

            position.Y += shift;
            node.Position = position;
            node.Size = size;
            node.TextureCoordinates = uv;
            node.TextureSize = size;
        }
    }

    private void OnAddonFinalize(AtkUnitBase* addon) {
        foreach (var n in nodes) {
            n.DetachNode();
            n.Dispose();
        }
        nodes.Clear();

        foreach (var buttonId in buttonIDs) {
            var btn = addon->GetComponentButtonById(buttonId);
            if (btn == null) continue;
            RestoreNode(btn);
        }
    }

    private void RestoreNode(AtkComponentButton* btn) {
        var imageNode = btn->GetImageNodeById(17);
        if (imageNode == null) return;
        imageNode->SetWidth((ushort)baseSize.X);
        imageNode->SetHeight((ushort)baseSize.Y);
    }

    private static Timeline AnimationTimeline() {
        return new TimelineBuilder()
            .BeginFrameSet(1, 20)
            .AddFrame(1, alpha: 0)
            .AddFrame(1, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(21, 90)
            .AddFrame(21, alpha: 0)
            .AddFrame(51, alpha: 127)
            .AddFrame(90, alpha: 0)
            .AddFrame(21, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .AddFrame(51, addColor: new Vector3(30, 30, 30), multiplyColor: new Vector3(100, 100, 100))
            .AddFrame(90, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(91, 140)
            .AddFrame(91, alpha: 51)
            .AddFrame(104, alpha: 127)
            .AddFrame(125, alpha: 127)
            .AddFrame(140, alpha: 51)
            .AddFrame(91, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .AddFrame(104, addColor: new Vector3(50, 50, 50), multiplyColor: new Vector3(100, 100, 100))
            .AddFrame(125, addColor: new Vector3(50, 50, 50), multiplyColor: new Vector3(100, 100, 100))
            .AddFrame(140, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(141, 175)
            .AddFrame(141, alpha: 76)
            .AddFrame(154, alpha: 178)
            .AddFrame(175, alpha: 76)
            .AddFrame(141, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .AddFrame(154, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .AddFrame(175, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(176, 203)
            .AddFrame(176, alpha: 127)
            .AddFrame(189, alpha: 255)
            .AddFrame(203, alpha: 127)
            .AddFrame(176, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .AddFrame(189, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .AddFrame(203, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .Build();
    }
}
