using System.Linq;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.ShowActiveSigns;

public class ShowActiveSigns : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Show Active Signs",
        Description = "Adds a checkmark icon to the signs window to indicate if a sign is already active on someone.",
        Type = ModificationType.UserInterface,
        Authors = ["MidoriKami"],
    };

    public override string ImageName => "ActiveSigns.png";

    private AddonController? addonController;

    public override async Task OnEnableAsync() {

        unsafe {
            addonController = new AddonController {
                AddonName = "Marker",
                OnUpdate = OnMarkerUpdate,
                OnFinalize = OnMarkerFinalize,
            };
        }

        await addonController.EnableAsync();
    }

    public override async Task OnDisableAsync() {
        await addonController.DisposeAsyncSafe();
        addonController = null;
    }

    private static unsafe void OnMarkerUpdate(AtkUnitBase* addon) {
        var listNode = addon->GetComponentListById(13);
        if (listNode is null) return;

        foreach (var index in Enumerable.Range(0, MarkingController.Instance()->Markers.Length)) {
            var itemRenderer = listNode->GetItemRenderer(index);
            if (itemRenderer is null) continue;

            var imageNode = itemRenderer->GetNodeById(2);
            if (imageNode is null) continue;

            var signIndex = TranslateSignIndex(index);

            var targetObjectId = MarkingController.Instance()->Markers[signIndex].Id;
            if (targetObjectId is 0xE0000000) {
                imageNode->Visible = false;
            }
            else {
                imageNode->Visible = IObjectTable.Get().SearchById(targetObjectId) is not null;
            }
        }
    }

    private static unsafe void OnMarkerFinalize(AtkUnitBase* addon) {
        var listNode = addon->GetComponentListById(13);
        if (listNode is null) return;

        foreach (var index in Enumerable.Range(0, MarkingController.Instance()->Markers.Length)) {
            var itemRenderer = listNode->GetItemRenderer(index);
            if (itemRenderer is null) continue;

            var imageNode = itemRenderer->GetNodeById(2);
            if (imageNode is null) continue;

            imageNode->Visible = false;
        }
    }

    // We have to shift 5,6,7 (signs 6, 7, 8) to the end because they were added later.
    private static int TranslateSignIndex(int index) => index switch {
        <= 4 => index,
        <= 7 => index + 9,
        _ => index - 3,
    };
}
