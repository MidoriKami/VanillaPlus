using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using KamiToolKit.Enums;
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

    private CosmicExplorationProgressAddon? progressAddon;
    private CircleButtonNode? hudShowNode;

    private AddonController? wksHudController;

    public override string ImageName => "CosmicExplorationProgressWindow.png";

    public override async Task OnEnableAsync() {
        progressAddon = new CosmicExplorationProgressAddon {
            Size = new Vector2(320.0f, 317.0f),
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

        await Services.GetService<IFramework>().RunSafely(wksHudController.Enable);
    }

    public override async Task OnDisableAsync() {
        await Services.GetService<IFramework>().RunSafely(() => {
            wksHudController?.Dispose();
            hudShowNode?.Dispose();
        });

        wksHudController = null;
        hudShowNode = null;

        await Task.WhenAll(progressAddon?.DisposeAsync().AsTask() ?? Task.CompletedTask);
        progressAddon = null;
    }

    private unsafe void WKSHudFinalize(AtkUnitBase* _) {
        hudShowNode?.Dispose();
        hudShowNode = null;
    }

    private unsafe void WKSHudSetup(AtkUnitBase* wksHud) {
        if (progressAddon is null) return;

        hudShowNode = new CircleButtonNode {
            Icon = CircleButtonIcon.Eye,
            AddColor = new Vector3(0.0f, -0.125f, 128f / 255f),
            Size = new Vector2(28.0f),
            Position = new Vector2(26.0f, 26.0f),
            OnClick = progressAddon.Toggle,
            TextTooltip = Strings.CosmicExplorationProgressWindow_HudButtonTooltip,
        };

        // override the texture to use the base theme, since that's what the gear button in WKSHud does
        hudShowNode.ImageNode.LoadTexture("ui/uld/CircleButtons.tex", false);

        hudShowNode.AttachNode(wksHud);
    }
}
