using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Agent;
using Dalamud.Game.Agent.AgentArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace VanillaPlus.Extensions;

public static unsafe class AgentLifecycleExtensions {
    extension(IAgentLifecycle agentLifecycle) {
        public void LogAgent(AgentId agent, params AgentEvent[] loggedEvents) {
            if (loggedEvents.Length is 0) {
                loggedEvents = [
                    AgentEvent.PreClassJobChange,
                    AgentEvent.PreGameEvent,
                    AgentEvent.PreHide,
                    AgentEvent.PreShow,
                    AgentEvent.PreLevelChange,
                    AgentEvent.PreReceiveEvent, 
                    AgentEvent.PreReceiveEventWithResult,
                ];
            }

            ActiveLoggers.TryAdd(agent, loggedEvents.ToList());
            foreach (var agentId in loggedEvents) {
                agentLifecycle.RegisterListener(agentId, agent, LogEventMessage);
            }
        }

        public void UnLogAgent(AgentId agent) {
            if (!ActiveLoggers.TryGetValue(agent, out var loggedModules)) return;

            foreach (var loggedModule in loggedModules) {
                agentLifecycle.UnregisterListener(loggedModule, agent, LogEventMessage);
            }
        }
    }

    private static readonly Dictionary<AgentId, List<AgentEvent>> ActiveLoggers = [];
    
    private static void LogEventMessage(AgentEvent type, AgentArgs args) {
        var logString = $"[{args.AgentId}] [{type}] ";

        switch (type) {
            case AgentEvent.PreReceiveEvent or AgentEvent.PreReceiveEventWithResult when args is AgentReceiveEventArgs receiveEventArgs:
                logString += $"Event Param: {receiveEventArgs.EventKind}, AtkValue Count: {receiveEventArgs.ValueCount} ";

                var index = 0;
                foreach (var value in receiveEventArgs.AtkValueEnumerable) {
                    if (value.IsNull) continue;

                    var valuePointer = (AtkValue*)value.Address;
                    logString += $"\n[{index++}] [{valuePointer->Type}] {valuePointer->GetValueAsString().IfEmpty("empty")}";
                }
                break;

            case AgentEvent.PreGameEvent when args is AgentGameEventArgs gameEventArgs:
                logString += $"Event Id: {gameEventArgs.GameEvent}";
                break;

            case AgentEvent.PreLevelChange when args is AgentLevelChangeArgs levelChangeArgs:
                logString += $"ClassJob: {levelChangeArgs.ClassJobId}, Level: {levelChangeArgs.Level}";
                break;

            case AgentEvent.PreClassJobChange when args is AgentClassJobChangeArgs classJobChangeArgs:
                logString += $"ClassJob: {classJobChangeArgs.ClassJobId}";
                break;

            case AgentEvent.PreShow:
            case AgentEvent.PreHide:
            case AgentEvent.PreUpdate:
                break;
        }
        
        Services.PluginLog.Information(logString);
    }
}
