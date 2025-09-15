using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
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
            new ChangeLogInfo(2, "Added support for 10 'CastBarEnemy' nodes"),
        ],
        CompatibilityModule = new SimpleTweaksCompatibilityModule("UiAdjustments@TargetCastbarCountdown"),
    };

    private AddonController<AtkUnitBase>? targetInfoCastBarController;
    private AddonController<AtkUnitBase>? targetInfoController;
    private AddonController<AtkUnitBase>? focusTargetController;
    private AddonController<AtkUnitBase>? castBarEnemyController;
    
    private TextNode? primaryTargetTextNode;
    private TextNode? primaryTargetAltTextNode;
    private TextNode? focusTargetTextNode;

    private TextNode?[]? castBarEnemyTextNode;
    
    private static string PrimaryTargetStylePath => Path.Combine(Config.ConfigPath, "TargetCastBarCountdown.PrimaryTarget.style.json");
    private static string PrimaryTargetAltStylePath => Path.Combine(Config.ConfigPath, "TargetCastBarCountdown.PrimaryTargetAlt.style.json");
    private static string FocusTargetStylePath => Path.Combine(Config.ConfigPath, "TargetCastBarCountdown.FocusTarget.style.json");
    private static string CastBarEnemyStylePath => Path.Combine(Config.ConfigPath, "TargetCastBarCountdown.CastBarEnemy.style.json");

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

        castBarEnemyController = new AddonController<AtkUnitBase>("CastBarEnemy");
        castBarEnemyController.OnAttach += AttachNode;
        castBarEnemyController.OnDetach += DetachNode;
        castBarEnemyController.OnUpdate += UpdateNode;
        castBarEnemyController.Enable();
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
        
        castBarEnemyController?.Dispose();
        castBarEnemyController = null;

        castBarEnemyTextNode = null;
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
                ImGui.TextColored(KnownColor.Gray.Vector(), "This line is just so that all four tabs line up nicely :)");
                focusTargetTextNode?.DrawConfig();
            }
        }
        
        using (var primaryTarget = ImRaii.TabItem("CastBarEnemy Target")) {
            if (primaryTarget) {
                ImGui.TextColored(KnownColor.Gray.Vector(), "This is for the cast bars under enemy names");
                var firstNode = castBarEnemyTextNode?.First();
                if (firstNode is not null) {
                    firstNode.DrawConfig();

                    foreach (var node in castBarEnemyTextNode ?? []) {
                        if (node != firstNode) {
                            node?.Load(firstNode);
                        }
                    }
                }
            }
        }
    }
    
    private void SaveNodes() {
        primaryTargetTextNode?.Save(PrimaryTargetStylePath);
        primaryTargetAltTextNode?.Save(PrimaryTargetAltStylePath);
        focusTargetTextNode?.Save(FocusTargetStylePath);
        castBarEnemyTextNode?.First()?.Save(CastBarEnemyStylePath);
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
                primaryTargetTextNode.IsVisible = true;
                
                System.NativeController.AttachNode(primaryTargetTextNode, addon->GetNodeById(7), NodePosition.AsLastChild);
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
                primaryTargetAltTextNode.IsVisible = true;
                System.NativeController.AttachNode(primaryTargetAltTextNode, addon->GetNodeById(15), NodePosition.AsLastChild);
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
                focusTargetTextNode.IsVisible = true;
                System.NativeController.AttachNode(focusTargetTextNode, addon->GetNodeById(8), NodePosition.AsLastChild);
                break;
            
            case "CastBarEnemy":
                var castBarAddon = (AddonCastBarEnemy*)addon;
                castBarEnemyTextNode = new TextNode[10];

                foreach (var index in Enumerable.Range(0, 10)) {
                    ref var info = ref castBarAddon->CastBarNodes[index];

                    var newNode = new TextNode {
                        Size = new Vector2(82.0f, 24.0f),
                        Position = new Vector2(0.0f, -12.0f),
                        FontSize = 12,
                        TextFlags = TextFlags.Edge,
                        TextOutlineColor = ColorHelper.GetColor(54),
                        FontType = FontType.Miedinger,
                        AlignmentType = AlignmentType.BottomRight,
                        IsVisible = true,
                    };
                    
                    castBarEnemyTextNode[index] = newNode;
                    newNode.Load(CastBarEnemyStylePath);
                    castBarEnemyTextNode[index]!.IsVisible = true;

                    var castBarNode = (AtkComponentNode*)info.CastBarNode;
                    System.NativeController.AttachNode(newNode, castBarNode->SearchNodeById<AtkResNode>(7));
                }
                break;
        }
    }

    private void DetachNode(AtkUnitBase* addon) {
        switch (addon->NameString) {
            case "_TargetInfoCastBar":
                System.NativeController.DisposeNode(ref primaryTargetTextNode);
                primaryTargetTextNode = null;
                break;
            
            case "_TargetInfo":
                System.NativeController.DisposeNode(ref primaryTargetAltTextNode);
                primaryTargetAltTextNode = null;
                break;
            
            case "_FocusTargetInfo":
                System.NativeController.DisposeNode(ref focusTargetTextNode);
                focusTargetTextNode = null;
                break;
            
            case "CastBarEnemy":
                foreach (var node in castBarEnemyTextNode ?? []) {
                    System.NativeController.DetachNode(node, () => {
                        node?.Dispose();
                    });
                }
                castBarEnemyTextNode = null;
                break;
        }
    }

    private void UpdateNode(AtkUnitBase* addon) {
        if (config is null) return;
        
        if (Services.ClientState.IsPvP) {
            if (primaryTargetTextNode is not null) primaryTargetTextNode.String = string.Empty;
            if (primaryTargetAltTextNode is not null) primaryTargetAltTextNode.String = string.Empty;
            if (focusTargetTextNode is not null) focusTargetTextNode.String = string.Empty;
            foreach (var node in castBarEnemyTextNode ?? []) {
                if (node is not null) {
                    node.String = string.Empty;
                }
            }
            return;
        }
        
        switch (addon->NameString) {
            case "_TargetInfoCastBar" when primaryTargetTextNode is not null:
                if (Services.TargetManager.Target is IBattleChara primaryTarget && primaryTarget.CurrentCastTime < primaryTarget.TotalCastTime && config.PrimaryTarget) {
                    var castTime = (primaryTarget.TotalCastTime - primaryTarget.CurrentCastTime).ToString("00.00", CultureInfo.InvariantCulture);

                    primaryTargetTextNode.String = castTime;
                }
                break;

            case "_TargetInfo" when primaryTargetAltTextNode is not null:
                if (Services.TargetManager.Target is IBattleChara target && target.CurrentCastTime < target.TotalCastTime && config.PrimaryTarget) {
                    var castTime = (target.TotalCastTime - target.CurrentCastTime).ToString("00.00", CultureInfo.InvariantCulture);

                    primaryTargetAltTextNode.String = castTime;
                }
                break;
            
            case "_FocusTargetInfo" when focusTargetTextNode is not null:
                if (Services.TargetManager.FocusTarget is IBattleChara focusTarget && focusTarget.CurrentCastTime < focusTarget.TotalCastTime && config.FocusTarget) {
                    var castTime = (focusTarget.TotalCastTime - focusTarget.CurrentCastTime).ToString("00.00", CultureInfo.InvariantCulture);

                    focusTargetTextNode.String = castTime;
                }
                break;
            
            case "CastBarEnemy" when castBarEnemyTextNode is not null:
                var castBarAddon = (AddonCastBarEnemy*)addon;
                
                foreach (var index in Enumerable.Range(0, 10)) {
                    var info = castBarAddon->CastBarInfo[index];
                    var node = castBarEnemyTextNode[index];
                    
                    var targetObject = Services.ObjectTable.FirstOrDefault(obj => obj.EntityId == info.ObjectId.ObjectId);
                    if (targetObject is IBattleNpc enemyTarget && enemyTarget.CurrentCastTime < enemyTarget.TotalCastTime) {
                        var castTime = (enemyTarget.TotalCastTime - enemyTarget.CurrentCastTime).ToString("00.00", CultureInfo.InvariantCulture);

                        if (node is not null) {
                            node.String = castTime;
                        }
                    }
                }
                break;
        }
    }
}
