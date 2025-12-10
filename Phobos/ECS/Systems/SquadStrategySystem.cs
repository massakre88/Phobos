using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Phobos.Diag;
using Phobos.ECS.Components.Objectives;
using Phobos.ECS.Entities;
using Phobos.ECS.Helpers;
using Phobos.ECS.Systems.Objectives;
using Phobos.Navigation;

namespace Phobos.ECS.Systems;

public class SquadStrategySystem
{
    private readonly TimePacing _pacing = new(1f);
    
    private readonly QuestObjectiveSystem _questObjectiveSystem;
    private readonly GuardObjectiveSystem _guardObjectiveSystem;
    private readonly AssistObjectiveSystem _assistObjectiveSystem;
    
    private readonly LocationQueue _locationQueue;
    
    private readonly BaseObjectiveSystem[] _objectiveSystems;

    public SquadStrategySystem(
        QuestObjectiveSystem questObjectiveSystem, GuardObjectiveSystem guardObjectiveSystem, AssistObjectiveSystem assistObjectiveSystem,
        LocationQueue locationQueue
        )
    {
        _questObjectiveSystem = questObjectiveSystem;
        _guardObjectiveSystem = guardObjectiveSystem;
        _assistObjectiveSystem = assistObjectiveSystem;
        
        _locationQueue = locationQueue;
        
        _objectiveSystems = new BaseObjectiveSystem[Enum.GetNames(typeof(ObjectiveType)).Length];
        _objectiveSystems[(int)ObjectiveType.Quest] = questObjectiveSystem;
        _objectiveSystems[(int)ObjectiveType.Guard] = guardObjectiveSystem;
        _objectiveSystems[(int)ObjectiveType.Assist] = assistObjectiveSystem;
    }


    public void Update(List<Squad> squads)
    {
        if (_pacing.Blocked())
            return;
        
        for (var i = 0; i < squads.Count; i++)
        {
            var squad = squads[i];

            UpdateSquad(squad);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateSquad(Squad squad)
    {
        if (squad.Objective == null)
        {
            // If the squad does not have an objective yet, grab one.
            var location = _locationQueue.Next();
            squad.Objective = location;
            DebugLog.Write($"Assigned {location} to {squad}");
        }

        var finishedCount = 0;
        
        for (var i = 0; i < squad.Members.Count; i++)
        {
            var member = squad.Members[i];
            var task = member.Task;
            
            if (!member.IsActive)
            {
                if (task.Current != null)
                {
                    _objectiveSystems[task.Current.TypeId].RemoveAgent(member);
                    task.Current = null;
                }
                
                // If we haven't failed, suspend the strategic task so we can resume
                if (task.Quest.Status != ObjectiveStatus.Failed && task.Quest.Status != ObjectiveStatus.Suspended)
                    _questObjectiveSystem.ResetObjective(member);
            }
            else
            {
                var prevTask = task.Current;
                
                // TODO: Add back the objective status to the Objective base class
                //       We'll need it to be able to determine that the current task completed and we can switch to something else,
                //       Otherwise the quest task will interrupt the assist task. 
                
                if (task.Current != task.Assist && TryFindSqadMemberInCombat(out _))
                {
                    DebugLog.Write($"Assigning {member} to Assist objective.");
                    // TODO: Implement assist
                    
                    // Needs an unconditional reset since the bot may move away even from a completed objective.
                    if (task.Quest.Status != ObjectiveStatus.Failed)
                        _questObjectiveSystem.ResetObjective(member);
                }
                else if (task.Current != task.Guard && task.Quest.Status == ObjectiveStatus.Success)
                {
                    DebugLog.Write($"Assigning {member} to Guard objective.");
                    // TODO: Implement guarding
                }
                else if (task.Current != task.Quest && task.Quest.Status == ObjectiveStatus.Suspended)
                {
                    _questObjectiveSystem.BeginObjective(member, squad.Objective);
                }
                
                // The objective changed, remove the agent from the previous objective
                // NB: We can't reset the objective here because it'd potentially reset the quest status even if we are just guarding
                if (prevTask != task.Current && prevTask != null)
                {
                    _objectiveSystems[task.Current.TypeId].RemoveAgent(member);
                }
            }

            if (task.Quest.Status is ObjectiveStatus.Success or ObjectiveStatus.Failed)
            {
                finishedCount++;
            }
        }
        
        if (finishedCount == squad.Count)
        {
            DebugLog.Write($"{squad} objective finished, resetting target locations.");
            squad.Objective = null;

            for (var i = 0; i < squad.Members.Count; i++)
            {
                var member = squad.Members[i];
                
                member.IsPhobosActive = true;
                member.Task.Current = null;
                
                for (var j = 0; j < _objectiveSystems.Length; j++)
                {
                    var system = _objectiveSystems[j];
                    system.RemoveAgent(member);
                    system.ResetObjective(member);
                }
            }
        }
    }

    private static bool TryFindSqadMemberInCombat(out Agent agent)
    {
        agent = null;
        return false;
    }
}