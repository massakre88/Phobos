using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Phobos.Diag;
using Phobos.ECS.Components;
using Phobos.ECS.Entities;
using Phobos.Helpers;
using Phobos.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Phobos.ECS.Systems;

public class MovementSystem(NavJobExecutor navJobExecutor, ActorList liveActors)
{
    private const int RetryLimit = 10;

    private const float TargetReachedDistanceSqr = 5f * 5f;
    private const float TargetVicinityDistanceSqr = 35f * 35f;
    private const float LookAheadDistanceSqr = 1.5f;

    private readonly Queue<ValueTuple<Actor, NavJob>> _moveJobs = new(20);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MoveToDestination(Actor actor, Vector3 destination)
    {
        ScheduleMoveJob(actor, destination);
        actor.Movement.Retry = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MoveRetry(Actor actor)
    {
        MoveRetry(actor, actor.Movement.Target.Position);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MoveRetry(Actor actor, Vector3 destination)
    {
        ScheduleMoveJob(actor, destination);
        actor.Movement.Retry++;
    }

    public void Update()
    {
        if (_moveJobs.Count > 0)
        {
            for (var i = 0; i < _moveJobs.Count; i++)
            {
                var (actor, job) = _moveJobs.Dequeue();

                // If the job is not ready, re-enqueue and skip to the next
                if (!job.IsReady)
                {
                    _moveJobs.Enqueue((actor, job));
                    continue;
                }

                // Discard the move job if the actor is inactive (Phobos might be deactivated, or the bot died, etc...)
                if (!actor.IsActive)
                    continue;

                StartMovement(actor, job);
            }
        }

        for (var i = 0; i < liveActors.Count; i++)
        {
            var actor = liveActors[i];

            // Bail out if the actor is inactive
            if (!actor.IsActive)
            {
                // Set status to suspended if we were active
                if (actor.Movement.Status == MovementStatus.Active)
                    actor.Movement.Status = MovementStatus.Suspended;

                continue;
            }

            UpdateMovement(actor);
        }
    }

    private void ScheduleMoveJob(Actor actor, Vector3 destination)
    {
        // Queues up a pathfinding job, once that's ready, we move the bot along the path.
        NavMesh.SamplePosition(actor.Bot.Position, out var origin, 5f, NavMesh.AllAreas);
        var job = navJobExecutor.Submit(origin.position, destination);
        _moveJobs.Enqueue((actor, job));
        actor.Movement.Status = MovementStatus.Suspended;
        // DebugLog.Write($"{actor} {actor.Movement} move job scheduled");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void StartMovement(Actor actor, NavJob job)
    {
        if (job.Status == NavMeshPathStatus.PathInvalid)
        {
            actor.Movement.Target = null;
            actor.Movement.Status = MovementStatus.Failed;
            return;
        }

        actor.Movement.Set(job);
        actor.Movement.Status = MovementStatus.Active;

        actor.Bot.Mover.GoToByWay(job.Path, 2);
        actor.Bot.Mover.ActualPathFinder.SlowAtTheEnd = true;

        // Debug
        PathVis.Show(job.Path, thickness: 0.1f);
        // DebugLog.Write($"{actor} {actor.Movement} movement commenced");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateMovement(Actor actor)
    {
        var bot = actor.Bot;
        var movement = actor.Movement;
        
        // Movement control - must be always applied if we are active.
        // The sprint flag has to be enforced on every frame, as the BSG code can sometimes decide to change it randomly.
        // We disable sprint for now as it looks jank and can get bots into weird spots sometimes.
        bot.Mover.Sprint(false);

        if (movement.Status is MovementStatus.Failed or MovementStatus.Suspended)
            return;

        // Failsafe
        if (movement.Target == null)
        {
            movement.Status = MovementStatus.Suspended;
            Plugin.Log.LogError($"Null target for {actor} even though the status is {movement.Status}");
            return;
        }

        movement.Target.DistanceSqr = (movement.Target.Position - bot.Position).sqrMagnitude;

        // Handle the case where we aren't following a path for some reason. The usual reasons are:
        // 1. The path was partial, and we arrived at the end
        // 2. We arrived at the destination.
        if (movement.ActualPath == null)
        {
            // If we arrived at the destination and have no active path, we are done
            if (movement.Status == MovementStatus.Completed)
            {
                return;
            }

            // Otherwise try to find a new path.
            if (movement.Retry < RetryLimit)
            {
                MoveRetry(actor);
            }
            else
            {
                movement.Status = MovementStatus.Failed;
            }

            return;
        }

        if (movement.Target.DistanceSqr < TargetReachedDistanceSqr)
        {
            movement.Status = MovementStatus.Completed;
        }

        // We'll enforce these whenever the bot is under way
        bot.SetPose(1f);
        bot.BotLay.GetUp(true);

        // Bot movement control
        // TODO: Add target speed as a variable in the Movement component and then have the objective tracking update it
        //       We'll always set the target speed irrespective of a movement target being present.
        var targetSpeed = Mathf.Lerp(0.65f, 1f, movement.Target.DistanceSqr / TargetVicinityDistanceSqr);
        bot.SetTargetMoveSpeed(targetSpeed);
        
        var lookPoint = PathHelper.CalculateForwardPointOnPath(
            movement.ActualPath.Vector3_0, bot.Position, movement.ActualPath.CurIndex, LookAheadDistanceSqr
        ) + 1.5f * Vector3.up;
        bot.Steering.LookToPoint(lookPoint, 360f);
    }

    // private static bool ShouldSprint(Actor actor)
    // {
    //     var bot = actor.Bot;
    //     var isFarFromDestination = actor.Movement.Target.DistanceSqr > TargetVicinityDistanceSqr;
    //     var isOutside = bot.AIData.EnvironmentId == 0;
    //     var isAbleToSprint = !bot.Mover.NoSprint && bot.GetPlayer.MovementContext.CanSprint;
    //     var isPathSmooth = CalculatePathAngleJitter(
    //         bot.Position,
    //         actor.Movement.ActualPath.Vector3_0,
    //         actor.Movement.ActualPath.CurIndex
    //     ) < 15f;
    //
    //     return isOutside && isAbleToSprint && isPathSmooth && isFarFromDestination;
    // }
}