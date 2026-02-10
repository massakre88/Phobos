using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Phobos.Components;
using Phobos.Data;
using Phobos.Diag;
using Phobos.Entities;
using Phobos.Navigation;
using Phobos.Systems;
using Unity.Collections;
using UnityEngine;

namespace Phobos.Tasks.Actions;

public class GuardAction(AgentData dataset, MovementSystem movementSystem, float hysteresis) : Task<Agent>(hysteresis)
{
    private const float UtilityBoost = 0.45f;
    private const float UtilityBase = 0.2f;
    private const float InnerRadiusRatio = 0.95f * 0.95f;
    private const int MaxWatchCandidateCount = 25;

    private readonly List<Vector3> _watchCandidates = [];

    public override void UpdateScore(int ordinal)
    {
        var agents = dataset.Entities.Values;
        for (var i = 0; i < agents.Count; i++)
        {
            var agent = agents[i];
            var location = agent.Objective.Location;

            // If we don't have an objective or cover point selected at all, bail out with zero
            if (location == null || agent.Guard.CoverPoint == null)
            {
                agent.TaskScores[ordinal] = 0;
                continue;
            }

            // The utility quickly increases to 0.65f
            var distSqr = (location.Position - agent.Position).sqrMagnitude;
            var utilityScale = Mathf.InverseLerp(location.RadiusSqr, InnerRadiusRatio * location.RadiusSqr, distSqr);
            agent.TaskScores[ordinal] = UtilityBase + utilityScale * UtilityBoost;
        }
    }

    public override void Update()
    {
        for (var i = 0; i < ActiveEntities.Count; i++)
        {
            var agent = ActiveEntities[i];

            if (agent.Guard.CoverPoint == null)
            {
                continue;
            }

            var coverPoint = agent.Guard.CoverPoint.Value;

            if (MovementSystem.IsMovementTargetCurrent(agent, coverPoint.Position))
            {
                if (agent.Movement.Status == MovementStatus.Moving) continue;

                // If we are no longer moving, crouch
                if (agent.Movement.Pose > 0.3f && (coverPoint.Level != CoverLevel.Stay || Random.value > 0.5f))
                {
                    MovementSystem.ResetGait(agent, pose: 0.25f);
                }

                if (agent.Guard.OverwatchJob == null)
                {
                    SubmitOverwatchJob(agent, coverPoint);
                    Log.Debug($"{agent} submitted overwatch job");
                    continue;
                }

                if (agent.Guard.WatchDirection == null)
                {
                    CompleteOverwatchJob(agent, agent.Guard.OverwatchJob.Value);
                    Log.Debug($"{agent} completed overwatch job, sweep angle: {agent.Guard.SweepAngle}");
                }

                if (agent.Guard.WatchDirection == null || agent.Guard.WatchTimeout > Time.time)
                {
                    continue;
                }
                
                ObserveDirection(agent, agent.Guard.WatchDirection.Value);
                Log.Debug($"{agent} changed observe direction timeout: {agent.Guard.WatchTimeout - Time.time}");
            }
            else
            {
                movementSystem.MoveToByPath(agent, coverPoint.Position, sprint: true, urgency: MovementUrgency.Low);
            }
        }
    }

    protected override void Deactivate(Agent entity)
    {
        var guard = entity.Guard;
        
        guard.OverwatchJob = null;
        guard.WatchDirection = null;
        guard.WatchTimeout = 0f;
        
        entity.Look.Target = null;
    }

    private void SubmitOverwatchJob(Agent agent, CoverPoint coverPoint)
    {
        var origin = agent.Player.PlayerBones.Head.position;

        _watchCandidates.Clear();

        if (coverPoint.Category == CoverCategory.Hard)
        {
            _watchCandidates.Add(-1 * coverPoint.Direction);

            // If it's not standing cover, also try to look in direction of the wall
            if (coverPoint.Level != CoverLevel.Stay)
            {
                _watchCandidates.Add(coverPoint.Direction);
            }
        }
        
        // Radial sweep
        const float angleStep = 360f / 10f;

        var forward = agent.Player.Transform.forward;
        
        for (var i = 0; i < 10; i++)
        {
            var angle = i * angleStep;
            var rotation = Quaternion.AngleAxis(angle, Vector3.up);
            var direction = rotation * forward;
            
            DebugGizmos.Line(agent.Position + Vector3.up, agent.Position + Vector3.up + 25f * direction, expiretime: 0f, color: Color.magenta);
            _watchCandidates.Add(direction);
        }

        // Find all arrival path points
        if (agent.Objective.ArrivalPath != null)
        {
            for (var i = agent.Objective.ArrivalPath.Length - 1; i >= 0; i--)
            {
                if (_watchCandidates.Count >= MaxWatchCandidateCount)
                {
                    break;
                }

                var point = agent.Objective.ArrivalPath[i] + 1.5f * Vector3.up;
                var dir = point - origin;
                dir.Normalize();
                _watchCandidates.Add(dir);
            }
        }

        // Find all nearby doors
        for (var i = 0; i < agent.Objective.Location.Doors.Count; i++)
        {
            if (_watchCandidates.Count >= MaxWatchCandidateCount)
            {
                break;
            }

            var door = agent.Objective.Location.Doors[i];
            var doorVector = door.transform.position - origin;
            doorVector.Normalize();
            _watchCandidates.Add(doorVector);
        }

        // Allocate raycast commands and results
        var commands = new NativeArray<RaycastCommand>(_watchCandidates.Count, Allocator.TempJob);
        var results = new NativeArray<RaycastHit>(_watchCandidates.Count, Allocator.TempJob);

        // Set up raycast commands
        for (var i = 0; i < _watchCandidates.Count; i++)
        {
            var direction = _watchCandidates[i];
            var parameters = new QueryParameters { layerMask = LayerMasksDataAbstractClass.HitMask };
            commands[i] = new RaycastCommand(origin, direction, parameters, 100);
        }
        
        Log.Debug($"{agent} found {_watchCandidates.Count} watch candidates");

        // Schedule and complete batch
        agent.Guard.OverwatchJob = new OverwatchJob
        {
            Handle = RaycastCommand.ScheduleBatch(commands, results, 1),
            Commands = commands,
            Hits = results,
        };
    }

    private static void CompleteOverwatchJob(Agent agent, OverwatchJob job)
    {
        job.Handle.Complete();

        var longest = -1f;
        var pick = Vector3.zero;
        
        for (var i = 0; i < job.Hits.Length; i++)
        {
            var cmd = job.Commands[i];
            var hit = job.Hits[i];
            
            var distance = hit.collider == null ? cmd.distance : hit.distance;

            if (distance <= longest)
            {
                continue;
            }
            
            longest = distance;
            pick = cmd.direction;
        }
        
        job.Commands.Dispose();
        job.Hits.Dispose();
        
        // Fallback to looking away from the objective if all else fails
        if (longest <= 0f)
        {
            pick = agent.Player.PlayerBones.Head.position - agent.Objective.Location.Position + Vector3.up;
            pick.Normalize();
        }
        
        agent.Guard.WatchDirection = pick;
        agent.Guard.SweepAngle = 35f * (0.75f + 0.25f * Mathf.InverseLerp(50f, 0f, longest));
    }

    private static void ObserveDirection(Agent agent, Vector3 watchDirection)
    {
        var randomDirection = LookSystem.RandomDirectionInEllipse(watchDirection, agent.Guard.SweepAngle, 15f);
        LookSystem.LookToDirection(agent, randomDirection, 120f);
        agent.Guard.WatchTimeout = Time.time + Random.Range(2.5f, 10f);
        
        DebugGizmos.Line(agent.Position + Vector3.up, agent.Position + Vector3.up + 25f * watchDirection, expiretime: 0f, color: Color.blue);
        DebugGizmos.Line(agent.Position + Vector3.up, agent.Position + Vector3.up + 25f * randomDirection, expiretime: 0f, color: Color.red);
    }
}