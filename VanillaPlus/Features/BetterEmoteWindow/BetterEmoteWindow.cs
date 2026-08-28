using System.Numerics;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Features.BetterEmoteWindow.Controllers;

namespace VanillaPlus.Features.BetterEmoteWindow;

public class BetterEmoteWindow : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_BetterEmoteWindow,
        Description = Strings.ModificationDescription_BetterEmoteWindow,
        Type = ModificationType.UserInterface,
        Authors = ["MapleRecall"],
    };

    public override string ImageName => "BetterEmoteWindow.png";

    private EmoteTooltipController? tooltipController;
    private AddonController? emoteController;

    public override async Task OnEnableAsync() {
        unsafe {
            emoteController = new AddonController {
                AddonName = "Emote",
                OnSetup = OnEmoteSetup,
                OnFinalize = OnEmoteFinalize,
            };
        }

        await emoteController.EnableAsync();

        tooltipController = new EmoteTooltipController();
        tooltipController.Enable();
    }

    public override async Task OnDisableAsync() {
        await tooltipController.DisposeAsyncSafe();
        tooltipController = null;

        await emoteController.DisposeAsyncSafe();
        emoteController = null;
    }

    private unsafe void OnEmoteSetup(AtkUnitBase* addon) {

        // Hide Description Panel
        var descriptionPanel = addon->GetNodeById(11);
        if (descriptionPanel is not null) {
            descriptionPanel->Visible = false;
        }

        // Move category sections upwards
        foreach (var nodeId in new[] { 4u, 16u, 21u, 41u }) {
            var categoryNode = addon->GetNodeById(nodeId);
            if (categoryNode is not null) {
                categoryNode->Position -= new Vector2(0.0f, descriptionPanel->Height - 5.0f);
            }
        }

        // Adjust list size
        var listNode = addon->GetComponentListById(4);
        if (listNode is not null) {
            listNode->OwnerNode->AtkResNode.Size += new Vector2(0.0f, descriptionPanel->Height - 5.0f);
            listNode->SetVisibleRowCount((short) (listNode->VisibleRowCount + 3));
        }

        // Trigger layout update
        addon->Size += new Vector2(0.0f, 5.0f);
    }

    private unsafe void OnEmoteFinalize(AtkUnitBase* addon) {

        // Restore Description Panel
        var descriptionPanel = addon->GetNodeById(11);
        if (descriptionPanel is not null) {
            descriptionPanel->Visible = true;
        }

        // Move category sections back
        foreach (var nodeId in new[] { 4u, 16u, 21u, 41u }) {
            var categoryNode = addon->GetNodeById(nodeId);
            if (categoryNode is not null) {
                categoryNode->Position += new Vector2(0.0f, descriptionPanel->Height - 5.0f);
            }
        }

        // Restore list size
        var listNode = addon->GetComponentListById(4);
        if (listNode is not null) {
            listNode->OwnerNode->AtkResNode.Size -= new Vector2(0.0f, descriptionPanel->Height - 5.0f);
            listNode->SetVisibleRowCount((short) (listNode->VisibleRowCount - 3));
        }

        // Trigger layout update
        addon->Size -= new Vector2(0.0f, 5.0f);
    }
}
