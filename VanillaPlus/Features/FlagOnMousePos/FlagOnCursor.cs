using System.Numerics;
using Dalamud.Game.Command;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.FlagOnMousePos;

public unsafe class FlagOnCursor : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Place Flag on Cursor",
        Description = "Places a flag on the map for where your cursor is pointing in the world.",
        Type = ModificationType.UserInterface,
        Authors = [ "QLEDHDTV" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    private const string CommandName = "/flagthere";

    public override void OnEnable() => Services.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) {
        HelpMessage = "Place a flag at mouse position",
        ShowInHelp = true,
    });

    public override void OnDisable()
        => Services.CommandManager.RemoveHandler(CommandName);

    private static void OnCommand(string command, string args) {
        ref var cursorData = ref UIInputData.Instance()->UIFilteredCursorInputs;
        var position = new Vector2(cursorData.PositionX, cursorData.PositionY);

        if (Services.GameGui.ScreenToWorld(position, out var mouseWorldPos)) {
            var agentMap = AgentMap.Instance();

            agentMap->FlagMarkerCount = 0;
            agentMap->SetFlagMapMarker(agentMap->CurrentTerritoryId, agentMap->CurrentMapId, mouseWorldPos);
            AgentChatLog.Instance()->InsertTextCommandParam(1048, false);
        }
    }
}
