using System;
using System.Collections.Generic;
using Phobos.Diag;
using Phobos.ECS.Components;
using Phobos.ECS.Entities;
using Phobos.Navigation;

namespace Phobos.ECS.Systems;

public class SquadTaskSystem(ObjectiveSystem objectiveSystem, LocationQueue locationQueue)
{
    public void Update(List<Squad> squads)
    {
        for (var i = 0; i < squads.Count; i++)
        {
            var squad = squads[i];
            
            if (squad.TargetLocation == null)
            {
                // If the squad does not have an objective yet, grab one.
                var location = locationQueue.Next();
                squad.TargetLocation = location;
                DebugLog.Write($"Assigned {location} to {squad}");
            }

            var finishedCount = 0;
            
            // If the squad has an objective, make sure all the members get it
            for (var j = 0; j < squad.Members.Count; j++)
            {
                var member = squad.Members[j];
                
                if (!member.IsActive)
                    continue;

                switch (member.Objective.Status)
                {
                    case ObjectiveStatus.Suspended:
                        objectiveSystem.BeginObjective(member, squad.TargetLocation);
                        break;
                    case ObjectiveStatus.Active:
                        break;
                    case ObjectiveStatus.Completed:
                    case ObjectiveStatus.Failed:
                        finishedCount++;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (finishedCount == squad.Count)
            {
                DebugLog.Write($"{squad} objective finished, resetting target locations.");
                squad.TargetLocation = null;
                
                for (var j = 0; j < squad.Members.Count; j++)
                {
                    var member = squad.Members[j];
                    member.Objective.Status = ObjectiveStatus.Suspended;
                    member.Objective.Location = null;
                }
            }
        }
    }
}