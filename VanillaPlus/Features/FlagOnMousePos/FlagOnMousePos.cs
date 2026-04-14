using Dalamud.Bindings.ImGui;
using Dalamud.Game.Command;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System;
using System.Numerics;
using System.Threading;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.FlagOnMousePos;
public class FlagOnMousePos : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_SampleGameModification,
        Description = Strings.ModificationDescription_SampleGameModification,
        Type = ModificationType.UserInterface,
        Authors = [ "QLEDHDTV" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    private const string CommandName = "/there";

    public override void OnEnable() {
        Services.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) {
            HelpMessage = "Place a flag at mouse position"
        });
    }

    public override void OnDisable() {
        Services.CommandManager.RemoveHandler(CommandName);
    }

    private unsafe void OnCommand(string command, string args) {
        Services.GameGui.ScreenToWorld(ImGui.GetIO().MousePos, out Vector3 mouseWorldPos);
        if (mouseWorldPos.X != 0 && mouseWorldPos.Y != 0 && mouseWorldPos.Z != 0) {
            AgentMap.Instance()->FlagMarkerCount = 0;
            AgentMap.Instance()->SetFlagMapMarker(Services.ClientState.TerritoryType, Services.ClientState.MapId, mouseWorldPos);
            AgentChatLog.Instance()->InsertTextCommandParam(1048, false);

        }
        
    }
}
