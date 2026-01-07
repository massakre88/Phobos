using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EFT.Interactive;
using Phobos.Components;
using Phobos.Diag;
using Phobos.Entities;
using Phobos.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Phobos.Systems;

public class MovementSystem(NavJobExecutor navJobExecutor)
{
    private const int RetryLimit = 10;
    private const float CornerReachedWalkDistSqr = 0.15f * 0.15f;
    private const float CornerReachedSprintDistSqr = 0.3f * 0.3f;
    private const float TargetReachedDistSqr = 1f;

    private readonly Queue<ValueTuple<Agent, NavJob>> _moveJobs = new(20);

    public void Update(List<Agent> liveAgents)
    {
        if (_moveJobs.Count > 0)
        {
            for (var i = 0; i < _moveJobs.Count; i++)
            {
                var (agent, job) = _moveJobs.Dequeue();

                // If the job is not ready, re-enqueue and skip to the next
                if (!job.IsReady)
                {
                    _moveJobs.Enqueue((agent, job));
                    continue;
                }

                // Discard the move job if the agent is inactive (Phobos might be deactivated, or the bot died, etc...)
                if (!agent.IsActive)
                    continue;

                StartMovement(agent, job);
            }
        }

        for (var i = 0; i < liveAgents.Count; i++)
        {
            var agent = liveAgents[i];

            // Bail out if the agent is inactive
            if (!agent.IsActive)
            {
                if (agent.Movement.IsValid)
                {
                    ResetPath(agent.Movement);
                }

                continue;
            }

            UpdateMovement(agent);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MoveToByPath(Agent agent, Vector3 destination)
    {
        ScheduleMoveJob(agent, destination);
        agent.Movement.Retry = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MoveToDirect(Agent agent, Vector3 destination)
    {
        throw new NotImplementedException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void MoveRetry(Agent agent, Vector3 destination)
    {
        ScheduleMoveJob(agent, destination);
        agent.Movement.Retry++;
    }

    private void ScheduleMoveJob(Agent agent, Vector3 destination)
    {
        // Queues up a pathfinding job, once that's ready, we move the bot along the path.
        var job = navJobExecutor.Submit(agent.Bot.Position, destination);
        _moveJobs.Enqueue((agent, job));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void StartMovement(Agent agent, NavJob job)
    {
        if (job.Status == NavMeshPathStatus.PathInvalid)
        {
            agent.Movement.Target = job.Target;
            agent.Movement.Status = NavMeshPathStatus.PathInvalid;
            ResetPath(agent.Movement);
            return;
        }

        AssignTarget(agent.Movement, job);

        agent.Bot.Mover.Stop();

        // Debug
        PathVis.Show(job.Path, thickness: 0.1f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateMovement(Agent agent)
    {
        var bot = agent.Bot;
        var player = agent.Player;
        var movement = agent.Movement;

        if (movement.Path == null || movement.Path.Length == 0 || movement.Status == NavMeshPathStatus.PathInvalid)
            return;

        var moveSpeedMult = 1f;

        // Door handling
        if (HandleDoors(agent))
        {
            moveSpeedMult = 0.25f;
        }

        // Speed
        var movementSpeed = movement.Speed * moveSpeedMult;
        var speedDelta = movementSpeed - player.Speed;
        if (Math.Abs(speedDelta) > 1e-8)
        {
            bot.Mover.SetTargetMoveSpeed(movementSpeed);
            // player.ChangeSpeed(speedDelta);
        }

        // Sprint
        if (movement.Sprint != bot.Mover.Sprinting)
        {
            bot.Mover.Sprint(movement.Sprint);
            // player.EnableSprint(movement.Sprint);
        }

        // Pose
        var poseDelta = movement.Pose - player.PoseLevel;
        if (Math.Abs(poseDelta) > 1e-2)
        {
            bot.SetPose(movement.Pose);
            // player.ChangePose(poseDelta);
        }

        // Prone
        if (bot.BotLay.IsLay != movement.Prone)
        {
            if (movement.Prone)
            {
                bot.BotLay.TryLay();
            }
            else
            {
                bot.BotLay.GetUp(true);
            }
        }
        
        // Path handling
        var moveVector = movement.Path[movement.CurrentCorner] - bot.Position;
        var nextCornerIndex = movement.CurrentCorner + 1;
        var hasNextCorner = nextCornerIndex < movement.Path.Length;

        if (hasNextCorner)
        {
            var cornerReached = false;
            var cornerReachedEps = bot.Mover.Sprinting ? CornerReachedSprintDistSqr : CornerReachedWalkDistSqr;
            var moveVectorSqrMag = moveVector.sqrMagnitude;

            if (moveVectorSqrMag <= cornerReachedEps)
            {
                cornerReached = true;
            }
            else if (moveVectorSqrMag < 1f)
            {
                var nextCorner = movement.Path[nextCornerIndex];

                if (!NavMesh.Raycast(bot.Position, nextCorner, out _, NavMesh.AllAreas))
                {
                    cornerReached = true;
                }
            }

            if (cornerReached)
            {
                movement.CurrentCorner = nextCornerIndex;
                moveVector = movement.Path[movement.CurrentCorner] - bot.Position;
            }
        }
        else
        {
            if (movement.Status == NavMeshPathStatus.PathPartial)
            {
                ResetPath(movement);

                if (movement.Retry >= RetryLimit)
                {
                    movement.Status = NavMeshPathStatus.PathInvalid;
                    return;
                }

                MoveRetry(agent, movement.Target);
                return;
            }

            // Sometimes the last movement corner might not be exactly on the actual target. Add an extra check to short circuit.
            if ((movement.Target - bot.Position).sqrMagnitude <= TargetReachedDistSqr
                || (movement.Path[movement.CurrentCorner] - bot.Position).sqrMagnitude <= TargetReachedDistSqr)
            {
                ResetPath(movement);
                return;
            }
        }

        // Steering
        moveVector.Normalize();
        var moveDir = CalcMoveDirection(moveVector, player.Rotation);
        player.CharacterController.SetSteerDirection(moveVector);
        player.Move(moveDir);
        bot.AimingManager.CurrentAiming.Move(player.Speed);

        if (movement.VoxelUpdatePacing.Allowed())
        {
            bot.AIData.SetPosToVoxel(bot.Position);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector2 CalcMoveDirection(Vector3 direction, Vector2 rotation)
    {
        var vector = Quaternion.Euler(0f, 0f, rotation.x) * new Vector2(direction.x, direction.z);
        return new Vector2(vector.x, vector.y);
    }

    private static bool HandleDoors(Agent agent)
    {
        var currentVoxel = agent.Bot.VoxelesPersonalData.CurVoxel;

        if (currentVoxel == null) return false;

        if (currentVoxel.DoorLinks.Count == 0)
            return false;

        var foundDoors = false;

        for (var i = 0; i < currentVoxel.DoorLinks.Count; i++)
        {
            var door = currentVoxel.DoorLinks[i].Door;
            var shouldOpen = door.enabled && door.Operatable && door.DoorState != EDoorState.Open &&
                             (door.transform.position - agent.Bot.Position).sqrMagnitude < 9f;

            if (!shouldOpen) continue;

            if (door.Collider != null)
            {
                agent.Bot.GetPlayer.MovementContext.IgnoreInteractionCollision(door.Collider, true);
            }

            door.Open();
            foundDoors = true;
        }

        return foundDoors;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ResetPath(Movement movement)
    {
        movement.Path = null;
        movement.CurrentCorner = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AssignTarget(Movement movement, NavJob job)
    {
        movement.Target = job.Target;
        movement.Path = job.Path;
        movement.Status = job.Status;
        movement.CurrentCorner = 0;
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