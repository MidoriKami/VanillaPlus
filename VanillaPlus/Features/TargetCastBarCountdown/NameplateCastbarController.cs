using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Controllers;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;
using VanillaPlus.Utilities;

using NewAddonCastBarEnemy = FFXIVClientStructs.FFXIV.Client.UI.AddonCastBarEnemy;

namespace VanillaPlus.Features.TargetCastBarCountdown;

public class NameplateCastbarController : IDisposable {
    public TextNodeStyle LoadedStyle { get; }

    private readonly TargetCastBarCountdownConfig config;
    private TextNode[]? textNodes;

    private const string StylePath = "TargetCastBarCountdown.CastBarEnemy.style.json";

    private readonly AddonController<NewAddonCastBarEnemy> addonController;

    public unsafe NameplateCastbarController(TargetCastBarCountdownConfig config) {
        this.config = config;

        var defaultStyle = new TextNodeStyle {
            Position = new Vector2(8.0f, -4.0f),
            FontSize = 12,
            TextColor = ColorHelper.GetColor(1),
            TextOutlineColor = ColorHelper.GetColor(23),
            FontType = FontType.Miedinger,
            AlignmentType = AlignmentType.BottomLeft,
        };

        LoadedStyle = Config.LoadConfig(StylePath, defaultStyle).Result;
        if (LoadedStyle.TextColor == Vector4.Zero) {
            LoadedStyle = defaultStyle;
            Task.Run(() => LoadedStyle.Save(StylePath));
        }

        LoadedStyle.StyleChanged += styleObj => {
            Task.Run(() => styleObj.Save(StylePath));
            foreach (var node in textNodes ?? []) {
                LoadedStyle.ApplyStyle(node);
            }
        };

        addonController = new AddonController<NewAddonCastBarEnemy> {
            AddonName = "CastBarEnemy",
            OnSetup = OnAddonSetup,
            OnFinalize = OnAddonFinalize,
            OnPreUpdate = OnAddonRefresh,
        };
    }

    public void Enable() {
        addonController.Enable();
    }

    public void Dispose() {
        addonController.Dispose();
    }

    private unsafe void OnAddonSetup(NewAddonCastBarEnemy* addonCastBarEnemy) {
        textNodes = new TextNode[10];

        foreach (var index in Enumerable.Range(0, 10)) {
            var info = addonCastBarEnemy->CastBarNodes[index];
            var castBarNode = (AtkComponentNode*)info.CastBarNode;
            var targetNode = castBarNode->SearchNodeById<AtkResNode>(7);
            if (targetNode is null) continue;

            textNodes[index] = new TextNode {
                Size = new Vector2(82.0f, 12.0f),
                Position = new Vector2(8.0f, -4.0f),
                FontSize = 12,
                TextFlags = TextFlags.Edge,
                TextColor = ColorHelper.GetColor(1),
                TextOutlineColor = ColorHelper.GetColor(23),
                FontType = FontType.Miedinger,
                AlignmentType = AlignmentType.BottomLeft,
            };

            LoadedStyle.ApplyStyle(textNodes[index]);
            textNodes[index].AttachNode(targetNode);
        }
    }

    private unsafe void OnAddonRefresh(NewAddonCastBarEnemy* addonCastBarEnemy) {
        if (Service<IClientState>.Get().IsPvP || !config.PrimaryTarget) {
            foreach (var node in textNodes ?? []) {
                node.String = string.Empty;
            }
            return;
        }

        foreach (var index in Enumerable.Range(0, 10)) {
            var info = addonCastBarEnemy->CastBarInfo[index];
            var battleChara = Service<IObjectTable>.Get().GetBattleChara(info.EntityId);

            textNodes?[index].String = battleChara?.GetCastTimeString;
        }
    }

    private unsafe void OnAddonFinalize(NewAddonCastBarEnemy* addonCastBarEnemy) {
        foreach (var node in textNodes ?? []) {
            node.Dispose();
        }
        textNodes = null;
    }
}
