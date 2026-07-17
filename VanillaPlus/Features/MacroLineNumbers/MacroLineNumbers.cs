using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.MacroLineNumbers;

public class MacroLineNumbers : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_MacroLineNumbers,
        Description = Strings.ModificationDescription_MacroLineNumbers,
        Type = ModificationType.UserInterface,
        Authors = ["MidoriKami"],
    };

    public override string ImageName => "MacroLineNumbers.png";

    private AddonController? macroAddonController;

    private readonly Vector2 sizeOffset = new(20.0f, 150.0f);

    private List<TextNode>? textNodes;

    public override async Task OnEnableAsync() {
        textNodes = [];

        unsafe {
            macroAddonController = new AddonController {
                AddonName = "Macro",
                OnSetup = MacroSetup,
                OnFinalize = MacroFinalize,
            };
        }

        await IFramework.Get().RunSafely(macroAddonController.Enable);
    }

    public override async Task OnDisableAsync() {
        await IFramework.Get().RunSafely(() => macroAddonController?.Dispose());
        macroAddonController = null;

        textNodes?.Clear();
        textNodes = null;
    }

    private unsafe void MacroSetup(AtkUnitBase* addon) {
        if (textNodes is null) return;

        var textInputNode = addon->GetNodeById<AtkComponentNode>(119);
        if (textInputNode is null) return;

        RepositionNode(textInputNode, sizeOffset);

        foreach (var index in Enumerable.Range(0, 15)) {
            var newTextNode = new TextNode {
                Position = new Vector2(460.0f, 119.0f + index * 14f),
                Size = new Vector2(sizeOffset.X - 5.0f, 14.0f),
                String = $"{index + 1}",
                FontType = FontType.Axis,
                FontSize = 12,
                AlignmentType = AlignmentType.TopRight, };
            newTextNode.AttachNode(addon);
            textNodes.Add(newTextNode);
        }
    }

    private unsafe void MacroFinalize(AtkUnitBase* addon) {
        if (textNodes is null) return;

        var textInputNode = addon->GetNodeById<AtkComponentNode>(119);
        if (textInputNode is null) return;

        RepositionNode(textInputNode, -sizeOffset);

        foreach (var node in textNodes) {
            node.Dispose();
        }

        textNodes.Clear();
    }

    private static unsafe void RepositionNode(AtkComponentNode* inputComponentNode, Vector2 offset) {
        var collisionNode = inputComponentNode->SearchNodeById<AtkCollisionNode>(20);
        var backgroundNode = inputComponentNode->SearchNodeById<AtkNineGridNode>(19);
        var borderNode = inputComponentNode->SearchNodeById<AtkNineGridNode>(18);
        var remainingLineNode = inputComponentNode->SearchNodeById<AtkTextNode>(17);
        var textInputNode = inputComponentNode->SearchNodeById<AtkTextNode>(16);

        if (collisionNode is null || backgroundNode is null || borderNode is null || textInputNode is null || remainingLineNode is null) return;
        var position = Vector2.Zero;

        inputComponentNode->GetPositionFloat(&position.X, &position.Y);
        inputComponentNode->SetPositionFloat(position.X + offset.X, position.Y);

        inputComponentNode->SetWidth((ushort)(inputComponentNode->GetWidth() - offset.X));
        inputComponentNode->SetHeight((ushort)(inputComponentNode->GetHeight() - offset.Y));

        collisionNode->SetWidth((ushort)(collisionNode->GetWidth() - offset.X));
        collisionNode->SetHeight((ushort)(collisionNode->GetHeight() - offset.Y));

        backgroundNode->SetWidth((ushort)(backgroundNode->GetWidth() - offset.X));
        backgroundNode->SetHeight((ushort)(backgroundNode->GetHeight() - offset.Y));

        borderNode->SetWidth((ushort)(borderNode->GetWidth() - offset.X));
        borderNode->SetHeight((ushort)(borderNode->GetHeight() - offset.Y));

        textInputNode->SetWidth((ushort)(textInputNode->GetWidth() - offset.X));
        textInputNode->SetHeight((ushort)(textInputNode->GetHeight() - offset.Y));

        remainingLineNode->GetPositionFloat(&position.X, &position.Y);
        remainingLineNode->SetPositionFloat(position.X, position.Y - offset.Y);
        remainingLineNode->SetWidth((ushort)(remainingLineNode->GetWidth() - offset.X));
    }
}
