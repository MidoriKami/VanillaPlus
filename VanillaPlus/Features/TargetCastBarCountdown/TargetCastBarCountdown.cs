using System.Globalization;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Classes.Controllers;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;
using VanillaPlus.NativeElements.Config;
using VanillaPlus.NativeElements.Config.NodeEntries;
using VanillaPlus.Utilities;

namespace VanillaPlus.Features.TargetCastBarCountdown;

public unsafe class TargetCastBarCountdown : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings("ModificationDisplay_TargetCastBarCountdown"),
        Description = Strings("ModificationDescription_TargetCastBarCountdown"),
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
    
    private TextNodeStyle? primaryTargetStyle;
    private TextNodeStyle? primaryTargetAltStyle;
    private TextNodeStyle? focusTargetStyle;
    private TextNodeStyle? castBarEnemyStyle;

    private TargetCastBarCountdownConfig? config;
    private ConfigAddon? configWindow;

    public override string ImageName => "TargetCastBarCountdown.png";

    public override void OnEnable() {
        config = TargetCastBarCountdownConfig.Load();

        LoadStyles();
        LoadConfigWindow();

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

    private void LoadStyles() {
        primaryTargetStyle = Config.LoadConfig<TextNodeStyle>("TargetCastBarCountdown.PrimaryTarget.style.json");
        primaryTargetAltStyle = Config.LoadConfig<TextNodeStyle>("TargetCastBarCountdown.PrimaryTargetAlt.style.json");
        focusTargetStyle = Config.LoadConfig<TextNodeStyle>("TargetCastBarCountdown.FocusTarget.style.json");
        castBarEnemyStyle = Config.LoadConfig<TextNodeStyle>("TargetCastBarCountdown.CastBarEnemy.style.json");

        primaryTargetStyle.StyleChanged += () => {
            primaryTargetStyle.Save("TargetCastBarCountdown.PrimaryTarget.style.json");
            primaryTargetStyle.ApplyStyle(primaryTargetTextNode);
        };
        
        primaryTargetAltStyle.StyleChanged += () => {
            primaryTargetAltStyle.Save("TargetCastBarCountdown.PrimaryTargetAlt.style.json");
            primaryTargetAltStyle.ApplyStyle(primaryTargetAltTextNode);
        };
        
        focusTargetStyle.StyleChanged += () => {
            focusTargetStyle.Save("TargetCastBarCountdown.FocusTarget.style.json");
            focusTargetStyle.ApplyStyle(focusTargetTextNode);
        };
        
        castBarEnemyStyle.StyleChanged += () => {
            castBarEnemyStyle.Save("TargetCastBarCountdown.CastBarEnemy.style.json");
            castBarEnemyStyle.ApplyStyle(castBarEnemyTextNode);
        };
    }

    private void LoadConfigWindow() {
        if (config is null) return;
        if (primaryTargetStyle is null) return;
        if (primaryTargetAltStyle is null) return;
        if (focusTargetStyle is null) return;
        if (castBarEnemyStyle is null) return;

        configWindow = new ConfigAddon {
            InternalName = "TargetCastBarConfig",
            Title = Strings("TargetCastBarCountdown_ConfigTitle"),
            Config = config,
        };

        configWindow.AddCategory(Strings("Toggles"))
            .AddCheckbox(Strings("TargetCastBarCountdown_CheckboxPrimary"), nameof(config.PrimaryTarget))
            .AddCheckbox(Strings("TargetCastBarCountdown_CheckboxFocus"), nameof(config.FocusTarget))
            .AddCheckbox(Strings("TargetCastBarCountdown_CheckboxNameplate"), nameof(config.NamePlateTargets));

        configWindow.AddCategory(Strings("TargetCastBarCountdown_CategoryPrimaryStyle"))
            .AddNodeConfig(primaryTargetStyle);

        configWindow.AddCategory(Strings("TargetCastBarCountdown_CategoryPrimaryAltStyle"))
            .AddNodeConfig(primaryTargetAltStyle);
        
        configWindow.AddCategory(Strings("TargetCastBarCountdown_CategoryFocusStyle"))
            .AddNodeConfig(focusTargetStyle);
        
        configWindow.AddCategory(Strings("TargetCastBarCountdown_CategoryNameplateStyle"))
            .AddNodeConfig(castBarEnemyStyle);
        
        OpenConfigAction = configWindow.Toggle;
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
                primaryTargetStyle?.ApplyStyle(primaryTargetTextNode);
                primaryTargetTextNode.AttachNode(addon->GetNodeById(7));
                break;

            case "_TargetInfo":
                primaryTargetAltTextNode = BuildTextNode(new Vector2(0.0f, -16.0f));
                primaryTargetAltStyle?.ApplyStyle(primaryTargetAltTextNode);
                primaryTargetAltTextNode.AttachNode(addon->GetNodeById(15));
                break;

            case "_FocusTargetInfo":
                focusTargetTextNode = BuildTextNode(new Vector2(0.0f, -16.0f));
                focusTargetStyle?.ApplyStyle(focusTargetTextNode);
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
                    castBarEnemyStyle?.ApplyStyle(newNode);

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
            primaryTargetTextNode?.String = string.Empty;
            primaryTargetAltTextNode?.String = string.Empty;
            focusTargetTextNode?.String = string.Empty;
            foreach (var node in castBarEnemyTextNode ?? []) {
                node?.String = string.Empty;
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

                    node?.String = GetCastTime(GetEntity(info.ObjectId.ObjectId), true);
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
}
