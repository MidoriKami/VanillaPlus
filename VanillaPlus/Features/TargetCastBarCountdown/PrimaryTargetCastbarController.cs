using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Controllers;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;
using VanillaPlus.Utilities;

namespace VanillaPlus.Features.TargetCastBarCountdown;

public class PrimaryTargetCastbarController {
    public TextNodeStyle LoadedStyle { get; }

    private readonly TargetCastBarCountdownConfig config;
    private TextNode? textNode;

    private const string StylePath = "TargetCastBarCountdown.PrimaryTarget.style.json";

    private readonly AddonController addonController;

    public unsafe PrimaryTargetCastbarController(TargetCastBarCountdownConfig config) {
        this.config = config;

        var defaultStyle = new TextNodeStyle {
            Position = new Vector2(0.0f, 16.0f),
            TextColor = ColorHelper.GetColor(1),
            TextOutlineColor = ColorHelper.GetColor(54),
            FontSize = 20,
            FontType = FontType.Miedinger,
            AlignmentType = AlignmentType.Right,
        };

        LoadedStyle = Config.LoadConfig(StylePath, defaultStyle).Result;
        if (LoadedStyle.TextColor == Vector4.Zero) {
            LoadedStyle = defaultStyle;
            Task.Run(() => LoadedStyle.Save(StylePath));
        }

        LoadedStyle.StyleChanged += styleObj => {
            Task.Run(() => styleObj.Save(StylePath));
            LoadedStyle.ApplyStyle(textNode);
        };

        addonController = new AddonController {
            AddonName = "_TargetInfoCastBar",
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

    private unsafe void OnAddonSetup(AtkUnitBase* addon) {
        var targetNode = addon->GetNodeById(7);
        if (targetNode is null) return;

        textNode = new TextNode {
            Size = new Vector2(82.0f, 22.0f),
            Position = new Vector2(0.0f, 16.0f),
            FontSize = 20,
            TextFlags = TextFlags.Edge,
            TextColor = ColorHelper.GetColor(1),
            TextOutlineColor = ColorHelper.GetColor(23),
            FontType = FontType.Miedinger,
            AlignmentType = AlignmentType.Center,
        };

        LoadedStyle.ApplyStyle(textNode);
        textNode.AttachNode(targetNode);
    }

    private unsafe void OnAddonRefresh(AtkUnitBase* addon) {
        if (IClientState.Get().IsPvP || !config.PrimaryTarget) {
            textNode?.String = string.Empty;
            return;
        }

        textNode?.String = ITargetManager.Get().GetTarget()?.GetCastTimeString;
    }

    private unsafe void OnAddonFinalize(AtkUnitBase* addon) {
        textNode?.Dispose();
        textNode = null;
    }
}
