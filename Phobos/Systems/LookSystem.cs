using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Phobos.Components;
using Phobos.Diag;
using Phobos.Entities;
using Phobos.Helpers;
using UnityEngine;

namespace Phobos.Systems;

public class LookSystem
{
    private const float MoveLookAheadDistSqr = 1.5f;
    private const float MoveTargetProxmityDistSqr = 1f;

    public static void Update(List<Agent> liveAgents)
    {
        for (var i = 0; i < liveAgents.Count; i++)
        {
            var agent = liveAgents[i];

            // Bail out if the agent is inactive
            if (!agent.IsActive)
            {
                agent.Look.Target = null;
                continue;
            }

            var bot = agent.Bot;
            var movement = agent.Movement;

            if (agent.Look.Target != null)
            {
                continue;
            }

            if (!movement.HasPath || (movement.Path[^1] - agent.Position).sqrMagnitude <= MoveTargetProxmityDistSqr) continue;

            var fwdPoint = PathHelper.CalcForwardPoint(movement.Path, agent.Position, movement.CurrentCorner, MoveLookAheadDistSqr);
            var lookDirection = fwdPoint - agent.Position;
            lookDirection.Normalize();
            bot.Steering.LookToDirection(lookDirection, 540f);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LookToPoint(Agent agent, Vector3 target, float rotateSpeed = 180f)
    {
        agent.Look.Target = target;
        agent.Look.Type = LookType.Position;
        agent.Bot.Steering.LookToPoint(agent.Look.Target.Value, rotateSpeed);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LookToDirection(Agent agent, Vector3 target, float rotateSpeed = 180f)
    {
        agent.Look.Target = target;
        agent.Look.Type = LookType.Direction;
        agent.Bot.Steering.LookToDirection(agent.Look.Target.Value, rotateSpeed);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 RandomDirectionInEllipse(Vector3 centerDirection, float horizontalAngle, float verticalAngle)
    {
        // Random offsets within half-angles
        var horizontalOffset = Random.Range(-horizontalAngle / 2f, horizontalAngle / 2f);
        var verticalOffset = Random.Range(-verticalAngle / 2f, verticalAngle / 2f);

        // Convert center direction to rotation
        var centerRotation = Quaternion.LookRotation(centerDirection);

        // Apply random offsets as euler angles
        var offsetRotation = Quaternion.Euler(verticalOffset, horizontalOffset, 0f);

        // Combine and return final direction
        return centerRotation * offsetRotation * Vector3.forward;
    }
}