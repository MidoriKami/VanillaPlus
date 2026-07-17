using System.Collections.Generic;
using System.Threading.Tasks;
using Dalamud.Game.Gui.NamePlate;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using DalamudObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;

namespace VanillaPlus.Features.DutyPortalNameplateOffset;

public class DutyPortalNameplateOffset : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_DutyPortalNameplateOffset,
        Description = Strings.ModificationDescription_DutyPortalNameplateOffset,
        Type = ModificationType.UserInterface,
        Authors = ["Epinephren"],
    };

    public override string ImageName => "DutyPortalNameplateOffset.png";

    // Slightly below SE's current elevated raid nameplate height.
    private const float RaisedNameplateY = 3.1f;

    public override Task OnEnableAsync() {
        Services.GetService<INamePlateGui>().OnDataUpdate += OnNamePlateUpdate;

        return Task.CompletedTask;
    }

    public override Task OnDisableAsync() {
        Services.GetService<INamePlateGui>().OnDataUpdate -= OnNamePlateUpdate;

        return Task.CompletedTask;
    }

    private static unsafe void OnNamePlateUpdate(INamePlateUpdateContext context, IReadOnlyList<INamePlateUpdateHandler> handlers) {
        foreach (var handler in handlers) {

            if (handler is { GameObject: { ObjectKind: DalamudObjectKind.EventObj, EntityId: not 0xE0000000 } }) {

                var nativeObject = (GameObject*)handler.GameObject.Address;

                if (nativeObject->EventId is { Id: not 0, EntryId: not 60006 } && nativeObject->NameplateOffset.Y < RaisedNameplateY) {

                    nativeObject->NameplateOffset.Y = RaisedNameplateY;
                    nativeObject->NameplateOffsetTarget.Y = RaisedNameplateY;
                }
            }
        }
    }
}
