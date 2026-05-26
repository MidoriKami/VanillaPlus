using System.Numerics;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Features.CosmicExplorationProgressWindow.Addons;

namespace VanillaPlus.Features.CosmicExplorationProgressWindow;

public class CosmicExplorationProgressWindow : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_CosmicExplorationProgressWindow,
        Description = Strings.ModificationDescription_CosmicExplorationProgressWindow,
        Type = ModificationType.NewWindow,
        Authors = ["salanth357"],
    };

    private CosmicExplorationProgressAddon? addon;
    private CircleButtonNode? hudShowNode;

    private AddonController? wksHudController;

    public override string ImageName => "CosmicExplorationProgressWindow.png";

    public override async Task OnEnableAsync() {
        addon = new CosmicExplorationProgressAddon {
            Size = new Vector2(320.0f, 290.0f),
            InternalName = "CosmicExplorationProgress",
            Title = string.Empty, // No title actually needed for this addon
            DisableClose = true,
            DisableCloseTransition = true,
        };

        unsafe {
            wksHudController = new AddonController {
                AddonName = "WKSHud",
                OnSetup = WKSHudSetup,
                OnFinalize = WKSHudFinalize,
            };
        }

        await wksHudController.EnableAsync();
    }

    public override async Task OnDisableAsync() {
        if (wksHudController is not null) {
            await wksHudController.DisableAsync();
            wksHudController = null;
        }

        if (hudShowNode is not null) {
            await hudShowNode.DisposeAsync();
            hudShowNode = null;
        }

        if (addon is not null) {
            await addon.DisposeAsync();
            addon = null;
        }
    }

    private unsafe void WKSHudFinalize(AtkUnitBase* _) {
        hudShowNode?.Dispose();
        hudShowNode = null;
    }

    private unsafe void WKSHudSetup(AtkUnitBase* wksHud) {
        if (addon is null) return;

        hudShowNode = new CircleButtonNode {
            Icon = ButtonIcon.Eye,
            AddColor = new Vector3(0.0f, -0.125f, 128f / 255f),
            Size = new Vector2(28.0f),
            Position = new Vector2(26.0f, 26.0f),
            OnClick = addon.Toggle,
            TextTooltip = Strings.CosmicExplorationProgressWindow_HudButtonTooltip, };

        // override the texture to use the base theme, since that's what the gear button in WKSHud does
        hudShowNode.ImageNode.LoadTexture("ui/uld/CircleButtons.tex", false);

        hudShowNode.AttachNode(wksHud);
    }

}
