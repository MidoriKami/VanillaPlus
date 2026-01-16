using System.Collections.Generic;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Controllers;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.HUDCoordinates;

public unsafe class HUDCoordinates : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_HUDCoordinates,
        Description = Strings.ModificationDescription_HUDCoordinates,
        Type = ModificationType.UserInterface,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    public override string ImageName => "HUDCoordinates.png";

    private AddonController? hudLayoutScreenController;

    private List<TextNode>? textNodes;

    public override void OnEnable() {
        textNodes = [];
        
        hudLayoutScreenController = new AddonController("_HudLayoutScreen");

        hudLayoutScreenController.OnAttach += addon => {
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
        };

        hudLayoutScreenController.OnRefresh += addon => {
            foreach (var node in addon->UldManager.Nodes) {
                if (node.Value is null) continue;
                if (node.Value->GetNodeType() is not NodeType.Component) continue;
                var componentNode = (AtkComponentNode*)node.Value;
                
                var textNode = componentNode->Component->GetTextNodeById(100);
                if (textNode is null) continue;
                
                var textNodeSizeOffset = new Vector2(node.Value->Width, node.Value->Height) / 2.0f - new Vector2(90.0f, 22.0f) / 2.0f;
                var textNodeCenter = new Vector2(node.Value->X, node.Value->Y) + new Vector2(node.Value->Width, node.Value->Height) / 2.0f;
                
                textNode->SetPositionFloat(textNodeSizeOffset.X, textNodeSizeOffset.Y);
                textNode->SetText(textNodeCenter.ToString());
            }
        };

        hudLayoutScreenController.OnDetach += _ => {
            foreach (var node in textNodes) {
                node.Dispose();
            }
            
            textNodes.Clear();
        };
        
        hudLayoutScreenController.Enable();
    }

    public override void OnDisable() {
        hudLayoutScreenController?.Dispose();
        hudLayoutScreenController = null;
        
        textNodes?.Clear();
        textNodes = null;
    }
}
