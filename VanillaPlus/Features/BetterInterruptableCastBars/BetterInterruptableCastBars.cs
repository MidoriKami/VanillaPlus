using System;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Classes;
using KamiToolKit.Classes.TimelineBuilding;
using KamiToolKit.NodeParts;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.BetterInterruptableCastBars;

public unsafe class BetterInterruptableCastBars : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Better Interruptable Castbars",
        Description = "Makes enemy interruptable castbars much more noticeable.\n\nAdditionally skills that can interrupt the cast are indicated on your hotbar.",
        Type = ModificationType.UserInterface,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "InitialChangelog"),
        ],
    };

    public override string ImageName => "BetterInterruptableCastBars.png";

    private AddonController? targetInfoCastbarController;
    private ImageNode? targetInfoCastbarPulseNode;

    private Hook<ActionManager.Delegates.IsActionHighlighted>? antsHook;

    public override void OnEnable() {
        antsHook = Services.Hooker.HookFromAddress<ActionManager.Delegates.IsActionHighlighted>(ActionManager.MemberFunctionPointers.IsActionHighlighted, OnAntsCheck);
        antsHook?.Enable();
        
        targetInfoCastbarController = new AddonController("_TargetInfoCastBar");
        targetInfoCastbarController.OnAttach += addon => {
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
            };

            LoadAssets(targetInfoCastbarPulseNode);
            System.NativeController.AttachNode(targetInfoCastbarPulseNode, (AtkResNode*)existingPulseNode, NodePosition.BeforeTarget);
        };
        
        targetInfoCastbarController.OnDetach += addon => {
            System.NativeController.DisposeNode(ref targetInfoCastbarPulseNode);

            var existingPulseNode = addon->GetImageNodeById(6);
            if (existingPulseNode is null) return;

            existingPulseNode->ScaleX = 1.0f;
            existingPulseNode->ScaleY = 1.0f;
            existingPulseNode->DrawFlags |= 1;
        };
        
        targetInfoCastbarController.Enable();
    }

    public override void OnDisable() {
        targetInfoCastbarController?.Dispose();
        targetInfoCastbarController = null;

        antsHook?.Dispose();
        antsHook = null;
    }

    private bool OnAntsCheck(ActionManager* thisPtr, ActionType actionType, uint actionId) {
        try {
            if (Services.TargetManager.Target is IBattleChara target) {
                if (actionId is 7538 or 7551 && target is { IsCasting: true, IsCastInterruptible: true }) {
                    return true;
                }
            }
        }
        catch (Exception e) {
            Services.PluginLog.Error(e, "Exception in BetterInterruptableCastBars.OnAntsCheck");
        }

        return antsHook!.Original(thisPtr, actionType, actionId);
    }

    private static void LoadAssets(ImageNode node) {
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
            .AddFrame(21, partId: 13)
            .AddFrame(45, partId: 0)
            .EndFrameSet()
            .Build()
        );
    }
}
