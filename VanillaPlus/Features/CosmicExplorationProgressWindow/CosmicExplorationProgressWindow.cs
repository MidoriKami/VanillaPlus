using System.Numerics;
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
        Authors = [ "salanth357" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };
    
    private CosmicExplorationProgressAddon? addon;
    private CircleButtonNode? hudShowNode;

    private AddonController? wksHudController;

    public override string ImageName => "CosmicExplorationProgressWindow.png";

    public override unsafe void OnEnable() {
        addon = new CosmicExplorationProgressAddon {
            Size = new Vector2(320.0f, 290.0f),
            InternalName = "CosmicExplorationProgress",
            Title = string.Empty, // No title actually needed for this addon
            DisableClose = true,
            DisableCloseTransition = true,
        };

        wksHudController = new AddonController("WKSHud");

        wksHudController.OnAttach += wksHud => {
            hudShowNode = new CircleButtonNode {
                Icon = ButtonIcon.Eye,
                AddColor = new Vector3(0.0f, -0.125f, 128f / 255f),
                Size = new Vector2(28.0f),
                Position = new Vector2(26.0f, 26.0f),
                OnClick = addon.Toggle,
                TextTooltip = Strings.CosmicExplorationProgressWindow_HudButtonTooltip,
            };

            // override the texture to use the base theme, since that's what the gear button in WKSHud does
            hudShowNode.ImageNode.LoadTexture("ui/uld/CircleButtons.tex", false);

            hudShowNode.AttachNode(wksHud);
        };

        wksHudController.OnDetach += _ => {
            hudShowNode?.Dispose();
            hudShowNode = null;
        };
        wksHudController.Enable();
    }

    public override void OnDisable() {
        wksHudController?.Disable();
        wksHudController = null;
        
        addon?.Dispose();
        addon = null;
        
        hudShowNode?.Dispose();
        hudShowNode = null;
    }
}
