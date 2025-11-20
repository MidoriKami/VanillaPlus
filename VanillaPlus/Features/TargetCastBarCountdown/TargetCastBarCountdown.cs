using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;
using VanillaPlus.NativeElements.Config;
using VanillaPlus.NativeElements.Config.NodeEntries;
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

    private MultiAddonController? addonController;
    
    private TextNode? primaryTargetTextNode;
    private TextNode? primaryTargetAltTextNode;
    private TextNode? focusTargetTextNode;

    private TextNode?[]? castBarEnemyTextNode;
    
    private static string PrimaryTargetStylePath => Path.Combine(Config.ConfigPath, "TargetCastBarCountdown.PrimaryTarget.style.json");
    private static string PrimaryTargetAltStylePath => Path.Combine(Config.ConfigPath, "TargetCastBarCountdown.PrimaryTargetAlt.style.json");
    private static string FocusTargetStylePath => Path.Combine(Config.ConfigPath, "TargetCastBarCountdown.FocusTarget.style.json");
    private static string CastBarEnemyStylePath => Path.Combine(Config.ConfigPath, "TargetCastBarCountdown.CastBarEnemy.style.json");

    private TargetCastBarCountdownConfig? config;
    private ConfigAddon? configWindow;

    public override string ImageName => "TargetCastBarCountdown.png";

    public override void OnEnable() {
        config = TargetCastBarCountdownConfig.Load();
        configWindow = new ConfigAddon {
            InternalName = "TargetCastBarConfig",
            Title = "Target Castbar Countdown Config",
            Config = config,
        };

        configWindow.AddCategory("Toggles")
            .AddCheckbox("Show on Primary Target Castbar", nameof(config.PrimaryTarget))
            .AddCheckbox("Show on Focus Target Castbar", nameof(config.FocusTarget))
            .AddCheckbox("Show on Nameplate Target Castbar", nameof(config.NamePlateTargets));

        const NodeConfigEnum nodeConfigOptions = NodeConfigEnum.TextColor | NodeConfigEnum.Position | NodeConfigEnum.TextSize | 
                                                 NodeConfigEnum.TextFont | NodeConfigEnum.TextAlignment | NodeConfigEnum.TextOutlineColor;

        configWindow.AddCategory("Target Castbar Style (Combined)")
            .AddTextNodeConfig(PrimaryTargetAltStylePath, nodeConfigOptions);

        configWindow.AddCategory("Target Castbar Style (Separate)")
            .AddTextNodeConfig(PrimaryTargetStylePath, nodeConfigOptions);
        
        configWindow.AddCategory("Focus Target Castbar Style")
            .AddTextNodeConfig(FocusTargetStylePath, nodeConfigOptions);
        
        configWindow.AddCategory("Nameplate Castbar Style")
            .AddTextNodeConfig(CastBarEnemyStylePath, nodeConfigOptions);

        config.OnSave += () => {
            primaryTargetTextNode?.Load(PrimaryTargetStylePath);
            primaryTargetAltTextNode?.Load(PrimaryTargetAltStylePath);
            focusTargetTextNode?.Load(FocusTargetStylePath);
            foreach (var node in castBarEnemyTextNode ?? []) {
                node?.Load(CastBarEnemyStylePath);
            }
        };
        OpenConfigAction = configWindow.Toggle;

        addonController = new MultiAddonController("_TargetInfoCastBar", "_TargetInfo", "_FocusTargetInfo", "CastBarEnemy");
        addonController.OnAttach += AttachNode;
        addonController.OnDetach += DetachNode;
        addonController.OnUpdate += UpdateNode;
        addonController.Enable();
    }

    public override void OnDisable() {
        configWindow?.Dispose();
        configWindow = null;

        addonController?.Dispose();
        addonController = null;

        castBarEnemyTextNode = null;
    }

    private static TextNode BuildTextNode(Vector2 position) => new() {
        Size = new Vector2(82.0f, 22.0f),
        Position = position,
        FontSize = 20,
        TextFlags = TextFlags.Edge,
        TextColor = ColorHelper.GetColor(1),
        TextOutlineColor = ColorHelper.GetColor(23),
        FontType = FontType.Miedinger,
        AlignmentType = AlignmentType.Right,
    };

    private void AttachNode(AtkUnitBase* addon) {
        switch (addon->NameString) {
            case "_TargetInfoCastBar":
                primaryTargetTextNode = BuildTextNode(new Vector2(0.0f, 16.0f));

                primaryTargetTextNode.Load(PrimaryTargetStylePath);
                ForceConfigValues(primaryTargetTextNode, PrimaryTargetStylePath);
                primaryTargetTextNode.AttachNode(addon->GetNodeById(7));
                break;

            case "_TargetInfo":
                primaryTargetAltTextNode = BuildTextNode(new Vector2(0.0f, -16.0f));

                primaryTargetAltTextNode.Load(PrimaryTargetAltStylePath);
                ForceConfigValues(primaryTargetAltTextNode, PrimaryTargetAltStylePath);

                primaryTargetAltTextNode.AttachNode(addon->GetNodeById(15));
                break;

            case "_FocusTargetInfo":
                focusTargetTextNode = BuildTextNode(new Vector2(0.0f, -16.0f));

                focusTargetTextNode.Load(FocusTargetStylePath);
                ForceConfigValues(focusTargetTextNode, FocusTargetStylePath);

                focusTargetTextNode.AttachNode(addon->GetNodeById(8));
                break;

            case "CastBarEnemy":
                var castBarAddon = (AddonCastBarEnemy*)addon;
                castBarEnemyTextNode = new TextNode[10];

                foreach (var index in Enumerable.Range(0, 10)) {
                    ref var info = ref castBarAddon->CastBarNodes[index];

                    var newNode = BuildTextNode(new Vector2(0.0f, -12.0f)); 

                    newNode.Size = new Vector2(82.0f, 24.0f);
                    newNode.AlignmentType = AlignmentType.BottomRight;
                    newNode.FontSize = 12;
                    
                    castBarEnemyTextNode[index] = newNode;
                    newNode.Load(CastBarEnemyStylePath);
                    ForceConfigValues(castBarEnemyTextNode[index]!, CastBarEnemyStylePath);

                    var castBarNode = (AtkComponentNode*)info.CastBarNode;
                    newNode.AttachNode(castBarNode->SearchNodeById<AtkResNode>(7));
                }
                break;
        }
    }

    private void DetachNode(AtkUnitBase* addon) {
        switch (addon->NameString) {
            case "_TargetInfoCastBar":
                primaryTargetTextNode?.Dispose();
                primaryTargetTextNode = null;
                break;
            
            case "_TargetInfo":
                primaryTargetAltTextNode?.Dispose();
                primaryTargetAltTextNode = null;
                break;
            
            case "_FocusTargetInfo":
                focusTargetTextNode?.Dispose();
                focusTargetTextNode = null;
                break;
            
            case "CastBarEnemy":
                foreach (var node in castBarEnemyTextNode ?? []) {
                    node?.Dispose();
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
                primaryTargetTextNode.String = GetCastTime(GetTarget(), config.PrimaryTarget);
                break;

            case "_TargetInfo" when primaryTargetAltTextNode is not null:
                primaryTargetAltTextNode.String = GetCastTime(GetTarget(), config.PrimaryTarget);
                break;
            
            case "_FocusTargetInfo" when focusTargetTextNode is not null:
                focusTargetTextNode.String = GetCastTime(GetFocusTarget(), config.FocusTarget);
                break;
            
            case "CastBarEnemy" when castBarEnemyTextNode is not null:
                var castBarAddon = (AddonCastBarEnemy*)addon;
                
                foreach (var index in Enumerable.Range(0, 10)) {
                    var info = castBarAddon->CastBarInfo[index];
                    var node = castBarEnemyTextNode[index];

                    if (node is not null) {
                        node.String = GetCastTime(GetEntity(info.ObjectId.ObjectId), true);
                    }
                }
                break;
        }
    }

    private static string GetCastTime(IBattleChara? target, bool enabled) {
        if (!enabled) return string.Empty;
        if (target is null) return string.Empty;
        if (!target.IsValid()) return string.Empty;
        if (target.EntityId == 0xE0000000) return string.Empty;
        if (target.CurrentCastTime >= target.TotalCastTime) return string.Empty;

        return (target.TotalCastTime - target.CurrentCastTime).ToString("00.00", CultureInfo.InvariantCulture);
    }
    
    private static IBattleChara? GetTarget()
        => Services.TargetManager.Target as IBattleChara ?? Services.TargetManager.SoftTarget as IBattleChara;

    private static IBattleChara? GetFocusTarget()
        => Services.TargetManager.FocusTarget as IBattleChara;

    private static IBattleChara? GetEntity(uint entityId)
        => Services.ObjectTable.CharacterManagerObjects.FirstOrDefault(obj => obj.EntityId == entityId) as IBattleChara;

    // Certain config options are no longer provided, but may have still been saved in an invalid state
    // This will force the correct default values and save them.
    private static void ForceConfigValues(TextNode node, string filePath) {
        if (!node.IsVisible || node.MultiplyColor != Vector3.One) {
            node.IsVisible = true;
            node.MultiplyColor = Vector3.One;
        
            node.Save(filePath);
        }
    }
}
