using System.Collections.Generic;
using Phobos.Data;
using Phobos.Diag;
using Phobos.Entities;

namespace Phobos.Orchestration;

public class SquadRegistry(SquadData squadData, StrategyManager strategyManager)
{
    private readonly Dictionary<int, int> _squadIdMap = new(16);
    
    public void AddAgent(Agent agent)
    {
        var squad = squadData.AddEntity(strategyManager.Tasks.Length);
        DebugLog.Write($"Registered new {squad}");
            
        squad.Leader = agent;
        squad.Leader.IsLeader = true;
        DebugLog.Write($"{squad} assigned new leader {squad.Leader}");

        squad.AddAgent(agent);
        agent.Squad = squad;
        DebugLog.Write($"Added {agent} to {squad} with {squad.Size} members");
    }

    public void RemoveAgent(Agent agent)
    {
        var squad = squadData.Entities[agent.Squad.Id];
        squad.RemoveAgent(agent);
        DebugLog.Write($"Removed {agent} from {squad} with {squad.Size} members remaining");

        if (squad.Size > 0)
        {
            // Reassign squad leader if neccessary
            if (agent != squad.Leader) return;
            
            squad.Leader = squad.Members[^1];
            squad.Leader.IsLeader = true;
            DebugLog.Write($"{squad} assigned new leader {squad.Leader}");
            return;
        }

        DebugLog.Write($"Removing empty {squad}");
        _squadIdMap.Remove(agent.Bot.BotsGroup.Id);
        squadData.Entities.Remove(squad);
        strategyManager.RemoveEntity(squad);
    }
}