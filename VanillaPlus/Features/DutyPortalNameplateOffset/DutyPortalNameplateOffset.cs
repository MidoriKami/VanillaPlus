using System.Collections.Generic;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.Gui.NamePlate;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.DutyPortalNameplateOffset;

public class DutyPortalNameplateOffset : GameModification {
    private const float RaisedNameplateY = 3.1f; // Slightly below SE's current elevated raid nameplate height.

    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Raise Duty Portal Nameplates",
        Description = "Raises certain duty-related interaction nameplates to improve visibility when crowded by player nameplates.",
        Type = ModificationType.UserInterface,
        Authors = ["Epinephren"],
    };
    public override Task OnEnableAsync() {
        Services.NamePlateGui.OnDataUpdate += OnNamePlateUpdate;
        return Task.CompletedTask;
    }

    public override Task OnDisableAsync() {
        Services.NamePlateGui.OnDataUpdate -= OnNamePlateUpdate;
        return Task.CompletedTask;
    }

    private static unsafe void OnNamePlateUpdate(
        INamePlateUpdateContext context,
        IReadOnlyList<INamePlateUpdateHandler> handlers)
    {
        foreach (var handler in handlers)
        {
            var gameObject = handler.GameObject;
            if (gameObject is null)
                continue;

            if (gameObject.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.EventObj)
                continue;

            var nativeObject = (GameObject*)gameObject.Address;
            if (nativeObject is null)
                continue;

            if (nativeObject->EntityId == 0xE0000000)
                continue;

            if (nativeObject->EventId.EntryId == 60006)
                continue;

            if (nativeObject->NameplateOffset.Y >= RaisedNameplateY)
                continue;

            nativeObject->NameplateOffset.Y = RaisedNameplateY;
            nativeObject->NameplateOffsetTarget.Y = RaisedNameplateY;
        }
    }
}
