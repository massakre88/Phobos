using Phobos.Components;
using Phobos.Components.Squad;
using Phobos.Data;
using Phobos.Diag;
using Phobos.Entities;
using Phobos.Navigation;
using Phobos.Systems;
using Phobos.Tasks.Actions;
using UnityEngine;

namespace Phobos.Tasks.Strategies;

public class GotoObjectiveStrategy(SquadData squadData, LocationSystem locationSystem, float hysteresis) : Task<Squad>(hysteresis)
{
    public override void UpdateScore(int ordinal)
    {
        var squads = squadData.Entities.Values;
        for (var i = 0; i < squads.Count; i++)
        {
            var squad = squads[i];
            squad.TaskScores[ordinal] = 0.5f;
        }
    }

    public override void Update()
    {
        for (var i = 0; i < ActiveEntities.Count; i++)
        {
            var squad = ActiveEntities[i];
        
            var finishedCount = 0;
            
            for (var j = 0; j < squad.Size; j++)
            {
                var agent = squad.Members[j];
        
                if (agent.Objective.Location != squad.Objective.Location)
                {
                    agent.Objective.Location = squad.Objective.Location;
                    agent.Objective.Status = ObjectiveStatus.Suspended;
                    DebugLog.Write($"{agent} assigned objective {squad.Objective.Location}");
                }

                if (agent.Objective.Location == null)
                {
                    continue;
                }

                if (agent.Objective.Status == ObjectiveStatus.Failed
                    || (agent.Objective.Location.Position - agent.Bot.Position).sqrMagnitude <= GotoObjectiveAction.ObjectiveEpsDistSqr)
                {
                    finishedCount++;
                }
            }

            
            if (squad.Objective.Location != null && finishedCount != squad.Size) continue;

            if (squad.Objective.Location != null)
            {
                locationSystem.Return(squad.Objective.Location);
                DebugLog.Write($"{squad} returned objective {squad.Objective.Location}");
            }
            
            var newLocation = locationSystem.RequestNear(squad.Leader.Bot.Position, squad.Objective.LocationPrevious);

            if (newLocation == null)
            {
                DebugLog.Write($"{squad} received null objective location");
                continue;
            }
                
            squad.Objective.LocationPrevious = squad.Objective.Location;
            squad.Objective.Location = newLocation;
                
            DebugLog.Write($"{squad} assigned objective {squad.Objective.Location}");
        }
    }
}