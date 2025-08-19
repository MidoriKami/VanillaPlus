using System.Drawing;
using System.IO;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;
using VanillaPlus.Extensions;
using VanillaPlus.Utilities;

namespace VanillaPlus.Features.TargetCastBarCountdown;

public unsafe class TargetCastBarCountdown : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Target Cast Bar Countdown",
        Description = "Adds the time remaining for your targets current cast to the cast bar.",
        Authors = ["MidoriKami"],
        Type = ModificationType.UserInterface,
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
        CompatabilityModule = new SimpleTweaksCompatabilityModule("UiAdjustments@TargetCastbarCountdown"),
    };

    private AddonController<AtkUnitBase>? targetInfoCastBarController;
    private AddonController<AtkUnitBase>? targetInfoController;
    private AddonController<AtkUnitBase>? focusTargetController;
    
    private TextNode? primaryTargetTextNode;
    private TextNode? primaryTargetAltTextNode;
    private TextNode? focusTargetTextNode;
    
    private static string PrimaryTargetStylePath => Path.Combine(Config.ConfigPath, "TargetCastBarCountdown.PrimaryTarget.style.json");
    private static string PrimaryTargetAltStylePath => Path.Combine(Config.ConfigPath, "TargetCastBarCountdown.PrimaryTargetAlt.style.json");
    private static string FocusTargetStylePath => Path.Combine(Config.ConfigPath, "TargetCastBarCountdown.FocusTarget.style.json");

    private TargetCastBarCountdownConfig? config;
    private TargetCastBarCountdownConfigWindow? configWindow;

    public override string ImageName => "TargetCastBarCountdown.png";

    public override void OnEnable() {
        config = TargetCastBarCountdownConfig.Load();
        configWindow = new TargetCastBarCountdownConfigWindow(config, DrawNodeConfigs, SaveNodes);
        configWindow.AddToWindowSystem();
        OpenConfigAction = configWindow.Toggle;

        targetInfoCastBarController = new AddonController<AtkUnitBase>("_TargetInfoCastBar");
        targetInfoCastBarController.OnAttach += AttachNode;
        targetInfoCastBarController.OnDetach += DetachNode;
        targetInfoCastBarController.OnUpdate += UpdateNode;
        targetInfoCastBarController.Enable();
        
        targetInfoController = new AddonController<AtkUnitBase>("_TargetInfo");
        targetInfoController.OnAttach += AttachNode;
        targetInfoController.OnDetach += DetachNode;
        targetInfoController.OnUpdate += UpdateNode;
        targetInfoController.Enable();
        
        focusTargetController = new AddonController<AtkUnitBase>("_FocusTargetInfo");
        focusTargetController.OnAttach += AttachNode;
        focusTargetController.OnDetach += DetachNode;
        focusTargetController.OnUpdate += UpdateNode;
        focusTargetController.Enable();
    }

    public override void OnDisable() {
        configWindow?.RemoveFromWindowSystem();
        configWindow = null;
        
        targetInfoCastBarController?.Dispose();
        targetInfoCastBarController = null;
        
        targetInfoController?.Dispose();
        targetInfoController = null;
        
        focusTargetController?.Dispose();
        focusTargetController = null;
    }
    
    private void DrawNodeConfigs() {
        using var toolbar = ImRaii.TabBar("");
        if (!toolbar) return;

        using (var primaryTarget = ImRaii.TabItem("Target Cast Bar")) {
            if (primaryTarget) {
                ImGui.TextColored(KnownColor.Gray.Vector(), "This is only shown when 'Display Target Info Independently' is enabled in HUD Settings");
                primaryTargetTextNode?.DrawConfig();
            }
        }

        using (var primaryTarget = ImRaii.TabItem("Target Info")) {
            if (primaryTarget) {
                ImGui.TextColored(KnownColor.Gray.Vector(), "This is only shown when 'Display Target Info Independently' is disabled in HUD Settings");
                primaryTargetAltTextNode?.DrawConfig();
            }
        }
        
        using (var primaryTarget = ImRaii.TabItem("Focus Target")) {
            if (primaryTarget) {
                ImGui.TextColored(KnownColor.Gray.Vector(), "This line is just so that all three tabs line up nicely :)");
                focusTargetTextNode?.DrawConfig();
            }
        }
    }
    
    private void SaveNodes() {
        primaryTargetTextNode?.Save(PrimaryTargetStylePath);
        primaryTargetAltTextNode?.Save(PrimaryTargetAltStylePath);
        focusTargetTextNode?.Save(FocusTargetStylePath);
    }

    private void AttachNode(AtkUnitBase* addon) {
        switch (addon->NameString) {
            case "_TargetInfoCastBar":
                primaryTargetTextNode = new TextNode {
                    Size = new Vector2(82.0f, 22.0f),
                    Position = new Vector2(0.0f, 16.0f),
                    FontSize = 20,
                    TextFlags = TextFlags.Edge,
                    TextOutlineColor = ColorHelper.GetColor(54),
                    FontType = FontType.Miedinger,
                    AlignmentType = AlignmentType.Right,
                    IsVisible = true,
                };
                primaryTargetTextNode.Load(PrimaryTargetStylePath);
                System.NativeController.AttachNode(primaryTargetTextNode, addon->GetNodeById(2), NodePosition.AsLastChild);
                break;
            
            case "_TargetInfo":
                primaryTargetAltTextNode = new TextNode {
                    Size = new Vector2(82.0f, 22.0f),
                    Position = new Vector2(0.0f, -16.0f),
                    FontSize = 20,
                    TextFlags = TextFlags.Edge,
                    TextOutlineColor = ColorHelper.GetColor(54),
                    FontType = FontType.Miedinger,
                    AlignmentType = AlignmentType.Right,
                    IsVisible = true,
                };
                primaryTargetAltTextNode.Load(PrimaryTargetAltStylePath);
                System.NativeController.AttachNode(primaryTargetAltTextNode, addon->GetNodeById(10), NodePosition.AsLastChild);
                break;
            
            case "_FocusTargetInfo":
                focusTargetTextNode = new TextNode {
                    Size = new Vector2(82.0f, 22.0f),
                    Position = new Vector2(0.0f, -16.0f),
                    FontSize = 20,
                    TextFlags = TextFlags.Edge,
                    TextOutlineColor = ColorHelper.GetColor(54),
                    FontType = FontType.Miedinger,
                    AlignmentType = AlignmentType.Right,
                    IsVisible = true,
                };
                focusTargetTextNode.Load(FocusTargetStylePath);
                System.NativeController.AttachNode(focusTargetTextNode, addon->GetNodeById(3), NodePosition.AsLastChild);
                break;
        }
    }

    private void DetachNode(AtkUnitBase* addon) {
        switch (addon->NameString) {
            case "_TargetInfoCastBar":
                System.NativeController.DisposeNode(ref primaryTargetTextNode);
                break;
            
            case "_TargetInfo":
                System.NativeController.DisposeNode(ref primaryTargetAltTextNode);
                break;
            
            case "_FocusTargetInfo":
                System.NativeController.DisposeNode(ref focusTargetTextNode);
                break;
        }
    }

    private void UpdateNode(AtkUnitBase* addon) {
        if (config is null) return;
        
        if (Services.ClientState.IsPvP) {
            if (primaryTargetTextNode is not null) primaryTargetTextNode.IsVisible = false;
            if (primaryTargetAltTextNode is not null) primaryTargetAltTextNode.IsVisible = false;
            if (focusTargetTextNode is not null) focusTargetTextNode.IsVisible = false;
            return;
        }
        
        switch (addon->NameString) {
            case "_TargetInfoCastBar":
            case "_TargetInfo":
                if (Services.TargetManager.Target is IBattleChara target && target.CurrentCastTime < target.TotalCastTime && config.PrimaryTarget) {
                    var castTime = (target.TotalCastTime - target.CurrentCastTime).ToString("00.00");

                    if (primaryTargetTextNode is not null) {
                        primaryTargetTextNode.IsVisible = true;
                        primaryTargetTextNode.Text = castTime;
                    }
                    
                    if (primaryTargetAltTextNode is not null) {
                        primaryTargetAltTextNode.IsVisible = true;
                        primaryTargetAltTextNode.Text = castTime;
                    }
                }
                else {
                    if (primaryTargetTextNode is not null) primaryTargetTextNode.IsVisible = false;
                    if (primaryTargetAltTextNode is not null) primaryTargetAltTextNode.IsVisible = false;
                }
                break;
            
            case "_FocusTargetInfo":
                if (Services.TargetManager.FocusTarget is IBattleChara focusTarget && focusTarget.CurrentCastTime < focusTarget.TotalCastTime && config.FocusTarget) {
                    var castTime = (focusTarget.TotalCastTime - focusTarget.CurrentCastTime).ToString("00.00");

                    if (focusTargetTextNode is not null) {
                        focusTargetTextNode.IsVisible = true;
                        focusTargetTextNode.Text = castTime;
                    }
                }
                else {
                    if (focusTargetTextNode is not null) focusTargetTextNode.IsVisible = false;
                }
                break;
        }
    }
}
