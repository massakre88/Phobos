using System;
using System.Collections.Generic;
using Phobos.Diag;
using Phobos.ECS.Components;
using Phobos.ECS.Entities;
using Phobos.ECS.Helpers;
using Phobos.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Phobos.ECS.Systems;

public class MovementSystem(NavJobExecutor navJobExecutor) : BaseActorSystem
{
    private const float SqrDistanceEpsilon = 5f * 5f;
    private const int FramePacingInterval = 5;

    private readonly FramePacing _framePacing = new(FramePacingInterval);
    private readonly Queue<ValueTuple<Actor, NavJob>> _pathJobs = new(20);

    public void MoveTo(Actor actor, Vector3 destination)
    {
        // Queues up a pathfinding job, once that's ready, moves the bot to that path.
        NavMesh.SamplePosition(actor.Bot.Position, out var origin, 5f, NavMesh.AllAreas);
        var job = navJobExecutor.Submit(origin.position, destination);
        _pathJobs.Enqueue((actor, job));
    }

    public void Update()
    {
        // Update the status on every frame
        for (var i = 0; i < Actors.Count; i++)
        {
            var actor = Actors[i];

            if (!actor.IsActive)
                continue;

            UpdateStatus(actor);
        }

        if (_framePacing.Blocked())
            return;

        if (_pathJobs.Count > 0)
        {
            for (var i = 0; i < _pathJobs.Count; i++)
            {
                var (actor, job) = _pathJobs.Dequeue();

                // If the job is not ready, re-enqueue and skip to the next
                if (!job.IsReady)
                {
                    _pathJobs.Enqueue((actor, job));
                    continue;
                }

                // Bail out if the actor is inactive or the pathfinding failed
                if (!actor.IsActive || job.Status == NavMeshPathStatus.PathInvalid)
                    continue;

                StartMovement(actor, job);
            }
        }

        for (var i = 0; i < Actors.Count; i++)
        {
            var actor = Actors[i];

            // Bail out if the actor is inactive
            if (!actor.IsActive)
                continue;

            UpdateMovement(actor);
        }
    }

    private static void UpdateStatus(Actor actor)
    {
        var bot = actor.Bot;

        if (bot.Mover.ActualPathController.CurPath == null)
            return;

        var routing = actor.Routing;

        routing.Update(actor.Bot);

        if (routing.SqrDistance < SqrDistanceEpsilon)
            routing.Status = RoutingStatus.Completed;
    }

    private static void StartMovement(Actor actor, NavJob job)
    {
        var routing = actor.Routing;
        routing.Set(job);
        actor.Bot.Mover.GoToByWay(routing.Path.Corners, 1);
        actor.Bot.Mover.ActualPathFinder.SlowAtTheEnd = true;

        // Debug
        PathVis.Show(routing.Path.Corners, thickness: 0.1f);
    }

    private void UpdateMovement(Actor actor)
    {
        var bot = actor.Bot;

        // Skip bots with no current pathing
        if (bot.Mover.ActualPathController.CurPath == null)
            return;

        // We'll enforce these whenever the bot is under way
        bot.SetPose(1f);
        bot.BotLay.GetUp(true);

        // Bots will not move at full speed without this
        bot.SetTargetMoveSpeed(1f);

        var shouldSprint = ShouldSprint(actor);
        bot.Mover.Sprint(shouldSprint);

        if (shouldSprint)
        {
            bot.Steering.LookToMovingDirection(520);
        }
        else
        {
            // Make the bot look 2-3 points ahead if not running 
            var lookIndex = Mathf.Min(actor.Routing.Path.Corners.Length - 1, actor.Routing.CurrentCorner + 2);
            bot.Steering.LookToPoint(actor.Routing.Path.Corners[lookIndex], 520);
        }
    }

    private static bool ShouldSprint(Actor actor)
    {
        var bot = actor.Bot;
        var isFarFromDestination = actor.Routing.SqrDistance > SqrDistanceEpsilon;
        var isOutside = bot.AIData.EnvironmentId == 0;
        var isAbleToSprint = !bot.Mover.NoSprint && bot.GetPlayer.MovementContext.CanSprint;
        var isPathSmooth = CalculatePathAngleJitter(
            bot.Mover.ActualPathController.CurPath.Vector3_0,
            bot.Mover.ActualPathController.CurPath.CurIndex
        ) < 10f;

        return isOutside && isAbleToSprint && isPathSmooth && isFarFromDestination;
    }

    private static float CalculatePathAngleJitter(Vector3[] pathCorners, int startIndex = 0, int count = 3)
    {
        // Validate inputs
        if (pathCorners == null || pathCorners.Length < 3)
            return 0f;

        if (startIndex < 0 || startIndex >= pathCorners.Length - 2)
            return 0f;

        // Clamp count to available corners
        var maxCount = pathCorners.Length - startIndex - 2;
        if (count > maxCount)
            count = maxCount;

        if (count <= 0)
            return 0f;

        var angleMax = 0f;

        // Calculate angles between consecutive segments
        for (var i = 0; i < count; i++)
        {
            var idx = startIndex + i;
            var pointA = pathCorners[idx];
            var pointB = pathCorners[idx + 1];
            var pointC = pathCorners[idx + 2];

            // Calculate direction vectors
            var directionAb = (pointB - pointA).normalized;
            var directionBc = (pointC - pointB).normalized;

            // Calculate angle between the two direction vectors
            var angle = Vector3.Angle(directionAb, directionBc);
            if (angle > angleMax)
                angleMax = angle;
        }

        return angleMax;
    }
}