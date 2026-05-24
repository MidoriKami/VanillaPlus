using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Game.Command;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.FlagOnCursor;

public unsafe class FlagOnCursor : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_FlagOnCursor,
        Description = Strings.ModificationDescription_FlagOnCursor,
        Type = ModificationType.UserInterface,
        Authors = ["QLEDHDTV"],
    };

    private const string CommandName = "/flagthere";

    public override Task OnEnableAsync() {
        Services.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) {
            HelpMessage = Strings.FlagOnCursor_CommandHelpMessage,
            ShowInHelp = true,
        });

        return Task.CompletedTask;
    }

    public override Task OnDisableAsync() {
        Services.CommandManager.RemoveHandler(CommandName);

        return Task.CompletedTask;
    }

    private static void OnCommand(string command, string args) {
        ref var cursorData = ref UIInputData.Instance()->UIFilteredCursorInputs;
        var position = new Vector2(cursorData.PositionX, cursorData.PositionY);

        if (Services.GameGui.ScreenToWorld(position, out var mouseWorldPos)) {
            var agentMap = AgentMap.Instance();

            agentMap->FlagMarkerCount = 0;
            agentMap->SetFlagMapMarker(agentMap->CurrentTerritoryId, agentMap->CurrentMapId, mouseWorldPos);

            Services.Framework.RunOnTick(() => {
                AgentChatLog.Instance()->InsertTextCommandParam(1048, false);
            });
        }
    }
}
