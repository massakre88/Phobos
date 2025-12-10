using System.Runtime.CompilerServices;
using Phobos.Diag;
using Phobos.ECS.Components;
using Phobos.ECS.Components.Objectives;
using Phobos.ECS.Entities;
using Phobos.Navigation;
using UnityEngine;

namespace Phobos.ECS.Systems.Objectives;

public class QuestObjectiveSystem(MovementSystem movementSystem) : BaseObjectiveSystem
{
    private const float ObjectiveReachedDistSqr = 10f * 10f;
    private const float ObjectiveVicinityDistSqr = 35f * 35f;
    
    public void BeginObjective(Agent agent, Location location)
    {
        var objective = agent.Task.Quest;
        
        objective.Location = location;
        objective.DistanceSqr = (objective.Location.Position - agent.Bot.Position).sqrMagnitude;
        
        // Short circuit if we are already within the AO
        if (objective.DistanceSqr <= ObjectiveReachedDistSqr)
        {
            objective.Status = ObjectiveStatus.Success;
            return;
        }
        
        objective.Status = ObjectiveStatus.Active;
        agent.Movement.Speed = 1f;
        
        AddAgent(agent);
        
        movementSystem.MoveToDestination(agent, location.Position);
        DebugLog.Write($"Assigned {location} to {agent}");
    }

    public override void ResetObjective(Agent agent)
    {
        var objective = agent.Task.Quest;
        objective.Status = ObjectiveStatus.Suspended;
        objective.DistanceSqr = 0f;
        objective.Location = null;
    }
    
    public void Update()
    {
        for (var i = 0; i < Agents.Count; i++)
        {
            var actor = Agents[i];
            UpdateObjective(actor);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateObjective(Agent agent)
    {
        var bot = agent.Bot;
        var objective = agent.Task.Quest;

        if (agent.Movement.Status == MovementStatus.Failed)
        {
            objective.Status = ObjectiveStatus.Failed;
        }
        
        if (objective.Status != ObjectiveStatus.Active)
        {
            RemoveAgent(agent);
            return;
        }
        
        objective.DistanceSqr = (objective.Location.Position - bot.Position).sqrMagnitude;

        if (objective.DistanceSqr <= ObjectiveReachedDistSqr)
        {
            objective.Status = ObjectiveStatus.Success;
            RemoveAgent(agent);
        }
        
        var targetSpeed = Mathf.Lerp(0.5f, 1f, Mathf.Pow(objective.DistanceSqr / ObjectiveVicinityDistSqr, 2));
        agent.Movement.Speed = targetSpeed;
    }
}