using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.MacroLineNumbers;

public unsafe class MacroLineNumbers : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Macro Line Numbers",
        Description = "Adds line numbers to the User Macros window.",
        Type = ModificationType.UserInterface,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "InitialChangelog"),
        ],
    };

    public override string ImageName => "MacroLineNumbers.png";

    private AddonController? macroAddonController;

    private const float SizeOffset = 20.0f;

    private List<TextNode>? textNodes;

    public override void OnEnable() {
        textNodes = [];
        
        macroAddonController = new AddonController("Macro");

        macroAddonController.OnAttach += addon => {
            var textInputNode = addon->GetNodeById<AtkComponentNode>(119);
            if (textInputNode is null) return;

            var position = Vector2.Zero;
            textInputNode->GetPositionFloat(&position.X, &position.Y);
            textInputNode->SetPositionFloat(position.X + SizeOffset, position.Y);
            
            textInputNode->SetWidth((ushort)(textInputNode->GetWidth() - SizeOffset));

            foreach (var childNode in textInputNode->Component->UldManager.Nodes) {
                if (childNode.Value is null) continue;
                if (childNode.Value->GetNodeType() is NodeType.NineGrid or NodeType.Text) {
                    childNode.Value->SetWidth((ushort)(childNode.Value->GetWidth() - SizeOffset));
                }
            }
            
            foreach (var index in Enumerable.Range(0, 15)) {
                var newTextNode = new TextNode {
                    Position = new Vector2(460.0f, 118.0f + index * 14.2f),
                    Size = new Vector2(SizeOffset - 5.0f, 14.0f),
                    IsVisible = true,
                    String = $"{index + 1}",
                    FontType = FontType.Axis,
                    FontSize = 12,
                    AlignmentType = AlignmentType.TopRight,
                };
                System.NativeController.AttachNode(newTextNode, addon->RootNode);
                textNodes.Add(newTextNode);
            }
        };

        macroAddonController.OnDetach += addon => {
            var textInputNode = addon->GetNodeById<AtkComponentNode>(119);
            if (textInputNode is null) return;

            var position = Vector2.Zero;
            textInputNode->GetPositionFloat(&position.X, &position.Y);
            textInputNode->SetPositionFloat(position.X - SizeOffset, position.Y);
            
            textInputNode->SetWidth((ushort)(textInputNode->GetWidth() + SizeOffset));

            foreach (var childNode in textInputNode->Component->UldManager.Nodes) {
                if (childNode.Value is null) continue;
                if (childNode.Value->GetNodeType() is NodeType.NineGrid or NodeType.Text) {
                    childNode.Value->SetWidth((ushort)(childNode.Value->GetWidth() + SizeOffset));
                }
            }
            
            foreach (var node in textNodes) {
                System.NativeController.DetachNode(node, () => {
                    node.Dispose();
                });
            }

            textNodes.Clear();
        };
        
        macroAddonController.Enable();
    }

    public override void OnDisable() {
        macroAddonController?.Dispose();
        macroAddonController = null;
        
        textNodes?.Clear();
        textNodes = null;
    }
}
