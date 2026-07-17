using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.HUDCoordinates;

public class HUDCoordinates : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_HUDCoordinates,
        Description = Strings.ModificationDescription_HUDCoordinates,
        Type = ModificationType.UserInterface,
        Authors = ["MidoriKami"],
    };

    public override string ImageName => "HUDCoordinates.png";

    private AddonController? hudLayoutScreenController;

    private List<TextNode>? textNodes;

    public override async Task OnEnableAsync() {
        textNodes = [];

        unsafe {
            hudLayoutScreenController = new AddonController {
                AddonName = "_HudLayoutScreen",
                OnSetup = HudLayoutSetup,
                OnRefresh = HudLayoutRefresh,
                OnFinalize = HudLayoutFinalize,
            };
        }

        await Services.GetService<IFramework>().RunSafely(hudLayoutScreenController.Enable);
    }

    public override async Task OnDisableAsync() {
        await Services.GetService<IFramework>().RunSafely(() => hudLayoutScreenController?.Dispose());
        hudLayoutScreenController = null;

        textNodes?.Clear();
        textNodes = null;
    }

    private unsafe void HudLayoutSetup(AtkUnitBase* addon) {
        textNodes = [];

        foreach (var node in addon->UldManager.Nodes) {
            if (node.Value is null) continue;
            if (node.Value->GetNodeType() is not NodeType.Component) continue;

            var newTextNode = new TextNode {
                NodeId = 100,
                Size = new Vector2(90.0f, 22.0f),
                Position = new Vector2(node.Value->Width / 2.0f, node.Value->Height / 2.0f) - new Vector2(90.0f, 22.0f) / 2.0f,
                String = new Vector2(node.Value->X, node.Value->Y).ToString(),
            };

            textNodes.Add(newTextNode);
            newTextNode.AttachNode(node.Value);
        }
    }

    private static unsafe void HudLayoutRefresh(AtkUnitBase* addon) {
        foreach (var node in addon->UldManager.Nodes) {
            if (node.Value is null) continue;
            if (node.Value->GetNodeType() is not NodeType.Component) continue;
            var componentNode = (AtkComponentNode*)node.Value;

            var textNode = componentNode->Component->GetTextNodeById(100);
            if (textNode is null) continue;

            var textNodeSizeOffset = node.Value->Size / 2.0f - textNode->AtkResNode.Size / 2.0f;
            var textNodeCenter = node.Value->Position + node.Value->Size / 2.0f;

            textNode->SetPositionFloat(textNodeSizeOffset.X, textNodeSizeOffset.Y);
            textNode->SetText(textNodeCenter.ToString());
        }
    }

    private unsafe void HudLayoutFinalize(AtkUnitBase* _) {
        foreach (var node in textNodes ?? []) {
            node.Dispose();
        }

        textNodes?.Clear();
        textNodes = null;
    }
}
