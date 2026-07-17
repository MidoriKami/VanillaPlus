using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Game.Command;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.FlagOnCursor;

public class FlagOnCursor : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_FlagOnCursor,
        Description = Strings.ModificationDescription_FlagOnCursor,
        Type = ModificationType.UserInterface,
        Authors = ["QLEDHDTV"],
    };

    private const string CommandName = "/flagthere";

    public override Task OnEnableAsync() {
        ICommandManager.Get().AddHandler(CommandName, new CommandInfo(OnCommand) {
            HelpMessage = Strings.FlagOnCursor_CommandHelpMessage,
            ShowInHelp = true,
        });

        return Task.CompletedTask;
    }

    public override Task OnDisableAsync() {
        ICommandManager.Get().RemoveHandler(CommandName);

        return Task.CompletedTask;
    }

    private static unsafe void OnCommand(string command, string args) {
        ref var cursorData = ref UIInputData.Instance()->UIFilteredCursorInputs;
        var position = new Vector2(cursorData.PositionX, cursorData.PositionY);

        if (IGameGui.Get().ScreenToWorld(position, out var mouseWorldPos)) {
            var agentMap = AgentMap.Instance();

            agentMap->FlagMarkerCount = 0;
            agentMap->SetFlagMapMarker(agentMap->CurrentTerritoryId, agentMap->CurrentMapId, mouseWorldPos);

            IFramework.Get().RunOnTick(() => {
                AgentChatLog.Instance()->InsertTextCommandParam(1048, false);
            });
        }
    }
}
