using Phobos.Diag;
using Phobos.ECS.Components;
using Phobos.ECS.Entities;
using Phobos.Objectives;

namespace Phobos.ECS.Systems;

public class ActorTaskSystem(MovementSystem movementSystem, ActorList liveActors)
{
    public void AssignObjective(Actor actor, Objective objective)
    {
        actor.Task.Assign(objective);
        movementSystem.MoveToDestination(actor, objective.Position);
        DebugLog.Write($"Assigned {objective} to {actor}");
    }

    public void Update()
    {
        for (var i = 0; i < liveActors.Count; i++)
        {
            var actor = liveActors[i];

            if (actor.Movement.Status == MovementStatus.Completed)
            {
                actor.IsPhobosActive = false;
            }
        }
        // TODO:
        // Track the objective for each actor, update it when finished.
        // Flag it as failed if it doesn't work for whatever reason.
    }
}