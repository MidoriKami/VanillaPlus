using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Controllers;
using KamiToolKit.Nodes;
using KamiToolKit.Timelines;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.BetterInterruptableCastBars;

public class BetterInterruptableCastBars : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_BetterInterruptableCastBars,
        Description = Strings.ModificationDescription_BetterInterruptableCastBars,
        Type = ModificationType.UserInterface,
        Authors = ["MidoriKami"],
        CompatibilityModule = new SimpleTweaksCompatibilityModule("UiAdjustments@ImprovedInterruptableCastbars"),
    };

    public override string ImageName => "BetterInterruptableCastBars.png";

    private AddonController? targetInfoCastbarController;
    private ImageNode? targetInfoCastbarPulseNode;

    private Hook<ActionManager.Delegates.IsActionHighlighted>? antsHook;

    public override async Task OnEnableAsync() {
        unsafe {
            antsHook = Services.Hooker.HookFromAddress<ActionManager.Delegates.IsActionHighlighted>(ActionManager.MemberFunctionPointers.IsActionHighlighted, OnAntsCheck);
            antsHook?.Enable();

            targetInfoCastbarController = new AddonController {
                AddonName = "_TargetInfoCastBar",
                OnSetup = TargetInfoCastBarSetup,
                OnFinalize = TargetInfoCastBarFinalize,
            };
        }

        await Services.Framework.Run(targetInfoCastbarController.Enable);
    }

    public override async Task OnDisableAsync() {
        await Services.Framework.Run(() => targetInfoCastbarController?.Disable() );
        targetInfoCastbarController = null;

        antsHook?.Dispose();
        antsHook = null;
    }

    private unsafe void TargetInfoCastBarFinalize(AtkUnitBase* addon) {
        targetInfoCastbarPulseNode?.Dispose();
        targetInfoCastbarPulseNode = null;

        var existingPulseNode = addon->GetImageNodeById(6);
        if (existingPulseNode is null) return;

        existingPulseNode->ScaleX = 1.0f;
        existingPulseNode->ScaleY = 1.0f;
        existingPulseNode->DrawFlags |= 1;
    }

    private unsafe void TargetInfoCastBarSetup(AtkUnitBase* addon) {
        var existingPulseNode = addon->GetImageNodeById(6);
        if (existingPulseNode is null) return;

        existingPulseNode->ScaleX = 1.33f;
        existingPulseNode->ScaleY = 1.33f;
        existingPulseNode->DrawFlags |= 1;

        targetInfoCastbarPulseNode = new ImageNode {
            Size = new Vector2(232.0f, 32.0f),
            Position = new Vector2(-12.0f, -6.0f),
            Scale = new Vector2(1.33f, 1.33f),
            Origin = new Vector2(116.0f, 16.0f),
            AddColor = new Vector3(255.0f, -80.0f, 0.0f) / 255.0f,
            Alpha = 0, };

        LoadAssets(targetInfoCastbarPulseNode);
        targetInfoCastbarPulseNode.AttachNode(existingPulseNode, NodePosition.BeforeTarget);
    }

    private unsafe bool OnAntsCheck(ActionManager* thisPtr, ActionType actionType, uint actionId) {
        try {
            if (Services.TargetManager.GetTarget() is { } target) {
                if (actionId is 7538 or 7551 && target is { IsCasting: true, IsCastInterruptible: true }) {
                    return true;
                }
            }
        }
        catch (Exception e) {
            Services.PluginLog.Exception(e);
        }

        return antsHook!.Original(thisPtr, actionType, actionId);
    }

    private static unsafe void LoadAssets(ImageNode node) {
        foreach (var index in Enumerable.Range(0, 14)) {
            var row = index / 2;
            var column = index % 2;
            node.AddPart(new Part {
                TexturePath = "ui/uld/Interrupt.tex",
                Size = new Vector2(232.0f, 32.0f),
                TextureCoordinates = new Vector2(232.0f * column, 32.0f * row),
            });
        }

        node.AddTimeline(new TimelineBuilder()
            .BeginFrameSet(21, 45)
            .AddFrame(21, partId: 13, alpha: 0)
            .AddFrame(45, partId: 0, alpha: 255)
            .EndFrameSet()
            .Build()
        );
    }
}
